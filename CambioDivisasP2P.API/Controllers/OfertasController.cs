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

        // 1. ENDPOINT: PUBLICAR OFERTA CON RETENCIÓN EN GARANTÍA (ESCROW)
        [HttpPost("crear")]
        public async Task<IActionResult> CrearOferta([FromBody] OfertaCreateDTO model)
        {
            // Validaciones básicas de negocio
            if (model.MontoOrigen <= 0 || model.TasaCambio <= 0)
            {
                return BadRequest(new { message = "El monto y la tasa de cambio deben ser mayores a cero." });
            }

            if (model.MonedaOrigenId == model.MonedaDestinoId)
            {
                return BadRequest(new { message = "No puedes intercambiar una moneda por sí misma." });
            }

            // Verificar que existan las monedas seleccionadas
            var monedaOrigenExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaOrigenId && m.Activo == true);
            var monedaDestinoExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaDestinoId && m.Activo == true);
            if (!monedaOrigenExiste || !monedaDestinoExiste)
            {
                return BadRequest(new { message = "Una o ambas monedas seleccionadas no son válidas." });
            }

            // BUSCAR LA BILLETERA INTERNA DEL USUARIO PARA VALIDAR SU SALDO DISPONIBLE
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaOrigenId);

            if (billetera == null || billetera.SaldoDisponible < model.MontoOrigen)
            {
                return BadRequest(new { message = "No cuentas con saldo disponible suficiente en tu billetera interna para respaldar esta oferta." });
            }

            // APLICAR LÓGICA ESCROW: Congelar los fondos del usuario
            billetera.SaldoDisponible -= model.MontoOrigen;
            billetera.SaldoBloqueado += model.MontoOrigen;

            // Registrar la oferta en la base de datos
            var nuevaOferta = new Ofertas
            {
                UsuarioId = model.UsuarioId,
                MonedaOrigenId = model.MonedaOrigenId,
                MonedaDestinoId = model.MonedaDestinoId,
                MontoOrigen = model.MontoOrigen,
                TasaCambio = model.TasaCambio,
                Estado = "ACTIVA",
                FechaPublicacion = DateTime.Now
            };

            _context.Ofertas.Add(nuevaOferta);
            await _context.SaveChangesAsync();

            return Ok(new { message = "¡Oferta publicada exitosamente! Tus fondos han sido retenidos en garantía de forma segura.", ofertaId = nuevaOferta.Id });
        }

        // 2. ENDPOINT: OBTENER PIZARRA DE MERCADO (OFERTAS ACTIVAS)
        [HttpGet("pizarra")]
        public async Task<IActionResult> ObtenerPizarraMercado(string? moneda = null, decimal? monto = null)
        {
            var consulta = _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o => o.Estado == "ACTIVA")
                .AsQueryable();
            if (monto.HasValue)
            {
                consulta = consulta.Where(o => o.MontoOrigen >= monto.Value);
            }

            if (!string.IsNullOrWhiteSpace(moneda))
            {
                consulta = consulta.Where(o =>
                    o.MonedaOrigen.CodigoIso == moneda ||
                    o.MonedaDestino.CodigoIso == moneda);
            }

            var resultado = await consulta
                .Select(o => new OfertaDetalleDTO
                {
                    Id = o.Id,
                    UsuarioId = o.UsuarioId,
                    NombreUsuario = o.Usuario.NombreCompleto,

                    MonedaOrigenCodigo = o.MonedaOrigen.CodigoIso,
                    MonedaOrigenSimbolo = o.MonedaOrigen.Simbolo,
                    MonedaOrigenBandera = o.MonedaOrigen.RutaBandera,
                    MontoOrigen = o.MontoOrigen,

                    MonedaDestinoCodigo = o.MonedaDestino.CodigoIso,
                    MonedaDestinoSimbolo = o.MonedaDestino.Simbolo,
                    MonedaDestinoBandera = o.MonedaDestino.RutaBandera,

                    TasaCambio = o.TasaCambio,
                    MontoDestinoCalculado = Math.Round(o.MontoOrigen * o.TasaCambio, 2),

                    Estado = o.Estado,
                    FechaPublicacion = (DateTime)o.FechaPublicacion
                })
                .ToListAsync();

            return Ok(resultado);
        }

        [HttpPost("matching")]
        public async Task<IActionResult> BuscarMatching([FromBody] MatchingDTO model)
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
                    message = "La oferta no está disponible para matching."
                });
            }

            var ofertaCompatible = await _context.Ofertas
                .Where(o =>
                    o.Id != oferta.Id &&
                    o.Estado == "ACTIVA" &&
                    o.MonedaOrigenId == oferta.MonedaDestinoId &&
                    o.MonedaDestinoId == oferta.MonedaOrigenId &&
                    o.MontoOrigen == oferta.MontoOrigen)
                .OrderBy(o => o.FechaPublicacion)
                .FirstOrDefaultAsync();

            if (ofertaCompatible == null)
            {
                return Ok(new
                {
                    message = "No se encontraron ofertas compatibles."
                });
            }

            oferta.Estado = "EN_PROCESO";
            ofertaCompatible.Estado = "EN_PROCESO";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Oferta compatible encontrada.",
                ofertaId = oferta.Id,
                ofertaCompatibleId = ofertaCompatible.Id,
                notificacionOferta = "Tu oferta encontró una coincidencia.",
                notificacionContraparte = "Se encontró una oferta compatible para tu solicitud."
            });
        }

    }
}