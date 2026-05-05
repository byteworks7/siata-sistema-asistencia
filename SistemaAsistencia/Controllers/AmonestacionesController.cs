using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.Models;
using SistemaAsistencia.Services;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/amonestaciones")]
    [Authorize(Roles = "ADMIN")]
    public class AmonestacionesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _email;

        public AmonestacionesController(AppDbContext db, EmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpGet]
        public async Task<IActionResult> GetTodas([FromQuery] int? idTrabajador)
        {
            var query = _db.Amonestaciones.Include(a => a.Trabajador).AsQueryable();
            if (idTrabajador.HasValue && idTrabajador > 0)
                query = query.Where(a => a.IdTrabajador == idTrabajador.Value);

            var lista = await query
                .OrderByDescending(a => a.FechaEmision)
                .Select(a => new
                {
                    id = a.Id,
                    idTrabajador = a.IdTrabajador,
                    nombreTrabajador = a.Trabajador != null
                        ? a.Trabajador.Nombres + " " + a.Trabajador.Apellidos : "",
                    tipo = a.Tipo,
                    motivo = a.Motivo,
                    fechaEmision = a.FechaEmision.ToString("yyyy-MM-dd"),
                    diasSuspension = a.DiasSuspension,
                    correoEnviado = a.CorreoEnviado
                }).ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearAmonestacionConFechaDto dto)
        {
            var trabajador = await _db.Trabajadores
                .FirstOrDefaultAsync(t => t.Id == dto.IdTrabajador);

            if (trabajador == null)
                return NotFound(new { mensaje = "Trabajador no encontrado" });

            byte dias = dto.Tipo switch
            {
                "SUSPENSION_1D" => 1,
                "SUSPENSION_2D" => 2,
                "SUSPENSION_3D" => 3,
                _ => 0
            };

            var amonestacion = new Amonestacion
            {
                IdTrabajador = dto.IdTrabajador,
                Tipo = dto.Tipo,
                Motivo = dto.Motivo,
                FechaEmision = DateOnly.FromDateTime(DateTime.Now),
                DiasSuspension = dias
            };

            _db.Amonestaciones.Add(amonestacion);

            var diasSuspendidos = new List<string>();
            if (dias > 0 && !string.IsNullOrEmpty(dto.FechaInicioSuspension))
            {
                var fechaInicio = DateOnly.Parse(dto.FechaInicioSuspension);
                diasSuspendidos = await MarcarDiasSuspension(dto.IdTrabajador, dias, fechaInicio);
            }

            bool correoEnviado = false;
            if (!string.IsNullOrEmpty(trabajador.Correo))
            {
                Console.WriteLine($"Intentando enviar correo a: {trabajador.Correo}");
                try
                {
                    await _email.EnviarAmonestacionAsync(
                        destinatario: trabajador.Correo,
                        nombreTrabajador: $"{trabajador.Nombres} {trabajador.Apellidos}",
                        tipoAmonestacion: dto.Tipo,
                        motivo: dto.Motivo,
                        diasSuspension: dias,
                        fechaEmision: DateTime.Now.ToString("dd/MM/yyyy")
                    );
                    correoEnviado = true;
                    Console.WriteLine($"Correo enviado exitosamente a: {trabajador.Correo}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando correo: {ex.Message}");
                    Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                }
            }
            else
            {
                Console.WriteLine("Trabajador sin correo registrado");
            }

            amonestacion.CorreoEnviado = correoEnviado;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Amonestacion registrada",
                id = amonestacion.Id,
                correoEnviado,
                diasSuspendidos,
                correoDestino = trabajador.Correo ?? "Sin correo"
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var a = await _db.Amonestaciones.FindAsync(id);
            if (a == null) return NotFound(new { mensaje = "No encontrada" });

            // Si tiene días de suspensión, limpiarlos en asistencias
            if (a.DiasSuspension > 0)
            {
                var hoy = DateOnly.FromDateTime(DateTime.Now);
                var asistenciasSuspendidas = await _db.Asistencias
                    .Where(ast => ast.IdTrabajador == a.IdTrabajador &&
                                  ast.EstadoEntrada == "SUSPENSION" &&
                                  ast.Fecha >= hoy) // solo futuros
                    .ToListAsync();

                _db.Asistencias.RemoveRange(asistenciasSuspendidas);
            }

            _db.Amonestaciones.Remove(a);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Amonestacion eliminada" });
        }

        private async Task<List<string>> MarcarDiasSuspension(
            int idTrabajador, int cantDias, DateOnly fechaInicio)
        {
            var diasMarcados = new List<string>();
            var fecha = fechaInicio;

            var horario = await _db.Horarios
                .Where(h => h.IdTrabajador == idTrabajador &&
                            h.FechaInicio <= fecha &&
                            (h.FechaFin == null || h.FechaFin >= fecha))
                .OrderByDescending(h => h.FechaInicio)
                .FirstOrDefaultAsync();

            var codigosDias = new[] { "D", "L", "M", "X", "J", "V", "S" };
            int diasContados = 0;
            int maxIter = 30;

            while (diasContados < cantDias && maxIter-- > 0)
            {
                if (EsDiaLaboral(horario, fecha, codigosDias))
                {
                    var asistencia = await _db.Asistencias
                        .FirstOrDefaultAsync(a => a.IdTrabajador == idTrabajador && a.Fecha == fecha);

                    if (asistencia == null)
                    {
                        _db.Asistencias.Add(new Asistencia
                        {
                            IdTrabajador = idTrabajador,
                            Fecha = fecha,
                            EstadoEntrada = "SUSPENSION",
                            EstadoSalida = "PENDIENTE",
                            CorregidoPorAdmin = true,
                            FechaCorreccion = DateTime.Now,
                            Observacion = "Dia de suspension por amonestacion"
                        });
                    }
                    else
                    {
                        asistencia.EstadoEntrada = "SUSPENSION";
                        asistencia.CorregidoPorAdmin = true;
                        asistencia.FechaCorreccion = DateTime.Now;
                        asistencia.Observacion = "Dia de suspension por amonestacion";
                    }

                    diasMarcados.Add(fecha.ToString("dd/MM/yyyy"));
                    diasContados++;
                }
                fecha = fecha.AddDays(1);
            }

            return diasMarcados;
        }

        private static bool EsDiaLaboral(Horario? horario, DateOnly fecha, string[] codigosDias)
        {
            if (horario == null) return true;
            if (!string.IsNullOrEmpty(horario.DiasTrabajo))
                return horario.DiasTrabajo.Contains(codigosDias[(int)fecha.DayOfWeek]);
            if (horario.DiasTrabajoCiclo.HasValue && horario.DiasDescansoCiclo.HasValue)
            {
                int ciclo = horario.DiasTrabajoCiclo.Value + horario.DiasDescansoCiclo.Value;
                int diasDesde = (fecha.ToDateTime(TimeOnly.MinValue) -
                                  horario.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days;
                return (diasDesde % ciclo) < horario.DiasTrabajoCiclo.Value;
            }
            return true;
        }
    }

    public class CrearAmonestacionConFechaDto
    {
        public int IdTrabajador { get; set; }
        public string Tipo { get; set; } = null!;
        public string Motivo { get; set; } = null!;
        public string? FechaInicioSuspension { get; set; }
    }
}