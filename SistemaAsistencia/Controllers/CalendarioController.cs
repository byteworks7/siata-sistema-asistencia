using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.DTOs;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/calendario")]
    [Authorize]
    public class CalendarioController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CalendarioController(AppDbContext db) => _db = db;

        [HttpGet("{idTrabajador:int}")]
        public async Task<IActionResult> GetCalendario(
            int idTrabajador, [FromQuery] int mes, [FromQuery] int anio)
        {
            var trabajador = await _db.Trabajadores
                .Include(t => t.Area)
                .Include(t => t.Cargo)
                .FirstOrDefaultAsync(t => t.Id == idTrabajador);

            if (trabajador == null)
                return NotFound(new { mensaje = "Trabajador no encontrado" });

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var primerDia = new DateOnly(anio, mes, 1);
            var ultimoDia = DateOnly.FromDateTime(new DateTime(anio, mes, 1).AddMonths(1).AddDays(-1));

            var asistencias = await _db.Asistencias
                .Where(a => a.IdTrabajador == idTrabajador &&
                            a.Fecha.Month == mes && a.Fecha.Year == anio)
                .ToListAsync();

            var descansos = await _db.DiasDescanso
                .Where(d => d.IdTrabajador == idTrabajador &&
                            d.Fecha.Month == mes && d.Fecha.Year == anio)
                .ToListAsync();

            var horario = await _db.Horarios
                .Where(h => h.IdTrabajador == idTrabajador &&
                            h.FechaInicio <= ultimoDia &&
                            (h.FechaFin == null || h.FechaFin >= primerDia))
                .OrderByDescending(h => h.FechaInicio)
                .FirstOrDefaultAsync();

            var dias = new List<CalendarioDiaDto>();
            var nombresDias = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
            var codigosDias = new[] { "D", "L", "M", "X", "J", "V", "S" };

            // Estados que tienen prioridad sobre el ciclo (admin los puso manualmente)
            var estadosPrioridad = new[] { "VACACIONES", "PERMISO", "LICENCIA", "SUSPENSION" };

            for (var fecha = primerDia; fecha <= ultimoDia; fecha = fecha.AddDays(1))
            {
                var dia = new CalendarioDiaDto
                {
                    Fecha = fecha.ToString("yyyy-MM-dd"),
                    DiaSemana = nombresDias[(int)fecha.DayOfWeek],
                    HorarioEntrada = horario?.HoraEntrada.ToString("HH:mm"),
                    HorarioSalida = horario?.HoraSalida.ToString("HH:mm")
                };

                // ── PRIORIDAD 1: Revisar asistencia con estado especial ──
                // Esto aplica a CUALQUIER día (pasado, hoy o futuro)
                var asistenciaEspecial = asistencias.FirstOrDefault(a => a.Fecha == fecha);
                if (asistenciaEspecial != null && estadosPrioridad.Contains(asistenciaEspecial.EstadoEntrada))
                {
                    dia.Estado = asistenciaEspecial.EstadoEntrada;
                    dia.HoraEntrada = asistenciaEspecial.HoraEntrada?.ToString("HH:mm");
                    dia.HoraSalida = asistenciaEspecial.HoraSalida?.ToString("HH:mm");
                    dia.EstadoSalida = asistenciaEspecial.EstadoSalida;
                    dia.CorregidoPorAdmin = asistenciaEspecial.CorregidoPorAdmin;
                    dia.Observacion = asistenciaEspecial.Observacion;
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 2: Descanso registrado manualmente ──
                var descanso = descansos.FirstOrDefault(d => d.Fecha == fecha);
                if (descanso != null)
                {
                    dia.Estado = "DESCANSO";
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 3: Sin horario ──
                if (horario == null)
                {
                    dia.Estado = fecha <= hoy ? "SIN_HORARIO" : "SIN_REGISTRO";
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 4: Día de descanso del ciclo ──
                bool esDiaLaboral = EsDiaLaboral(horario, fecha, codigosDias);
                if (!esDiaLaboral)
                {
                    dia.Estado = "DESCANSO";
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 5: Hoy ──
                if (fecha == hoy)
                {
                    var asistenciaHoy = asistencias.FirstOrDefault(a => a.Fecha == fecha);
                    if (asistenciaHoy != null)
                    {
                        dia.HoraEntrada = asistenciaHoy.HoraEntrada?.ToString("HH:mm");
                        dia.HoraSalida = asistenciaHoy.HoraSalida?.ToString("HH:mm");
                        dia.Estado = asistenciaHoy.EstadoEntrada;
                        dia.EstadoSalida = asistenciaHoy.EstadoSalida;
                        dia.MinutosTardanza = asistenciaHoy.MinutosTardanza;
                        dia.MinutosSalidaAnticipada = asistenciaHoy.MinutosSalidaAnticipada;
                        dia.CorregidoPorAdmin = asistenciaHoy.CorregidoPorAdmin;
                        dia.Observacion = asistenciaHoy.Observacion;
                    }
                    else { dia.Estado = "HOY"; }
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 6: Días pasados ──
                if (fecha < hoy)
                {
                    var asistencia = asistencias.FirstOrDefault(a => a.Fecha == fecha);
                    if (asistencia != null)
                    {
                        dia.HoraEntrada = asistencia.HoraEntrada?.ToString("HH:mm");
                        dia.HoraSalida = asistencia.HoraSalida?.ToString("HH:mm");
                        dia.MinutosTardanza = asistencia.MinutosTardanza;
                        dia.MinutosSalidaAnticipada = asistencia.MinutosSalidaAnticipada;
                        dia.CorregidoPorAdmin = asistencia.CorregidoPorAdmin;
                        dia.Observacion = asistencia.Observacion;
                        dia.EstadoSalida = asistencia.EstadoSalida;
                        dia.Estado = (asistencia.HoraEntrada != null &&
                                      asistencia.EstadoSalida == "SALIDA_NO_REGISTRADA")
                                     ? "SALIDA_NO_REGISTRADA"
                                     : asistencia.EstadoEntrada;
                    }
                    else { dia.Estado = "FALTA"; }
                    dias.Add(dia);
                    continue;
                }

                // ── PRIORIDAD 7: Días futuros ──
                dia.Estado = "SIN_REGISTRO";
                dias.Add(dia);
            }

            return Ok(new CalendarioMesDto
            {
                Mes = mes,
                Anio = anio,
                NombreTrabajador = $"{trabajador.Nombres} {trabajador.Apellidos}",
                Cargo = trabajador.Cargo?.Nombre,
                Area = trabajador.Area?.Nombre,
                FotoUrl = trabajador.FotoUrl,
                Dias = dias,
                TotalPuntuales = dias.Count(d => d.Estado == "PUNTUAL"),
                TotalATiempo = dias.Count(d => d.Estado == "A_TIEMPO"),
                TotalTardanzas = dias.Count(d => d.Estado == "TARDANZA"),
                TotalFaltas = dias.Count(d => d.Estado == "FALTA"),
                TotalSalidaNoRegistrada = dias.Count(d => d.Estado == "SALIDA_NO_REGISTRADA"),
                TotalDescansos = dias.Count(d => d.Estado == "DESCANSO"),
                TotalMinutosTardanza = asistencias.Sum(a => (int)a.MinutosTardanza),
                TotalMinutosSalidaAnticipada = asistencias.Sum(a => (int)a.MinutosSalidaAnticipada)
            });
        }

        [HttpGet("{idTrabajador:int}/semana")]
        public async Task<IActionResult> GetSemana(int idTrabajador)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            int offset = ((int)hoy.DayOfWeek == 0) ? 6 : (int)hoy.DayOfWeek - 1;
            var lunes = hoy.AddDays(-offset);
            var sabado = lunes.AddDays(5);

            var asistencias = await _db.Asistencias
                .Where(a => a.IdTrabajador == idTrabajador &&
                            a.Fecha >= lunes && a.Fecha <= sabado)
                .ToListAsync();

            var descansos = await _db.DiasDescanso
                .Where(d => d.IdTrabajador == idTrabajador &&
                            d.Fecha >= lunes && d.Fecha <= sabado)
                .ToListAsync();

            var nombresDias = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
            var estadosPrioridad = new[] { "VACACIONES", "PERMISO", "LICENCIA", "SUSPENSION" };
            var dias = new List<DiaSemanaDto>();

            for (int i = 0; i <= 5; i++)
            {
                var fecha = lunes.AddDays(i);
                var descanso = descansos.FirstOrDefault(d => d.Fecha == fecha);
                var asistencia = asistencias.FirstOrDefault(a => a.Fecha == fecha);

                var diaDto = new DiaSemanaDto
                {
                    Fecha = fecha.ToString("yyyy-MM-dd"),
                    DiaSemana = nombresDias[(int)fecha.DayOfWeek],
                    EsDescanso = descanso != null,
                    EsHoy = fecha == hoy,
                    EstadoEntrada = "SIN_REGISTRO",
                    EstadoSalida = "PENDIENTE"
                };

                if (asistencia != null)
                {
                    diaDto.HoraEntrada = asistencia.HoraEntrada?.ToString("HH:mm");
                    diaDto.HoraSalida = asistencia.HoraSalida?.ToString("HH:mm");
                    diaDto.EstadoEntrada = asistencia.EstadoEntrada;
                    diaDto.EstadoSalida = asistencia.EstadoSalida;
                    diaDto.MinutosTardanza = asistencia.MinutosTardanza;
                    diaDto.MinutosSalidaAnticipada = asistencia.MinutosSalidaAnticipada;
                }
                else if (descanso == null && fecha < hoy)
                {
                    diaDto.EstadoEntrada = "FALTA";
                }

                dias.Add(diaDto);
            }

            return Ok(dias);
        }

        private static bool EsDiaLaboral(
            Models.Horario horario, DateOnly fecha, string[] codigosDias)
        {
            if (!string.IsNullOrEmpty(horario.DiasTrabajo))
            {
                var codigo = codigosDias[(int)fecha.DayOfWeek];
                return horario.DiasTrabajo.Contains(codigo);
            }
            if (horario.DiasTrabajoCiclo.HasValue && horario.DiasDescansoCiclo.HasValue)
            {
                int ciclo = horario.DiasTrabajoCiclo.Value + horario.DiasDescansoCiclo.Value;
                int diasDesde = (fecha.ToDateTime(TimeOnly.MinValue) -
                                   horario.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days;
                int posEnCiclo = diasDesde % ciclo;
                return posEnCiclo < horario.DiasTrabajoCiclo.Value;
            }
            return true;
        }
    }
}