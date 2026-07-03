using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public UsuariosController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        [HttpGet("admin")]
        public async Task<IActionResult> ObtenerUsuariosAdmin()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.RolNavigation)
                .Select(u => new
                {
                    u.Id,
                    u.NombreCompleto,
                    u.Email,
                    u.FechaRegistro,
                    Activo = u.Activo ?? false,
                    Rol = u.RolNavigation.Nombre,

                    TotalMovimientos = _context.MovimientosFondos
                        .Count(m => m.UsuarioId == u.Id),

                    TotalOfertas = _context.Ofertas
                        .Count(o => o.UsuarioId == u.Id),

                    TotalOperacionesP2P = _context.Ofertas
                        .Count(o => o.UsuarioId == u.Id || o.UsuarioCompradorId == u.Id),

                    CalificacionPromedio = _context.Calificaciones
                        .Where(c => c.UsuarioEvaluadoId == u.Id)
                        .Average(c => (decimal?)c.Puntuacion) ?? 0,

                    TotalCalificaciones = _context.Calificaciones
                        .Count(c => c.UsuarioEvaluadoId == u.Id)
                })
                .OrderByDescending(u => u.FechaRegistro)
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpPost("cambiar-estado/{usuarioId}")]
        public async Task<IActionResult> CambiarEstadoUsuario(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            usuario.Activo = !(usuario.Activo ?? false);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = usuario.Activo == true
                    ? "Usuario activado correctamente."
                    : "Usuario deshabilitado correctamente.",
                activo = usuario.Activo
            });
        }
    }
}