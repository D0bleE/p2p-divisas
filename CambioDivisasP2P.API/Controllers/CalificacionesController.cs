using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalificacionesController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public CalificacionesController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // POST: api/Calificaciones
        [HttpPost]
        public async Task<IActionResult> CalificarOperacion([FromBody] CalificacionDTO model)
        {
            // 1. Validar que la oferta exista
            var oferta = await _context.Ofertas.FindAsync(model.OfertaId);
            if (oferta == null)
            {
                return NotFound(new { message = "La oferta especificada no existe." });
            }

            // 2. REGLA: Solo se califican ofertas COMPLETADAS
            if (oferta.Estado != "COMPLETADO")
            {
                return BadRequest(new { message = "Solo puedes calificar una operación que haya sido completada con éxito." });
            }

            // 3. REGLA UNIDIRECCIONAL: Validar que el evaluador sea SÍ O SÍ el comprador de la oferta
            if (!oferta.UsuarioCompradorId.HasValue || oferta.UsuarioCompradorId.Value != model.UsuarioEvaluadorId)
            {
                return BadRequest(new { message = "No tienes permisos para calificar esta operación. Solo el usuario que aceptó/compró la oferta puede emitir una calificación." });
            }

            // 4. EL EVALUADO SIEMPRE SERÁ EL CREADOR/OFERTANTE
            int usuarioEvaluadoId = oferta.UsuarioId;

            // 5. REGLA DE SEGURIDAD EXTRA: Evitar que el ofertante intente trucar el sistema (doble check)
            if (model.UsuarioEvaluadorId == usuarioEvaluadoId)
            {
                return BadRequest(new { message = "Operación inválida: El creador de la oferta no puede auto-calificarse." });
            }

            // 6. REGLA: Única Calificación (Evitar que el comprador califique la misma oferta más de una vez)
            var calificacionExistente = await _context.Calificaciones
                .AnyAsync(c => c.OfertaId == model.OfertaId);

            if (calificacionExistente)
            {
                return BadRequest(new { message = "Esta oferta ya ha sido calificada anteriormente por el comprador." });
            }

            // Mapear el DTO a la Entidad Física de la Base de Datos
            var nuevaCalificacion = new Calificaciones
            {
                OfertaId = model.OfertaId,
                UsuarioEvaluadorId = model.UsuarioEvaluadorId,
                UsuarioEvaluadoId = usuarioEvaluadoId,
                Puntuacion = (int)model.Puntuacion, // Conversión a entero para la base de datos
                Comentario = string.IsNullOrWhiteSpace(model.Comentario) ? "Sin comentarios." : model.Comentario,
                Fecha = DateTime.Now
            };

            _context.Calificaciones.Add(nuevaCalificacion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "¡Calificación registrada con éxito! Tu opinión ayuda a la reputación del ofertante en el mercado P2P.",
                detalle = new
                {
                    OfertaId = nuevaCalificacion.OfertaId,
                    CompradorId = nuevaCalificacion.UsuarioEvaluadorId,
                    OfertanteId = nuevaCalificacion.UsuarioEvaluadoId,
                    Estrellas = model.Puntuacion,
                    Comentario = nuevaCalificacion.Comentario,
                    Fecha = nuevaCalificacion.Fecha
                }
            });
        }

        // GET: api/Calificaciones/Ofertante/5/Reputacion
        [HttpGet("Ofertante/{usuarioId}/Reputacion")]
        public async Task<IActionResult> ObtenerReputacionOfertante(int usuarioId)
        {
            // 1. Validar primero si el usuario existe en el sistema
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste)
            {
                return NotFound(new { message = "El usuario especificado no existe." });
            }

            // 2. Obtener todas las calificaciones donde este usuario fue el EVALUADO (ofertante)
            var calificaciones = _context.Calificaciones.Where(c => c.UsuarioEvaluadoId == usuarioId);

            int totalCalificaciones = await calificaciones.CountAsync();

            // 3. Si nadie lo ha calificado aún, le damos 0 estrellas por defecto de forma limpia
            if (totalCalificaciones == 0)
            {
                return Ok(new ReputacionUsuarioDTO
                {
                    UsuarioId = usuarioId,
                    PromedioPuntuacion = 0.0m,
                    TotalCalificaciones = 0
                });
            }

            // 4. Calcular el promedio matemático (Redondeado a 1 decimal, ej: 4.675 -> 4.7)
            double promedioRaw = await calificaciones.AverageAsync(c => c.Puntuacion);
            decimal promedioRedondeado = Math.Round((decimal)promedioRaw, 1);

            var reputacion = new ReputacionUsuarioDTO
            {
                UsuarioId = usuarioId,
                PromedioPuntuacion = promedioRedondeado,
                TotalCalificaciones = totalCalificaciones
            };

            return Ok(reputacion);
        }
    }
}