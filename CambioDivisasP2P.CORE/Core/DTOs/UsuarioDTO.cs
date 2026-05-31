using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    // 1. DTO para responder al frontend cuando el usuario ya se logueó con éxito
    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!; // Para manejar la sesión segura más adelante
        public string Rol { get; set; } = null!;   // 'USU' o 'ADM'
    }

    // 2. DTO que recibe los datos exactos del formulario "Create Account" (Pág 3 del PDF)
    public class UsuarioRegistroDTO
    {
        public string NombreCompleto { get; set; } = null!; // Campo: Full Name
        public string Email { get; set; } = null!;          // Campo: E-mail
        public string Password { get; set; } = null!;       // Campo: Password
        public string ConfirmarPassword { get; set; } = null!;
    }

    // 3. DTO para el formulario de inicio de sesión
    public class LoginDTO
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}