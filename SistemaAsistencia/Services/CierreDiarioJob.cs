using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.Models;

namespace SistemaAsistencia.Services
{
    public class CierreDiarioJob : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CierreDiarioJob> _logger;

        public CierreDiarioJob(IServiceProvider services, ILogger<CierreDiarioJob> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🕐 CierreDiarioJob iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calcular cuánto tiempo falta para las 23:59
                var ahora = DateTime.Now;
                var cierreHoy = ahora.Date.AddHours(23).AddMinutes(59);
                var espera = cierreHoy > ahora
                    ? cierreHoy - ahora
                    : cierreHoy.AddDays(1) - ahora;

                _logger.LogInformation($"⏳ Próximo cierre en: {espera.Hours}h {espera.Minutes}m");

                await Task.Delay(espera, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await EjecutarCierre();
                }
            }
        }

        private async Task EjecutarCierre()
        {
            _logger.LogInformation($"🔄 Ejecutando cierre diario: {DateTime.Now:dd/MM/yyyy HH:mm}");

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ayer = hoy.AddDays(-1);
            var codigosDias = new[] { "D", "L", "M", "X", "J", "V", "S" };

            // 1. Cerrar salidas pendientes de ayer
            var conEntradaSinSalida = await db.Asistencias
                .Where(a => a.Fecha == ayer &&
                            a.HoraEntrada != null &&
                            a.EstadoSalida == "PENDIENTE")
                .ToListAsync();

            foreach (var a in conEntradaSinSalida)
            {
                a.EstadoSalida = "SALIDA_NO_REGISTRADA";
                a.Observacion = (a.Observacion != null ? a.Observacion + " | " : "") +
                                  "Salida no registrada — cierre automático";
            }

            _logger.LogInformation($"✅ {conEntradaSinSalida.Count} registros con salida pendiente cerrados");

            // 2. Registrar FALTA para trabajadores que no llegaron ayer
            var trabajadores = await db.Trabajadores
                .Where(t => t.Estado)
                .ToListAsync();

            int faltas = 0;

            foreach (var trabajador in trabajadores)
            {
                // Verificar si ya tiene registro de ayer
                var tieneRegistro = await db.Asistencias
                    .AnyAsync(a => a.IdTrabajador == trabajador.Id && a.Fecha == ayer);

                if (tieneRegistro) continue;

                // Verificar si es día de descanso
                var horario = await db.Horarios
                    .Where(h => h.IdTrabajador == trabajador.Id &&
                                h.FechaInicio <= ayer &&
                                (h.FechaFin == null || h.FechaFin >= ayer))
                    .OrderByDescending(h => h.FechaInicio)
                    .FirstOrDefaultAsync();

                if (horario == null) continue; // Sin horario, no se registra falta

                bool esDiaLaboral = EsDiaLaboral(horario, ayer, codigosDias);
                if (!esDiaLaboral) continue; // Era día de descanso

                // Verificar días de descanso manuales
                bool esDescansoManual = await db.DiasDescanso
                    .AnyAsync(d => d.IdTrabajador == trabajador.Id && d.Fecha == ayer);

                if (esDescansoManual) continue;

                // Crear registro de FALTA
                db.Asistencias.Add(new Asistencia
                {
                    IdTrabajador = trabajador.Id,
                    Fecha = ayer,
                    EstadoEntrada = "FALTA",
                    EstadoSalida = "SALIDA_NO_REGISTRADA",
                    Observacion = "Falta registrada automáticamente"
                });

                faltas++;
            }

            await db.SaveChangesAsync();

            _logger.LogInformation($"✅ Cierre completado: {conEntradaSinSalida.Count} salidas cerradas, {faltas} faltas registradas");
        }

        private static bool EsDiaLaboral(Horario horario, DateOnly fecha, string[] codigosDias)
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