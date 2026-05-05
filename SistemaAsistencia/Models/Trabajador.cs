namespace SistemaAsistencia.Models
{
    public class Trabajador
    {
        public int Id { get; set; }
        public string Dni { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? FotoUrl { get; set; }
        public int? IdArea { get; set; }
        public int? IdCargo { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        public Area? Area { get; set; }
        public Cargo? Cargo { get; set; }
        public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
        public ICollection<DiaDescanso> DiasDescanso { get; set; } = new List<DiaDescanso>();
        public ICollection<Amonestacion> Amonestaciones { get; set; } = new List<Amonestacion>();
    }
}