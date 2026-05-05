using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.DTOs;
using SistemaAsistencia.Models;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/asistencias")]
    [Authorize]
    public class AsistenciasController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AsistenciasController(AppDbContext db) => _db = db;

        // POST /api/asistencias/marcar
        [AllowAnonymous]
        [HttpPost("marcar")]
        public async Task<IActionResult> Marcar([FromBody] MarcarAsistenciaDto dto)
        {
            var trabajador = await _db.Trabajadores
                .FirstOrDefaultAsync(t => t.Dni == dto.Dni && t.Estado);

            if (trabajador == null)
                return NotFound(new ResultadoMarcadoDto
                {
                    Exito = false,
                    Mensaje = "DNI no encontrado. Verifique e intente nuevamente."
                });

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ahora = DateTime.Now;

            var horario = await _db.Horarios
                .Where(h => h.IdTrabajador == trabajador.Id &&
                            h.FechaInicio <= hoy &&
                            (h.FechaFin == null || h.FechaFin >= hoy))
                .OrderByDescending(h => h.FechaInicio)
                .FirstOrDefaultAsync();

            var asistencia = await _db.Asistencias
                .FirstOrDefaultAsync(a => a.IdTrabajador == trabajador.Id && a.Fecha == hoy);

            // ENTRADA
            if (dto.Tipo == "ENTRADA")
            {
                if (asistencia?.HoraEntrada != null)
                    return BadRequest(new ResultadoMarcadoDto { Exito = false, Mensaje = "Ya registraste tu entrada hoy." });

                string estadoEntrada = "SIN_HORARIO";
                short minTardanza = 0;
                string mensaje = "¡Bienvenido! Entrada registrada.";

                var heEntrada = asistencia?.HoraEntradaProgramada ?? horario?.HoraEntrada;

                if (heEntrada.HasValue)
                {
                    var entradaExacta = ahora.Date.Add(heEntrada.Value.ToTimeSpan());
                    var limiteEntrada = entradaExacta.AddMinutes(horario?.ToleranciaMinutos ?? 5);

                    if (ahora < entradaExacta)
                    { estadoEntrada = "PUNTUAL"; mensaje = "¡Llegaste antes! Entrada registrada."; }
                    else if (ahora <= limiteEntrada)
                    { estadoEntrada = "A_TIEMPO"; mensaje = "¡Bienvenido! Entrada registrada a tiempo."; }
                    else
                    {
                        estadoEntrada = "TARDANZA";
                        minTardanza = (short)(ahora - entradaExacta).TotalMinutes;
                        mensaje = $"Entrada con tardanza de {minTardanza} min.";
                    }
                }

                if (asistencia == null)
                {
                    asistencia = new Asistencia
                    {
                        IdTrabajador = trabajador.Id,
                        Fecha = hoy,
                        HoraEntrada = ahora,
                        EstadoEntrada = estadoEntrada,
                        EstadoSalida = "PENDIENTE",
                        MinutosTardanza = minTardanza
                    };
                    _db.Asistencias.Add(asistencia);
                }
                else
                {
                    asistencia.HoraEntrada = ahora;
                    asistencia.EstadoEntrada = estadoEntrada;
                    asistencia.MinutosTardanza = minTardanza;
                }

                await _db.SaveChangesAsync();
                return Ok(new ResultadoMarcadoDto
                {
                    Exito = true,
                    Mensaje = mensaje,
                    NombreTrabajador = $"{trabajador.Nombres} {trabajador.Apellidos}",
                    Estado = estadoEntrada,
                    Hora = ahora.ToString("HH:mm:ss"),
                    FotoUrl = trabajador.FotoUrl
                });
            }

            // SALIDA
            if (dto.Tipo == "SALIDA")
            {
                if (asistencia?.HoraEntrada == null)
                    return BadRequest(new ResultadoMarcadoDto { Exito = false, Mensaje = "Debes registrar tu entrada primero." });

                if (asistencia.HoraSalida != null)
                    return BadRequest(new ResultadoMarcadoDto { Exito = false, Mensaje = "Ya registraste tu salida hoy." });

                string estadoSalida = "REGISTRADA";
                short minSalidaAnticipada = 0;

                var heSalida = asistencia.HoraSalidaProgramada ?? horario?.HoraSalida;
                if (heSalida.HasValue)
                {
                    var salidaProgramada = ahora.Date.Add(heSalida.Value.ToTimeSpan());
                    if (ahora < salidaProgramada)
                    {
                        estadoSalida = "SALIDA_ANTICIPADA";
                        minSalidaAnticipada = (short)(salidaProgramada - ahora).TotalMinutes;
                    }
                }

                asistencia.HoraSalida = ahora;
                asistencia.EstadoSalida = estadoSalida;
                asistencia.MinutosSalidaAnticipada = minSalidaAnticipada;
                await _db.SaveChangesAsync();

                string mensaje = estadoSalida == "SALIDA_ANTICIPADA"
                    ? $"Salida anticipada ({minSalidaAnticipada} min antes). ¡Hasta pronto!"
                    : "Salida registrada. ¡Hasta mañana!";

                return Ok(new ResultadoMarcadoDto
                {
                    Exito = true,
                    Mensaje = mensaje,
                    NombreTrabajador = $"{trabajador.Nombres} {trabajador.Apellidos}",
                    Estado = estadoSalida,
                    Hora = ahora.ToString("HH:mm:ss"),
                    FotoUrl = trabajador.FotoUrl
                });
            }

            return BadRequest(new { mensaje = "Tipo inválido. Use ENTRADA o SALIDA." });
        }

        // POST /api/asistencias/crear-manual
        [HttpPost("crear-manual")]
        public async Task<IActionResult> CrearManual([FromBody] CrearAsistenciaManualDto dto)
        {
            var fecha = DateOnly.Parse(dto.Fecha);

            // Si ya existe, solo actualizar
            var existente = await _db.Asistencias
                .FirstOrDefaultAsync(a => a.IdTrabajador == dto.IdTrabajador && a.Fecha == fecha);

            if (existente != null)
            {
                existente.EstadoEntrada = dto.EstadoEntrada;
                existente.EstadoSalida = dto.EstadoSalida ?? "PENDIENTE";
                existente.Observacion = dto.Observacion;
                existente.CorregidoPorAdmin = true;
                existente.FechaCorreccion = DateTime.Now;
            }
            else
            {
                var nueva = new Asistencia
                {
                    IdTrabajador = dto.IdTrabajador,
                    Fecha = fecha,
                    EstadoEntrada = dto.EstadoEntrada,
                    EstadoSalida = dto.EstadoSalida ?? "PENDIENTE",
                    Observacion = dto.Observacion,
                    CorregidoPorAdmin = true,
                    FechaCorreccion = DateTime.Now
                };
                _db.Asistencias.Add(nueva);
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Registro creado correctamente" });
        }

        // POST /api/asistencias/cerrar-dia
        [HttpPost("cerrar-dia")]
        public async Task<IActionResult> CerrarDia()
        {
            var ayer = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            var pendientes = await _db.Asistencias
                .Where(a => a.Fecha == ayer && a.EstadoSalida == "PENDIENTE" && a.HoraEntrada != null)
                .ToListAsync();

            foreach (var a in pendientes)
            {
                a.EstadoSalida = "SALIDA_NO_REGISTRADA";
                a.Observacion = (a.Observacion != null ? a.Observacion + " | " : "") +
                                  "Salida no registrada - cierre automático";
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"{pendientes.Count} registros cerrados" });
        }

        // GET /api/asistencias/hoy
        [HttpGet("hoy")]
        public async Task<IActionResult> GetHoy()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var lista = await _db.Asistencias
                .Include(a => a.Trabajador)
                    .ThenInclude(t => t!.Cargo)
                .Include(a => a.Trabajador)
                    .ThenInclude(t => t!.Area)
                .Where(a => a.Fecha == hoy)
                .Select(a => new
                {
                    id = a.Id,
                    idTrabajador = a.IdTrabajador,
                    nombreTrabajador = $"{a.Trabajador!.Nombres} {a.Trabajador.Apellidos}",
                    cargo = a.Trabajador.Cargo != null ? a.Trabajador.Cargo.Nombre : "",
                    area = a.Trabajador.Area != null ? a.Trabajador.Area.Nombre : "",
                    fecha = a.Fecha.ToString("yyyy-MM-dd"),
                    horaEntrada = a.HoraEntrada.HasValue ? a.HoraEntrada.Value.ToString("HH:mm") : null,
                    horaSalida = a.HoraSalida.HasValue ? a.HoraSalida.Value.ToString("HH:mm") : null,
                    estadoEntrada = a.EstadoEntrada,
                    estadoSalida = a.EstadoSalida,
                    minutosTardanza = a.MinutosTardanza,
                    corregidoPorAdmin = a.CorregidoPorAdmin,
                    observacion = a.Observacion
                }).ToListAsync();

            return Ok(lista);
        }

        // GET /api/asistencias/trabajador/{id}?mes=X&anio=Y
        [HttpGet("trabajador/{idTrabajador:int}")]
        public async Task<IActionResult> GetByTrabajador(
            int idTrabajador, [FromQuery] int mes, [FromQuery] int anio)
        {
            var lista = await _db.Asistencias
                .Where(a => a.IdTrabajador == idTrabajador &&
                            a.Fecha.Month == mes && a.Fecha.Year == anio)
                .Select(a => new AsistenciaDto
                {
                    Id = a.Id,
                    IdTrabajador = a.IdTrabajador,
                    NombreTrabajador = "",
                    Fecha = a.Fecha.ToString("yyyy-MM-dd"),
                    HoraEntrada = a.HoraEntrada.HasValue ? a.HoraEntrada.Value.ToString("HH:mm") : null,
                    HoraSalida = a.HoraSalida.HasValue ? a.HoraSalida.Value.ToString("HH:mm") : null,
                    EstadoEntrada = a.EstadoEntrada,
                    EstadoSalida = a.EstadoSalida,
                    MinutosTardanza = a.MinutosTardanza,
                    MinutosSalidaAnticipada = a.MinutosSalidaAnticipada,
                    CorregidoPorAdmin = a.CorregidoPorAdmin,
                    Observacion = a.Observacion
                }).ToListAsync();

            return Ok(lista);
        }

        // PUT /api/asistencias/{id}/corregir
        [HttpPut("{id:int}/corregir")]
        public async Task<IActionResult> Corregir(int id, [FromBody] CorregirAsistenciaDto dto)
        {
            var asistencia = await _db.Asistencias.FindAsync(id);
            if (asistencia == null)
                return NotFound(new { mensaje = "Asistencia no encontrada" });

            if (!string.IsNullOrEmpty(dto.HoraEntrada))
                asistencia.HoraEntrada = asistencia.Fecha.ToDateTime(TimeOnly.Parse(dto.HoraEntrada));

            if (!string.IsNullOrEmpty(dto.HoraSalida))
                asistencia.HoraSalida = asistencia.Fecha.ToDateTime(TimeOnly.Parse(dto.HoraSalida));

            if (!string.IsNullOrEmpty(dto.EstadoEntrada))
                asistencia.EstadoEntrada = dto.EstadoEntrada;

            if (!string.IsNullOrEmpty(dto.EstadoSalida))
                asistencia.EstadoSalida = dto.EstadoSalida;

            if (!string.IsNullOrEmpty(dto.Observacion))
                asistencia.Observacion = dto.Observacion;

            asistencia.CorregidoPorAdmin = true;
            asistencia.FechaCorreccion = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Asistencia corregida correctamente" });
        }
    }
}

// DTO para crear asistencia manual
public class CrearAsistenciaManualDto
{
    public int IdTrabajador { get; set; }
    public string Fecha { get; set; } = null!;
    public string EstadoEntrada { get; set; } = "VACACIONES";
    public string? EstadoSalida { get; set; }
    public string? Observacion { get; set; }
}