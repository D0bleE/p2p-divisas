using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities; // Ajusta si el Scaffold le puso otro nombre a esta carpeta
using CambioDivisasP2P.CORE.Core.DTOs;
using BCrypt.Net;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Reemplaza "CambioDivisasP2PContext" por el nombre exacto que generó tu Scaffold en la capa CORE
        private readonly CambioDivisasP2PContext _context;

        public AuthController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // 1. ENDPOINT: REGISTRO DE USUARIOS (US-01)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegistroDTO model)
        {
            // 1. Validar que los campos no vengan vacíos (Agregamos el nuevo campo aquí)
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) ||
                string.IsNullOrEmpty(model.NombreCompleto) || string.IsNullOrEmpty(model.ConfirmarPassword))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            // NUEVA VALIDACIÓN: Verificar que las contraseñas sean idénticas
            if (model.Password != model.ConfirmarPassword)
            {
                return BadRequest(new { message = "Las contraseñas no coinciden." });
            }

            // Validar si el correo ya existe en la base de datos
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (existeUsuario)
            {
                return BadRequest(new { message = "El correo electrónico ya está registrado." });
            }

            // Buscar el ID del rol 'USU' para asignarlo por defecto
            var rolUsuario = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "USU");
            if (rolUsuario == null)
            {
                return BadRequest(new { message = "Error: El rol 'USU' no está configurado en la base de datos." });
            }

            // Encriptar la contraseña usando la librería BCrypt que instalamos
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Crear el nuevo objeto de usuario para guardarlo
            var nuevoUsuario = new Usuarios
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                PasswordHash = passwordHash,
                RolId = rolUsuario.Id,
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(new { message = "¡Cuenta creada con éxito! Ya puedes iniciar sesión." });
        }

        // 2. ENDPOINT: INICIO DE SESIÓN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            // Buscar al usuario por su correo electrónico
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Si no existe el usuario
            if (usuario == null)
            {
                return Unauthorized(new { message = "El correo electrónico o la contraseña son incorrectos." });
            }

            // Verificar si la cuenta está desactivada (Baneo lógico)
            if (usuario.Activo == false)
            {
                return BadRequest(new { message = "Esta cuenta se encuentra suspendida." });
            }

            // Verificar si la contraseña coincide con el Hash guardado en la BD
            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash);
            if (!esPasswordValido)
            {
                return Unauthorized(new { message = "El correo electrónico o la contraseña son incorrectos." });
            }

            // Obtener el nombre del rol para el DTO
            var rol = await _context.Roles.FindAsync(usuario.RolId);
            string nombreRol = rol?.Nombre ?? "USU";

            // Mapear los datos al DTO unificado del profesor para responderle al cliente
            var respuestaDto = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = nombreRol,
                // Como es simulado localmente, devolvemos un token estático. 
                // En producción aquí se generaría un JWT real.
                Token = "TOKEN_SIMULADO_SESION_LOCAL_VALIDA_12345XYZ"
            };

            return Ok(respuestaDto);
        }
    }
}