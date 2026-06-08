using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfertasController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public OfertasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // 1. PUBLICAR UNA OFERTA P2P
        [HttpPost("crear")]
        public async Task<IActionResult> CrearOferta([FromBody] OfertaCreateDTO model)
        {
            if (model.MontoOrigen <= 0 || model.TasaCambio <= 0)
            {
                return BadRequest(new { message = "El monto y la tasa de cambio deben ser mayores a cero." });
            }

            if (model.MonedaOrigenId == model.MonedaDestinoId)
            {
                return BadRequest(new { message = "La moneda de origen y destino no pueden ser iguales." });
            }

            // Verificar si el usuario tiene la billetera de la moneda que ofrece
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaOrigenId);

            if (billetera == null || billetera.SaldoDisponible < model.MontoOrigen)
            {
                return BadRequest(new { message = "No tienes suficiente saldo disponible en tu billetera para publicar esta oferta." });
            }

            // REGLA DE NEGOCIO: Bloquear los fondos en la billetera
            billetera.SaldoDisponible -= model.MontoOrigen;
            billetera.SaldoBloqueado += model.MontoOrigen;

            // Mapear a la entidad de la base de datos (Ofertas)
            var nuevaOferta = new Ofertas
            {
                UsuarioId = model.UsuarioId,
                MonedaOrigenId = model.MonedaOrigenId,
                MonedaDestinoId = model.MonedaDestinoId,
                MontoOrigen = model.MontoOrigen,
                TasaCambio = model.TasaCambio,
                // Nota: Si tu tabla física no tiene el campo "MontoDestino", el cálculo se hace al vuelo en los GETs.
                // Si tu tabla lo tiene, descomenta la siguiente línea:
                // MontoDestino = model.MontoOrigen * model.TasaCambio,
                Estado = "DISPONIBLE",
                FechaPublicacion = DateTime.Now
                // Puedes concatenar la descripción opcional en algún campo de texto si tu tabla lo permite
            };

            _context.Ofertas.Add(nuevaOferta);
            await _context.SaveChangesAsync();

            // Cálculo instantáneo para la respuesta
            decimal montoRecibir = model.MontoOrigen * model.TasaCambio;

            return Ok(new
            {
                message = "Oferta publicada con éxito en el mercado P2P. Tus fondos han sido congelados en garantía.",
                OfertaId = nuevaOferta.Id,
                MontoARecibir = montoRecibir
            });
        }

        // 2. CANCELAR UNA OFERTA (Devuelve el dinero bloqueado al disponible)
        [HttpPost("cancelar/{id}")]
        public async Task<IActionResult> CancelarOferta(int id)
        {
            var oferta = await _context.Ofertas.FindAsync(id);

            if (oferta == null) return NotFound(new { message = "La oferta no existe." });

            if (oferta.Estado != "DISPONIBLE")
            {
                return BadRequest(new { message = $"No se puede cancelar esta oferta porque su estado actual es: {oferta.Estado}." });
            }

            // Buscar la billetera de origen del usuario para devolverle los fondos
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == oferta.UsuarioId && b.MonedaId == oferta.MonedaOrigenId);

            if (billetera != null)
            {
                // REGLA DE NEGOCIO INVERSA: Liberar los fondos bloqueados
                billetera.SaldoBloqueado -= oferta.MontoOrigen;
                billetera.SaldoDisponible += oferta.MontoOrigen;
            }

            // Actualizar estado de la oferta
            oferta.Estado = "CANCELADO";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Oferta cancelada con éxito. Los fondos congelados han regresado a tu saldo disponible." });
        }

        // 3. VER MERCADO P2P (Optimizado para el frontend: Cero consultas extra)
        [HttpGet("mercado")]
        public async Task<IActionResult> ObtenerMercadoP2P()
        {
            var ofertasActivas = await _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o => o.Estado == "DISPONIBLE")
                .Select(o => new
                {
                    o.Id,
                    CreadorId = o.UsuarioId,
                    CreadorNombre = o.Usuario.NombreCompleto, // Nombre de quien la crea

                    MonedaOrigenId = o.MonedaOrigenId,
                    MonedaOrigenCodigo = o.MonedaOrigen.CodigoIso,
                    MonedaOrigenSimbolo = o.MonedaOrigen.Simbolo,
                    MontoOfrecido = o.MontoOrigen, // Lo que tiene el usuario

                    MonedaDestinoId = o.MonedaDestinoId,
                    MonedaDestinoCodigo = o.MonedaDestino.CodigoIso,
                    MonedaDestinoSimbolo = o.MonedaDestino.Simbolo,

                    TasaCambio = o.TasaCambio, // Tipo de cambio establecido por el creador

                    MontoARecibir = o.MontoOrigen * o.TasaCambio,

                    o.FechaPublicacion
                })
                .OrderByDescending(o => o.FechaPublicacion)
                .ToListAsync();

            return Ok(ofertasActivas);
        }
    }
}