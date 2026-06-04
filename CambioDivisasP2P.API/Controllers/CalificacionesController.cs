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

        [HttpPost]
        public async Task<IActionResult> CrearCalificacion([FromBody] CalificacionDTO model)
        {
            // Validaciones basicas
            if (model.Puntuacion < 1 || model.Puntuacion > 5)
                return BadRequest(new { message = "La puntuación debe estar entre 1 y 5." });

            var transaccion = await _context.Transacciones
                .Include(t => t.Calificaciones)
                .FirstOrDefaultAsync(t => t.Id == model.TransaccionId);

            if (transaccion == null)
                return NotFound(new { message = "Transacción no encontrada." });

            // Solo transacciones completadas pueden ser calificadas
            var estadosPermitidos = new[] { "COMPLETADA" };
            if (!estadosPermitidos.Contains(transaccion.Estado?.ToUpper()))
                return BadRequest(new { message = "Solo se pueden calificar transacciones finalizadas." });

            // Evitar calificaciones duplicadas por la misma transaccion y evaluador
            var existe = transaccion.Calificaciones
                .Any(c => c.UsuarioEvaluadorId == model.UsuarioEvaluadorId);
            if (existe)
                return BadRequest(new { message = "Ya has calificado esta transacción." });

            var calificacion = new Calificaciones
            {
                TransaccionId = model.TransaccionId,
                UsuarioEvaluadorId = model.UsuarioEvaluadorId,
                UsuarioEvaluadoId = model.UsuarioEvaluadoId,
                Puntuacion = model.Puntuacion,
                Comentario = model.Comentario,
                Fecha = DateTime.Now
            };

            _context.Calificaciones.Add(calificacion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Calificación agregada correctamente." });
        }

        [HttpGet("transaccion/{transaccionId}")]
        public async Task<IActionResult> ObtenerCalificacionPorTransaccion(int transaccionId)
        {
            var califs = await _context.Calificaciones
                .Where(c => c.TransaccionId == transaccionId)
                .Select(c => new {
                    c.Id,
                    c.TransaccionId,
                    c.UsuarioEvaluadorId,
                    c.UsuarioEvaluadoId,
                    c.Puntuacion,
                    c.Comentario,
                    c.Fecha
                }).ToListAsync();

            return Ok(califs);
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerCalificacionesUsuario(int usuarioId)
        {
            var califs = await _context.Calificaciones
                .Where(c => c.UsuarioEvaluadoId == usuarioId)
                .Select(c => new {
                    c.Id,
                    c.TransaccionId,
                    c.UsuarioEvaluadorId,
                    c.Puntuacion,
                    c.Comentario,
                    c.Fecha
                }).ToListAsync();

            return Ok(califs);
        }
    }
}
