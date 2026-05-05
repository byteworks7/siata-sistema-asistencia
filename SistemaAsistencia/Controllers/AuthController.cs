using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SistemaAsistencia.Data;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ── POST /api/auth/login-admin ────────────────────────────
        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminDto dto)
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { mensaje = "Complete todos los campos" });

            var admin = await _db.Administradores
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (admin == null)
                return Unauthorized(new { mensaje = "Credenciales incorrectas" });

            bool valido = BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash);
            if (!valido)
                return Unauthorized(new { mensaje = "Credenciales incorrectas" });

            var token = GenerarToken(admin.Id, admin.Nombre, "ADMIN");

            return Ok(new
            {
                token,
                nombre = admin.Nombre,
                rol = "ADMIN",
                id = admin.Id
            });
        }

        // ── POST /api/auth/login-trabajador ───────────────────────
        [HttpPost("login-trabajador")]
        public async Task<IActionResult> LoginTrabajador([FromBody] LoginTrabajadorDto dto)
        {
            if (string.IsNullOrEmpty(dto.Dni))
                return BadRequest(new { mensaje = "Ingrese el DNI" });

            var trabajador = await _db.Trabajadores
                .Include(t => t.Cargo)
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.Dni == dto.Dni);

            if (trabajador == null)
                return Unauthorized(new { mensaje = "DNI no encontrado" });

            var token = GenerarToken(
                trabajador.Id,
                $"{trabajador.Nombres} {trabajador.Apellidos}",
                "TRABAJADOR");

            return Ok(new
            {
                token,
                nombre = $"{trabajador.Nombres} {trabajador.Apellidos}",
                rol = "TRABAJADOR",
                id = trabajador.Id,
                cargo = trabajador.Cargo?.Nombre,
                area = trabajador.Area?.Nombre
            });
        }

        // ── POST /api/auth/generar-hash ───────────────────────────
        [HttpPost("generar-hash")]
        public IActionResult GenerarHash([FromBody] HashDto dto)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            return Ok(new { hash });
        }

        // ── Generar JWT ───────────────────────────────────────────
        private string GenerarToken(int id, string nombre, string rol)
        {
            var secret = _config["Jwt:Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id",              id.ToString()),
                new Claim(ClaimTypes.Name,   nombre),
                new Claim(ClaimTypes.Role,   rol),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginAdminDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginTrabajadorDto
    {
        public string Dni { get; set; } = "";
    }

    public class HashDto
    {
        public string Password { get; set; } = "";
    }
}