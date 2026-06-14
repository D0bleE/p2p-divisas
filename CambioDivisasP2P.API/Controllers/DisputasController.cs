using CambioDivisasP2P.CORE.Core.DTOs;
using CambioDivisasP2P.CORE.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DisputasController:ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public DisputasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // 1. Reportar una disputa
        [HttpPost("crear")]
        public async Task<IActionResult> CrearDisputa([FromBody] DisputaCreateDTO model)
        {
            var oferta = await _context.Ofertas.FindAsync(model.OfertaId);

            if (oferta == null)
            {
                return NotFound(new { message = "La oferta no existe." });
            }

            var disputa = new Disputas
            {
                OfertaId = model.OfertaId,
                UsuarioDemandanteId = model.UsuarioDemandanteId,
                Motivo = model.Motivo,
                Estado = "ABIERTA",
                FechaApertura = DateTime.Now
            };

            _context.Disputas.Add(disputa);

            // Cambiar estado de la oferta
            oferta.Estado = "EN_PROCESO";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Disputa registrada correctamente.",
                disputa.Id
            });
        }

        // 2. Ver todas las disputas
        [HttpGet]
        public async Task<IActionResult> ObtenerDisputas()
        {
            var disputas = await _context.Disputas
                .Include(d => d.UsuarioDemandante)
                .Include(d => d.Oferta)
                .Select(d => new
                {
                    d.Id,
                    d.OfertaId,
                    d.UsuarioDemandanteId,
                    Usuario = d.UsuarioDemandante.NombreCompleto,
                    d.Motivo,
                    d.Estado,
                    d.Resolucion,
                    d.FechaApertura,
                    d.FechaResolucion
                })
                .OrderByDescending(d => d.FechaApertura)
                .ToListAsync();

            return Ok(disputas);
        }

        // 3. Resolver disputa
        [HttpPost("resolver/{id}")]
        public async Task<IActionResult> ResolverDisputa(int id, [FromBody] DisputaResolverDTO model)
        {
            var disputa = await _context.Disputas.FindAsync(id);

            if (disputa == null)
            {
                return NotFound(new { message = "La disputa no existe." });
            }

            disputa.Estado = "RESUELTA";
            disputa.Resolucion = model.Resolucion;
            disputa.FechaResolucion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Disputa resuelta correctamente."
            });
        }
    }
}
