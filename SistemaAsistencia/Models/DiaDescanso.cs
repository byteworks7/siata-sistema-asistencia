namespace SistemaAsistencia.Models
{
    public class DiaDescanso
    {
        public int Id { get; set; }
        public int IdTrabajador { get; set; }
        public DateOnly Fecha { get; set; }
        // PROGRAMADO | COMPENSATORIO | FERIADO | HORAS_EXTRAS
        public string Motivo { get; set; } = "PROGRAMADO";
        public string? Observacion { get; set; }
        public int? CreadoPor { get; set; }
        public DateTime CreadoEn { get; set; } = DateTime.Now;

        public Trabajador? Trabajador { get; set; }
    }
}