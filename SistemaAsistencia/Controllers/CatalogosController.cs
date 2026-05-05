using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Data;

namespace SistemaAsistencia.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Roles = "ADMIN")]
    public class CatalogosController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CatalogosController(AppDbContext db) => _db = db;

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var areas = await _db.Areas
                .Select(a => new { id = a.Id, nombre = a.Nombre })
                .ToListAsync();
            return Ok(areas);
        }

        [HttpPost("areas")]
        public async Task<IActionResult> CrearArea([FromBody] NombreDto dto)
        {
            var area = new Models.Area { Nombre = dto.Nombre };
            _db.Areas.Add(area);
            await _db.SaveChangesAsync();
            return Ok(new { id = area.Id, nombre = area.Nombre });
        }

        [HttpGet("cargos")]
        public async Task<IActionResult> GetCargos()
        {
            var cargos = await _db.Cargos
                .Select(c => new { id = c.Id, nombre = c.Nombre })
                .ToListAsync();
            return Ok(cargos);
        }

        [HttpPost("cargos")]
        public async Task<IActionResult> CrearCargo([FromBody] NombreDto dto)
        {
            var cargo = new Models.Cargo { Nombre = dto.Nombre };
            _db.Cargos.Add(cargo);
            await _db.SaveChangesAsync();
            return Ok(new { id = cargo.Id, nombre = cargo.Nombre });
        }
    }

    public class NombreDto
    {
        public string Nombre { get; set; } = "";
    }
}