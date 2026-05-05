using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.DTOs;
using SistemaAsistencia.Models;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/horarios")]
    [Authorize(Roles = "ADMIN")]
    public class HorariosController : ControllerBase
    {
        private readonly AppDbContext _db;
        public HorariosController(AppDbContext db) => _db = db;

        // GET /api/horarios
        [HttpGet]
        public async Task<IActionResult> GetTodos()
        {
            var horarios = await _db.Horarios
                .Include(h => h.Plantilla)
                .Include(h => h.Trabajador)
                .OrderByDescending(h => h.FechaInicio)
                .ToListAsync();

            var result = horarios.Select(h => new
            {
                id = h.Id,
                idTrabajador = h.IdTrabajador,
                nombreTrabajador = h.Trabajador != null
                    ? h.Trabajador.Nombres + " " + h.Trabajador.Apellidos : "",
                idPlantilla = h.IdPlantilla,
                nombrePlantilla = h.Plantilla != null ? h.Plantilla.Nombre : "Personalizado",
                horaEntrada = h.HoraEntrada.ToString("HH:mm"),
                horaSalida = h.HoraSalida.ToString("HH:mm"),
                diasDescanso = h.DiasTrabajo != null
                    ? h.DiasTrabajo
                    : (h.DiasTrabajoCiclo.HasValue
                        ? $"{h.DiasTrabajoCiclo}x{h.DiasDescansoCiclo} (rotativo)"
                        : "—"),
                fechaInicio = h.FechaInicio.ToString("yyyy-MM-dd"),
                fechaFin = h.FechaFin.HasValue ? h.FechaFin.Value.ToString("yyyy-MM-dd") : null
            });

            return Ok(result);
        }

        // GET /api/horarios/trabajador/{id}
        [HttpGet("trabajador/{idTrabajador:int}")]
        public async Task<IActionResult> GetByTrabajador(int idTrabajador)
        {
            var horarios = await _db.Horarios
                .Include(h => h.Plantilla)
                .Where(h => h.IdTrabajador == idTrabajador)
                .OrderByDescending(h => h.FechaInicio)
                .Select(h => new HorarioDto
                {
                    Id = h.Id,
                    IdTrabajador = h.IdTrabajador,
                    IdPlantilla = h.IdPlantilla,
                    NombrePlantilla = h.Plantilla != null ? h.Plantilla.Nombre : null,
                    HoraEntrada = h.HoraEntrada.ToString("HH:mm"),
                    HoraSalida = h.HoraSalida.ToString("HH:mm"),
                    ToleranciaMinutos = h.ToleranciaMinutos,
                    DiasTrabajo = h.DiasTrabajo,
                    DiasTrabajoCiclo = h.DiasTrabajoCiclo,
                    DiasDescansoCiclo = h.DiasDescansoCiclo,
                    FechaInicio = h.FechaInicio.ToString("yyyy-MM-dd"),
                    FechaFin = h.FechaFin.HasValue ? h.FechaFin.Value.ToString("yyyy-MM-dd") : null
                }).ToListAsync();

            return Ok(horarios);
        }

        // POST /api/horarios
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearHorarioDto dto)
        {
            var fechaInicio = DateOnly.Parse(dto.FechaInicio);
            var fechaFin = dto.FechaFin != null ? DateOnly.Parse(dto.FechaFin) : (DateOnly?)null;

            if (dto.CerrarHorarioAnterior)
            {
                var vigente = await _db.Horarios
                    .Where(h => h.IdTrabajador == dto.IdTrabajador &&
                                h.FechaFin == null &&
                                h.FechaInicio < fechaInicio)
                    .OrderByDescending(h => h.FechaInicio)
                    .FirstOrDefaultAsync();

                if (vigente != null)
                    vigente.FechaFin = fechaInicio.AddDays(-1);
            }

            byte? diasTrabajoCiclo = dto.DiasTrabajoCiclo.HasValue ? (byte)dto.DiasTrabajoCiclo.Value : null;
            byte? diasDescansoCiclo = dto.DiasDescansoCiclo.HasValue ? (byte)dto.DiasDescansoCiclo.Value : null;
            string? diasTrabajo = dto.DiasTrabajo;
            TimeOnly entrada = TimeOnly.Parse(dto.HoraEntrada);
            TimeOnly salida = TimeOnly.Parse(dto.HoraSalida);

            if (dto.IdPlantilla.HasValue && dto.IdPlantilla > 0)
            {
                var plantilla = await _db.PlantillasTurno.FindAsync(dto.IdPlantilla);
                if (plantilla != null)
                {
                    diasTrabajoCiclo = plantilla.DiasTrabajoCiclo;
                    diasDescansoCiclo = plantilla.DiasDescansoCiclo;
                    diasTrabajo = plantilla.DiasSemanaFijos;
                }
            }

            var horario = new Horario
            {
                IdTrabajador = dto.IdTrabajador,
                IdPlantilla = dto.IdPlantilla > 0 ? dto.IdPlantilla : null,
                HoraEntrada = entrada,
                HoraSalida = salida,
                ToleranciaMinutos = (byte)dto.ToleranciaMinutos,
                DiasTrabajo = diasTrabajo,
                DiasTrabajoCiclo = diasTrabajoCiclo,
                DiasDescansoCiclo = diasDescansoCiclo,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                CreadoPor = 1
            };

            _db.Horarios.Add(horario);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Horario creado correctamente", id = horario.Id });
        }

        // PUT /api/horarios/override-dia
        [HttpPut("override-dia")]
        public async Task<IActionResult> OverrideDia([FromBody] OverrideDiaDto dto)
        {
            var fecha = DateOnly.Parse(dto.Fecha);
            var asistencia = await _db.Asistencias
                .FirstOrDefaultAsync(a => a.IdTrabajador == dto.IdTrabajador && a.Fecha == fecha);

            if (asistencia == null)
            {
                asistencia = new Asistencia
                {
                    IdTrabajador = dto.IdTrabajador,
                    Fecha = fecha,
                    EstadoEntrada = "SIN_REGISTRO",
                    EstadoSalida = "PENDIENTE",
                    HoraEntradaProgramada = dto.HoraEntrada != null ? TimeOnly.Parse(dto.HoraEntrada) : null,
                    HoraSalidaProgramada = dto.HoraSalida != null ? TimeOnly.Parse(dto.HoraSalida) : null,
                    Observacion = dto.Observacion
                };
                _db.Asistencias.Add(asistencia);
            }
            else
            {
                asistencia.HoraEntradaProgramada = dto.HoraEntrada != null ? TimeOnly.Parse(dto.HoraEntrada) : null;
                asistencia.HoraSalidaProgramada = dto.HoraSalida != null ? TimeOnly.Parse(dto.HoraSalida) : null;
                asistencia.Observacion = dto.Observacion;
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Horario del día actualizado" });
        }

        // PUT /api/horarios/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CrearHorarioDto dto)
        {
            var h = await _db.Horarios.FindAsync(id);
            if (h == null) return NotFound(new { mensaje = "Horario no encontrado" });

            h.HoraEntrada = TimeOnly.Parse(dto.HoraEntrada);
            h.HoraSalida = TimeOnly.Parse(dto.HoraSalida);
            h.ToleranciaMinutos = (byte)dto.ToleranciaMinutos;
            h.DiasTrabajo = dto.DiasTrabajo;
            h.DiasTrabajoCiclo = dto.DiasTrabajoCiclo.HasValue ? (byte)dto.DiasTrabajoCiclo.Value : null;
            h.DiasDescansoCiclo = dto.DiasDescansoCiclo.HasValue ? (byte)dto.DiasDescansoCiclo.Value : null;
            h.FechaInicio = DateOnly.Parse(dto.FechaInicio);
            h.FechaFin = dto.FechaFin != null ? DateOnly.Parse(dto.FechaFin) : null;
            h.IdPlantilla = dto.IdPlantilla > 0 ? dto.IdPlantilla : null;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Horario actualizado" });
        }

        // DELETE /api/horarios/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var h = await _db.Horarios.FindAsync(id);
            if (h == null) return NotFound(new { mensaje = "Horario no encontrado" });
            _db.Horarios.Remove(h);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Horario eliminado" });
        }

        // GET /api/horarios/plantillas
        [HttpGet("plantillas")]
        public async Task<IActionResult> GetPlantillas()
        {
            var plantillas = await _db.PlantillasTurno
                .Where(p => p.Activo)
                .Select(p => new PlantillaTurnoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    HorasTurno = p.HorasTurno,
                    DiasTrabajoCiclo = p.DiasTrabajoCiclo,
                    DiasDescansoCiclo = p.DiasDescansoCiclo,
                    DiasSemanaFijos = p.DiasSemanaFijos
                }).ToListAsync();

            return Ok(plantillas);
        }
    }

    public class OverrideDiaDto
    {
        public int IdTrabajador { get; set; }
        public string Fecha { get; set; } = null!;
        public string? HoraEntrada { get; set; }
        public string? HoraSalida { get; set; }
        public string? Observacion { get; set; }
    }
}