namespace SistemaAsistencia.Models
{
    public class Administrador
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? Nombre { get; set; }
        public bool Estado { get; set; } = true;
    }
}