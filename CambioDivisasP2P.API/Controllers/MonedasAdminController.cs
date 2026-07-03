using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonedasAdminController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public MonedasAdminController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMonedas()
        {
            var monedas = await _context.Monedas
                .OrderBy(m => m.CodigoIso)
                .Select(m => new
                {
                    m.Id,
                    m.CodigoIso,
                    m.Nombre,
                    m.Simbolo,
                    m.RutaBandera,
                    m.Activo
                })
                .ToListAsync();

            return Ok(monedas);
        }

        [HttpPost("cambiar-estado/{id}")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var moneda = await _context.Monedas.FindAsync(id);

            if (moneda == null)
                return NotFound(new { message = "Moneda no encontrada." });

            moneda.Activo = !(moneda.Activo ?? false);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = moneda.Activo == true
                    ? "Moneda activada correctamente."
                    : "Moneda deshabilitada correctamente.",
                activo = moneda.Activo
            });
        }
        [HttpPost("crear")]
        public async Task<IActionResult> CrearMoneda(
    [FromForm] string codigoIso,
    [FromForm] string nombre,
    [FromForm] string simbolo,
    [FromForm] bool activo,
    [FromForm] IFormFile bandera)
        {
            if (string.IsNullOrWhiteSpace(codigoIso) ||
                string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(simbolo))
                return BadRequest(new { message = "Todos los campos son obligatorios." });

            codigoIso = codigoIso.ToUpper().Trim();

            var existe = await _context.Monedas.AnyAsync(m => m.CodigoIso == codigoIso);

            if (existe)
                return BadRequest(new { message = "Ya existe una moneda con ese código ISO." });

            if (bandera == null || bandera.Length == 0)
                return BadRequest(new { message = "Debes subir una bandera." });

            var extension = Path.GetExtension(bandera.FileName).ToLower();

            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                return BadRequest(new { message = "Solo se permiten imágenes JPG o PNG." });

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "banderas");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{codigoIso.ToLower()}_{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await bandera.CopyToAsync(stream);
            }

            var nuevaMoneda = new Monedas
            {
                CodigoIso = codigoIso,
                Nombre = nombre,
                Simbolo = simbolo,
                RutaBandera = $"/imagenes/banderas/{nombreArchivo}",
                Activo = activo
            };

            _context.Monedas.Add(nuevaMoneda);
            await _context.SaveChangesAsync();

            var usuarios = await _context.Usuarios.ToListAsync();

            foreach (var usuario in usuarios)
            {
                _context.Billeteras.Add(new Billeteras
                {
                    UsuarioId = usuario.Id,
                    MonedaId = nuevaMoneda.Id,
                    SaldoDisponible = 0,
                    SaldoBloqueado = 0
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Moneda creada correctamente y billeteras generadas para los usuarios." });
        }

    }
}