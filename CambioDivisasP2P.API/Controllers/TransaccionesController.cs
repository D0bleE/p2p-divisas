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

        // US-06 INICIO DE TRANSACCIONES ENTRE USUARIOS
        [HttpPost("iniciar")]
        public async Task<IActionResult> IniciarTransaccion([FromBody] TransaccionDTO model)
        {
            var oferta = await _context.Ofertas
                .FirstOrDefaultAsync(o => o.Id == model.OfertaId);

            if (oferta == null)
            {
                return NotFound(new
                {
                    message = "La oferta no existe."
                });
            }

            if (oferta.Estado != "ACTIVA")
            {
                return BadRequest(new
                {
                    message = "La oferta ya no está disponible."
                });
            }

            if (oferta.UsuarioId == model.UsuarioContraparteId)
            {
                return BadRequest(new
                {
                    message = "No puedes aceptar tu propia oferta."
                });
            }

            var transaccion = new Transacciones
            {
                OfertaId = oferta.Id,
                UsuarioContraparteId = model.UsuarioContraparteId,

                MonedaOrigenId = oferta.MonedaOrigenId,
                MonedaDestinoId = oferta.MonedaDestinoId,

                MontoOrigen = oferta.MontoOrigen,
                MontoDestino = Math.Round(
                    oferta.MontoOrigen * oferta.TasaCambio, 2),

                TasaCambioPactada = oferta.TasaCambio,

                // CORREGIDO PARA SQL SERVER
                Estado = "PENDIENTE_PAGO",

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

        // US-07 VISUALIZAR ESTADO DE TRANSACCIONES
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerMisTransacciones(int usuarioId)
        {
            var transacciones = await _context.Transacciones
                .Where(t => t.UsuarioContraparteId == usuarioId)
                .Select(t => new
                {
                    t.Id,
                    t.OfertaId,

                    Estado = ObtenerEstadoLegible(t.Estado),

                    t.MontoOrigen,
                    t.MontoDestino,
                    t.FechaInicio,
                    t.FechaActualizacion
                })
                .ToListAsync();

            return Ok(transacciones);
        }

        // US-07 CAMBIAR ESTADO DE TRANSACCIÓN
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstado(
            int id,
            [FromBody] ActualizarEstadoDTO model)
        {
            var transaccion = await _context.Transacciones
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaccion == null)
            {
                return NotFound(new
                {
                    message = "Transacción no encontrada."
                });
            }

            var estadosPermitidos = new List<string>
            {
                "PENDIENTE_PAGO",
                "PAGO_REPORTADO",
                "COMPLETADA",
                "CANCELADA"
            };

            if (!estadosPermitidos.Contains(model.NuevoEstado))
            {
                return BadRequest(new
                {
                    message = "Estado inválido."
                });
            }

            transaccion.Estado = model.NuevoEstado;
            transaccion.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Estado actualizado correctamente.",
                nuevoEstado = ObtenerEstadoLegible(
                    transaccion.Estado)
            });
        }

        // MÉTODO AUXILIAR PARA MOSTRAR ESTADOS BONITOS
        private string ObtenerEstadoLegible(string estado)
        {
            return estado switch
            {
                "PENDIENTE_PAGO" => "Pendiente",
                "PAGO_REPORTADO" => "Pago Enviado",
                "COMPLETADA" => "Finalizada",
                "CANCELADA" => "Cancelada",
                _ => estado
            };
        }
    }
}