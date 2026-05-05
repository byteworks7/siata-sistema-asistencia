namespace SistemaAsistencia.Models
{
    public class Horario
    {
        public int Id { get; set; }
        public int IdTrabajador { get; set; }
        public int? IdPlantilla { get; set; }
        public TimeOnly HoraEntrada { get; set; }
        public TimeOnly HoraSalida { get; set; }
        public byte ToleranciaMinutos { get; set; } = 5;
        // Días fijos: "L,M,X,J,V" — null si turno rotativo
        public string? DiasTrabajo { get; set; }
        // Ciclo rotativo: 2 trabaja / 2 descansa, etc.
        public byte? DiasTrabajoCiclo { get; set; }
        public byte? DiasDescansoCiclo { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public int? CreadoPor { get; set; }
        public DateTime CreadoEn { get; set; } = DateTime.Now;

        // Navegación
        public Trabajador? Trabajador { get; set; }
        public PlantillaTurno? Plantilla { get; set; }
    }
}