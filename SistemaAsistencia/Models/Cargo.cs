namespace SistemaAsistencia.Models
{
    public class Cargo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;

        public ICollection<Trabajador> Trabajadores { get; set; } = new List<Trabajador>();
    }
}