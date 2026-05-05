namespace SistemaAsistencia.Models
{
    public class PlantillaTurno
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public byte HorasTurno { get; set; }
        public byte DiasTrabajoCiclo { get; set; }
        public byte DiasDescansoCiclo { get; set; }
        public string? DiasSemanaFijos { get; set; }  // "L,M,X,J,V" o null si rotativo
        public bool Activo { get; set; } = true;

        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
    }
}