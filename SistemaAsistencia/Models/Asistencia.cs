namespace SistemaAsistencia.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public int IdTrabajador { get; set; }
        public DateOnly Fecha { get; set; }

        public DateTime? HoraEntrada { get; set; }
        public DateTime? HoraSalida { get; set; }

        // Estados: PUNTUAL | A_TIEMPO | TARDANZA | FALTA | SIN_HORARIO | SIN_REGISTRO
        public string EstadoEntrada { get; set; } = "SIN_REGISTRO";
        // Estados: REGISTRADA | SALIDA_ANTICIPADA | SALIDA_NO_REGISTRADA | PENDIENTE
        public string EstadoSalida { get; set; } = "PENDIENTE";

        public short MinutosTardanza { get; set; } = 0;
        public short MinutosSalidaAnticipada { get; set; } = 0;

        // Override de horario para ese día (admin puede cambiar solo un día)
        public TimeOnly? HoraEntradaProgramada { get; set; }
        public TimeOnly? HoraSalidaProgramada { get; set; }

        // Corrección admin
        public bool CorregidoPorAdmin { get; set; } = false;
        public int? IdAdminCorrector { get; set; }
        public DateTime? FechaCorreccion { get; set; }
        public string? Observacion { get; set; }

        // Navegación
        public Trabajador? Trabajador { get; set; }
    }
}