using Microsoft.AspNetCore.Mvc;
using CambioDivisasP2P.CORE.Core.Entities;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransaccionesController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public TransaccionesController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        [HttpPost("iniciar")]
        public async Task<IActionResult> IniciarTransaccion([FromBody] TransaccionDTO model)
        {
            var oferta = await _context.Ofertas
                .FirstOrDefaultAsync(o => o.Id == model.OfertaId);

            if (oferta == null)
            {
                return NotFound(new { message = "La oferta no existe." });
            }

            if (oferta.Estado != "ACTIVA")
            {
                return BadRequest(new { message = "La oferta ya no está disponible." });
            }

            if (oferta.UsuarioId == model.UsuarioContraparteId)
            {
                return BadRequest(new { message = "No puedes aceptar tu propia oferta." });
            }

            var transaccion = new Transacciones
            {
                OfertaId = oferta.Id,
                UsuarioContraparteId = model.UsuarioContraparteId,

                MonedaOrigenId = oferta.MonedaOrigenId,
                MonedaDestinoId = oferta.MonedaDestinoId,

                MontoOrigen = oferta.MontoOrigen,
                MontoDestino = Math.Round(oferta.MontoOrigen * oferta.TasaCambio, 2),

                TasaCambioPactada = oferta.TasaCambio,

                Estado = "PENDIENTE",

                FechaInicio = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            oferta.Estado = "EN_PROCESO";

            _context.Transacciones.Add(transaccion);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Transacción iniciada correctamente.",
                transaccionId = transaccion.Id
            });
        }
    }
}