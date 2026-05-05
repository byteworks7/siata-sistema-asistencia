namespace SistemaAsistencia.Models
{
    public class Amonestacion
    {
        public int Id { get; set; }
        public int IdTrabajador { get; set; }
        // AVISO_ESCRITO | SUSPENSION_1D | SUSPENSION_2D
        public string Tipo { get; set; } = null!;
        public string Motivo { get; set; } = null!;
        public DateOnly FechaEmision { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public byte DiasSuspension { get; set; } = 0;
        public bool CorreoEnviado { get; set; } = false;
        public DateTime? FechaCorreo { get; set; }
        public int CreadoPor { get; set; }
        public DateTime CreadoEn { get; set; } = DateTime.Now;

        public Trabajador? Trabajador { get; set; }
    }
}