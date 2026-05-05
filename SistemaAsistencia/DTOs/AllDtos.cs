namespace SistemaAsistencia.DTOs
{
    // AUTH
    public class LoginAdminDto { public string Username { get; set; } = null!; public string Password { get; set; } = null!; }
    public class LoginTrabajadorDto { public string Dni { get; set; } = null!; }
    public class LoginResponseDto { public string Token { get; set; } = null!; public string Nombre { get; set; } = null!; public string Rol { get; set; } = null!; public int Id { get; set; } public string? Cargo { get; set; } public string? Area { get; set; } }

    // TRABAJADORES
    public class TrabajadorDto { public int Id { get; set; } public string Dni { get; set; } = null!; public string Nombres { get; set; } = null!; public string Apellidos { get; set; } = null!; public string? Correo { get; set; } public string? Telefono { get; set; } public string? FotoUrl { get; set; } public int? IdArea { get; set; } public string? Area { get; set; } public int? IdCargo { get; set; } public string? Cargo { get; set; } public bool Estado { get; set; } }
    public class CrearTrabajadorDto { public string Dni { get; set; } = null!; public string Nombres { get; set; } = null!; public string Apellidos { get; set; } = null!; public string? Correo { get; set; } public string? Telefono { get; set; } public string? FotoUrl { get; set; } public int? IdArea { get; set; } public int? IdCargo { get; set; } public bool Estado { get; set; } = true; }

    // ASISTENCIAS
    public class MarcarAsistenciaDto { public string Dni { get; set; } = null!; public string Tipo { get; set; } = null!; }
    public class ResultadoMarcadoDto { public bool Exito { get; set; } public string Mensaje { get; set; } = null!; public string? NombreTrabajador { get; set; } public string? Estado { get; set; } public string? Hora { get; set; } public string? FotoUrl { get; set; } }
    public class AsistenciaDto { public int Id { get; set; } public int IdTrabajador { get; set; } public string NombreTrabajador { get; set; } = null!; public string Fecha { get; set; } = null!; public string? HoraEntrada { get; set; } public string? HoraSalida { get; set; } public string EstadoEntrada { get; set; } = null!; public string EstadoSalida { get; set; } = null!; public int MinutosTardanza { get; set; } public int MinutosSalidaAnticipada { get; set; } public bool CorregidoPorAdmin { get; set; } public string? Observacion { get; set; } }
    public class CorregirAsistenciaDto { public string? HoraEntrada { get; set; } public string? HoraSalida { get; set; } public string? EstadoEntrada { get; set; } public string? EstadoSalida { get; set; } public string? Observacion { get; set; } }
    public class CrearAsistenciaManualDto { public int IdTrabajador { get; set; } public string Fecha { get; set; } = null!; public string EstadoEntrada { get; set; } = "VACACIONES"; public string? EstadoSalida { get; set; } public string? Observacion { get; set; } }

    // SEMANA
    public class DiaSemanaDto { public string Fecha { get; set; } = null!; public string DiaSemana { get; set; } = null!; public string? HoraEntrada { get; set; } public string? HoraSalida { get; set; } public string EstadoEntrada { get; set; } = null!; public string EstadoSalida { get; set; } = null!; public int MinutosTardanza { get; set; } public int MinutosSalidaAnticipada { get; set; } public bool EsDescanso { get; set; } public bool EsHoy { get; set; } }

    // CALENDARIO
    public class CalendarioDiaDto { public string Fecha { get; set; } = null!; public string DiaSemana { get; set; } = null!; public string Estado { get; set; } = null!; public string? HoraEntrada { get; set; } public string? HoraSalida { get; set; } public string? HorarioEntrada { get; set; } public string? HorarioSalida { get; set; } public string? EstadoSalida { get; set; } public int MinutosTardanza { get; set; } public int MinutosSalidaAnticipada { get; set; } public bool CorregidoPorAdmin { get; set; } public string? Observacion { get; set; } }
    public class CalendarioMesDto { public int Mes { get; set; } public int Anio { get; set; } public string NombreTrabajador { get; set; } = null!; public string? Cargo { get; set; } public string? Area { get; set; } public string? FotoUrl { get; set; } public List<CalendarioDiaDto> Dias { get; set; } = new(); public int TotalPuntuales { get; set; } public int TotalATiempo { get; set; } public int TotalTardanzas { get; set; } public int TotalFaltas { get; set; } public int TotalSalidaNoRegistrada { get; set; } public int TotalDescansos { get; set; } public int TotalMinutosTardanza { get; set; } public int TotalMinutosSalidaAnticipada { get; set; } }

    // HORARIOS
    public class HorarioDto { public int Id { get; set; } public int IdTrabajador { get; set; } public int? IdPlantilla { get; set; } public string? NombrePlantilla { get; set; } public string HoraEntrada { get; set; } = null!; public string HoraSalida { get; set; } = null!; public int ToleranciaMinutos { get; set; } public string? DiasTrabajo { get; set; } public byte? DiasTrabajoCiclo { get; set; } public byte? DiasDescansoCiclo { get; set; } public string FechaInicio { get; set; } = null!; public string? FechaFin { get; set; } }
    public class CrearHorarioDto { public int IdTrabajador { get; set; } public int? IdPlantilla { get; set; } public string HoraEntrada { get; set; } = null!; public string HoraSalida { get; set; } = null!; public int ToleranciaMinutos { get; set; } = 5; public string? DiasTrabajo { get; set; } public byte? DiasTrabajoCiclo { get; set; } public byte? DiasDescansoCiclo { get; set; } public string FechaInicio { get; set; } = null!; public string? FechaFin { get; set; } public bool CerrarHorarioAnterior { get; set; } = true; }

    // DESCANSOS
    public class CrearDescansoDto { public int IdTrabajador { get; set; } public string Fecha { get; set; } = null!; public string Motivo { get; set; } = "PROGRAMADO"; public string? Observacion { get; set; } }
    public class CrearDescansoRangoDto { public int IdTrabajador { get; set; } public string FechaInicio { get; set; } = null!; public string FechaFin { get; set; } = null!; public string Motivo { get; set; } = "PROGRAMADO"; public string? Observacion { get; set; } }

    // AMONESTACIONES
    public class CrearAmonestacionDto { public int IdTrabajador { get; set; } public string Tipo { get; set; } = null!; public string Motivo { get; set; } = null!; }
    public class AmonestacionDto { public int Id { get; set; } public int IdTrabajador { get; set; } public string NombreTrabajador { get; set; } = null!; public string Tipo { get; set; } = null!; public string Motivo { get; set; } = null!; public string FechaEmision { get; set; } = null!; public byte DiasSuspension { get; set; } public bool CorreoEnviado { get; set; } }

    // CATÁLOGOS
    public class AreaDto { public int Id { get; set; } public string Nombre { get; set; } = null!; public string? Descripcion { get; set; } }
    public class CargoDto { public int Id { get; set; } public string Nombre { get; set; } = null!; public string? Descripcion { get; set; } }
    public class PlantillaTurnoDto { public int Id { get; set; } public string Nombre { get; set; } = null!; public string? Descripcion { get; set; } public byte HorasTurno { get; set; } public byte DiasTrabajoCiclo { get; set; } public byte DiasDescansoCiclo { get; set; } public string? DiasSemanaFijos { get; set; } }
}