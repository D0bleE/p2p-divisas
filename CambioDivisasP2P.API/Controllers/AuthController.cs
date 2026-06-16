using BCrypt.Net;
using CambioDivisasP2P.CORE.Core.DTOs;
using CambioDivisasP2P.CORE.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;
        private readonly IConfiguration _configuration;

        // CORREGIDO: Ahora inyectamos correctamente tanto el Context como el Configuration
        public AuthController(CambioDivisasP2PContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. ENDPOINT: REGISTRO DE USUARIOS (US-01)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegistroDTO model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) ||
                string.IsNullOrEmpty(model.NombreCompleto) || string.IsNullOrEmpty(model.ConfirmarPassword))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            if (model.Password != model.ConfirmarPassword)
            {
                return BadRequest(new { message = "Las contraseñas no coinciden." });
            }

            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (existeUsuario)
            {
                return BadRequest(new { message = "El correo electrónico ya está registrado." });
            }

            var rolUsuario = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "USU");
            if (rolUsuario == null)
            {
                return BadRequest(new { message = "Error: El rol 'USU' no está configurado en la base de datos." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var nuevoUsuario = new Usuarios
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                PasswordHash = passwordHash,
                RolId = rolUsuario.Id,
                FechaRegistro = DateTime.Now,
                Activo = true
            };
            //CAMBIOOOOOOOOOOOOOOOOOOOO2
            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var monedasActivas = await _context.Monedas
                .Where(m => m.Activo == true)
                .ToListAsync();

            foreach (var moneda in monedasActivas)
            {
                _context.Billeteras.Add(new Billeteras
                {
                    UsuarioId = nuevoUsuario.Id,
                    MonedaId = moneda.Id,
                    SaldoDisponible = 0,
                    SaldoBloqueado = 0
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "¡Cuenta creada con éxito! Ya puedes iniciar sesión." });
        }

        // 2. ENDPOINT: INICIO DE SESIÓN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null)
                return Unauthorized(new { message = "El correo electrónico o la contraseña son incorrectos." });

            if (usuario.Activo == false)
                return BadRequest(new { message = "Esta cuenta se encuentra suspendida." });

            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash);
            if (!esPasswordValido)
                return Unauthorized(new { message = "El correo electrónico o la contraseña son incorrectos." });

            var rol = await _context.Roles.FindAsync(usuario.RolId);
            string nombreRol = rol?.Nombre ?? "USU";

            string tokenReal = GenerarJwtToken(usuario.Id.ToString(), usuario.Email, nombreRol);

            var respuestaDto = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = nombreRol,
                Token = tokenReal
            };

            return Ok(respuestaDto);
        }

        // MÉTODO PRIVADO PARA CREAR EL JWT
        private string GenerarJwtToken(string usuarioId, string email, string rol)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 