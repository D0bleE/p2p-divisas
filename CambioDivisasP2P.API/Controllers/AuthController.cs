using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // 1. ENDPOINT: REGISTRO DE USUARIOS
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegistroDTO model)
        {
            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // 2. ENDPOINT: INICIO DE SESIÓN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            var result = await _authService.LoginAsync(model);

            if (!result.Success)
            {
                if (result.Message.StartsWith("Unauthorized:"))
                {
                    return Unauthorized(new { message = result.Message.Replace("Unauthorized: ", "") });
                }
                return BadRequest(new { message = result.Message.Replace("BadRequest: ", "") });
            }

            return Ok(result.Data);
        }
    }
}