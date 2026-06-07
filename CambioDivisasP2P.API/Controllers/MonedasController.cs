using CambioDivisasP2P.CORE.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonedasController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public MonedasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // GET: api/Monedas/activas
        // Este endpoint lo llamará Vue.js apenas cargue la página de recarga
        [HttpGet("activas")]
        public async Task<IActionResult> ObtenerMonedasActivas()
        {
            // Obtiene el dominio actual (ej: https://localhost:7120)
            string urlBase = $"{Request.Scheme}://{Request.Host}";

            var monedas = await _context.Monedas
                .Where(m => m.Activo == true)
                .Select(m => new
                {
                    m.Id,
                    m.Nombre,
                    m.CodigoIso,
                    m.Simbolo,
                    // 🔥 Combina el dominio con la ruta guardada en la BD
                    RutaBandera = $"{urlBase}{m.RutaBandera}"
                })
                .ToListAsync();

            return Ok(monedas);
        }
    }
}