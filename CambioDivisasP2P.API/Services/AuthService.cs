using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;
using BCrypt.Net;

namespace CambioDivisasP2P.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly CambioDivisasP2PContext _context;

        public AuthService(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<bool>> RegisterAsync(UsuarioRegistroDTO model)
        {
            // 1. Validar que los campos no vengan vacíos
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) ||
                string.IsNullOrEmpty(model.NombreCompleto) || string.IsNullOrEmpty(model.ConfirmarPassword))
            {
                return new ServiceResult<bool> { Success = false, Message = "Todos los campos son obligatorios." };
            }

            // 2. Verificar que las contraseñas sean idénticas
            if (model.Password != model.ConfirmarPassword)
            {
                return new ServiceResult<bool> { Success = false, Message = "Las contraseñas no coinciden." };
            }

            // 3. Validar si el correo ya existe en la base de datos
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (existeUsuario)
            {
                return new ServiceResult<bool> { Success = false, Message = "El correo electrónico ya está registrado." };
            }

            // 4. Buscar el ID del rol 'USU' para asignarlo por defecto
            var rolUsuario = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "USU");
            if (rolUsuario == null)
            {
                return new ServiceResult<bool> { Success = false, Message = "Error: El rol 'USU' no está configurado en la base de datos." };
            }

            // 5. Encriptar la contraseña usando BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // 6. Crear el nuevo objeto de usuario (ponemos "USU" en el campo string Rol para evitar conflictos)
            var nuevoUsuario = new Usuarios
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                PasswordHash = passwordHash,
                RolId = rolUsuario.Id,
                Rol = "USU",
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return new ServiceResult<bool> { Success = true, Message = "¡Cuenta creada con éxito! Ya puedes iniciar sesión." };
        }

        public async Task<ServiceResult<UsuarioDTO>> LoginAsync(LoginDTO model)
        {
            // 1. Buscar al usuario por su correo electrónico
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null)
            {
                return new ServiceResult<UsuarioDTO> { Success = false, Message = "Unauthorized: El correo electrónico o la contraseña son incorrectos." };
            }

            // 2. Verificar si la cuenta está desactivada
            if (usuario.Activo == false)
            {
                return new ServiceResult<UsuarioDTO> { Success = false, Message = "BadRequest: Esta cuenta se encuentra suspendida." };
            }

            // 3. Verificar si la contraseña coincide con el Hash
            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash);
            if (!esPasswordValido)
            {
                return new ServiceResult<UsuarioDTO> { Success = false, Message = "Unauthorized: El correo electrónico o la contraseña son incorrectos." };
            }

            // 4. Obtener el nombre del rol desde la tabla Roles usando el RolId del usuario
            var rol = await _context.Roles.FindAsync(usuario.RolId);
            string nombreRol = rol?.Nombre ?? "USU";

            // 5. Mapear al DTO de respuesta
            var respuestaDto = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = nombreRol,
                Token = "TOKEN_SIMULADO_SESION_LOCAL_VALIDA_12345XYZ"
            };

            return new ServiceResult<UsuarioDTO> { Success = true, Data = respuestaDto };
        }
    }
}