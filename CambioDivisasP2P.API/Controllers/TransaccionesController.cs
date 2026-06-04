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

        // ENDPOINT: Historial de transacciones del usuario
        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorial(
            [FromQuery] int usuarioId,
            [FromQuery] string? q,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? estado,
            [FromQuery] string? sort = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Validar sesión mínima: verificar que el usuario exista
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste)
                return Unauthorized(new { message = "Usuario no autenticado o no existe." });

            var query = _context.Transacciones
                .Include(t => t.Oferta).ThenInclude(o => o.Usuario)
                .Include(t => t.UsuarioContraparte)
                .Include(t => t.MonedaOrigen)
                .Include(t => t.MonedaDestino)
                .AsQueryable();

            // El historial muestra las transacciones donde el usuario participó (como ofertante o contraparte)
            query = query.Where(t => t.UsuarioContraparteId == usuarioId || t.Oferta.UsuarioId == usuarioId);

            // Filtros de fecha
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.FechaInicio >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                // incluir todo el día final
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.FechaInicio <= endOfDay);
            }

            // Filtrar por estado si se proporciona
            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(t => t.Estado.ToUpper() == estado.ToUpper());
            }

            // Búsqueda rápida por id de transacción, id de oferta, o nombre de contraparte
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                // intentar parsear a id numérico
                if (int.TryParse(q, out var idSearch))
                {
                    query = query.Where(t => t.Id == idSearch || t.OfertaId == idSearch);
                }
                else
                {
                    var qLower = q.ToLower();
                    query = query.Where(t => t.UsuarioContraparte.NombreCompleto.ToLower().Contains(qLower)
                        || t.Oferta.Usuario.NombreCompleto.ToLower().Contains(qLower));
                }
            }

            // Ordenamiento por fecha
            bool desc = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase);
            query = desc ? query.OrderByDescending(t => t.FechaInicio) : query.OrderBy(t => t.FechaInicio);

            // Paginación
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    transaccionId = t.Id,
                    ofertaId = t.OfertaId,
                    fecha = t.FechaInicio,
                    estado = t.Estado,
                    montoOrigen = t.MontoOrigen,
                    monedaOrigen = t.MonedaOrigen.CodigoIso,
                    montoDestino = t.MontoDestino,
                    monedaDestino = t.MonedaDestino.CodigoIso,
                    tasaCambio = t.TasaCambioPactada,
                    contraparteId = (t.UsuarioContraparteId == usuarioId) ? t.Oferta.UsuarioId : t.UsuarioContraparteId,
                    contraparteNombre = (t.UsuarioContraparteId == usuarioId) ? t.Oferta.Usuario.NombreCompleto : t.UsuarioContraparte.NombreCompleto
                }).ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                items
            });
        }
    }
}