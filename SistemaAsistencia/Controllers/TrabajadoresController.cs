using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;
using SistemaAsistencia.DTOs;
using SistemaAsistencia.Models;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api/trabajadores")]
    [Authorize(Roles = "ADMIN")]
    public class TrabajadoresController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TrabajadoresController(AppDbContext db) => _db = db;

        // GET api/trabajadores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _db.Trabajadores
                .Include(t => t.Area)
                .Include(t => t.Cargo)
                .Select(t => new TrabajadorDto
                {
                    Id = t.Id,
                    Dni = t.Dni,
                    Nombres = t.Nombres,
                    Apellidos = t.Apellidos,
                    Correo = t.Correo,
                    Telefono = t.Telefono,
                    FotoUrl = t.FotoUrl,
                    IdArea = t.IdArea,
                    Area = t.Area != null ? t.Area.Nombre : null,
                    IdCargo = t.IdCargo,
                    Cargo = t.Cargo != null ? t.Cargo.Nombre : null,
                    Estado = t.Estado
                }).ToListAsync();

            return Ok(lista);
        }

        // GET api/trabajadores/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _db.Trabajadores
                .Include(x => x.Area)
                .Include(x => x.Cargo)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t == null) return NotFound(new { mensaje = "Trabajador no encontrado" });
            return Ok(MapDto(t));
        }

        // GET api/trabajadores/dni/{dni} — AllowAnonymous para el kiosco
        [AllowAnonymous]
        [HttpGet("dni/{dni}")]
        public async Task<IActionResult> GetByDni(string dni)
        {
            var t = await _db.Trabajadores
                .Include(x => x.Area)
                .Include(x => x.Cargo)
                .FirstOrDefaultAsync(x => x.Dni == dni && x.Estado);

            if (t == null) return NotFound(new { mensaje = "DNI no encontrado" });
            return Ok(MapDto(t));
        }

        // POST api/trabajadores
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearTrabajadorDto dto)
        {
            if (await _db.Trabajadores.AnyAsync(t => t.Dni == dto.Dni))
                return BadRequest(new { mensaje = "Ya existe un trabajador con ese DNI" });

            var trabajador = new Trabajador
            {
                Dni = dto.Dni,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                FotoUrl = dto.FotoUrl,
                IdArea = dto.IdArea,
                IdCargo = dto.IdCargo,
                Estado = dto.Estado
            };

            _db.Trabajadores.Add(trabajador);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Trabajador registrado", id = trabajador.Id });
        }

        // PUT api/trabajadores/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CrearTrabajadorDto dto)
        {
            var t = await _db.Trabajadores.FindAsync(id);
            if (t == null) return NotFound(new { mensaje = "Trabajador no encontrado" });

            t.Nombres = dto.Nombres;
            t.Apellidos = dto.Apellidos;
            t.Correo = dto.Correo;
            t.Telefono = dto.Telefono;
            t.FotoUrl = dto.FotoUrl;
            t.IdArea = dto.IdArea;
            t.IdCargo = dto.IdCargo;
            t.Estado = dto.Estado;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Trabajador actualizado" });
        }

        // DELETE api/trabajadores/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var t = await _db.Trabajadores.FindAsync(id);
            if (t == null) return NotFound(new { mensaje = "Trabajador no encontrado" });
            t.Estado = false;
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Trabajador desactivado" });
        }

        // POST api/trabajadores/{id}/foto
        [HttpPost("{id:int}/foto")]
        public async Task<IActionResult> GuardarFoto(int id, [FromBody] FotoDto dto)
        {
            var t = await _db.Trabajadores.FindAsync(id);
            if (t == null) return NotFound(new { mensaje = "Trabajador no encontrado" });
            t.FotoUrl = dto.FotoBase64;
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Foto guardada" });
        }

        private static TrabajadorDto MapDto(Trabajador t) => new()
        {
            Id = t.Id,
            Dni = t.Dni,
            Nombres = t.Nombres,
            Apellidos = t.Apellidos,
            Correo = t.Correo,
            Telefono = t.Telefono,
            FotoUrl = t.FotoUrl,
            IdArea = t.IdArea,
            Area = t.Area?.Nombre,
            IdCargo = t.IdCargo,
            Cargo = t.Cargo?.Nombre,
            Estado = t.Estado
        };
    }

    public class FotoDto
    {
        public string FotoBase64 { get; set; } = "";
    }
}