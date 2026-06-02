using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BilleterasController : ControllerBase
    {
        private readonly IBilleteraService _billeteraService;

        // Inyectamos la interfaz del servicio de billeteras
        public BilleterasController(IBilleteraService billeteraService)
        {
            _billeteraService = billeteraService;
        }

        // 1. ENDPOINT: SIMULAR RECARGA DE FONDOS
        [HttpPost("recargar")]
        public async Task<IActionResult> Recargar([FromBody] BilleteraOperacionDTO model)
        {
            var result = await _billeteraService.RecargarFondosAsync(model);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // 2. ENDPOINT: VER SALDOS DETALLADOS DEL USUARIO
        [HttpGet("saldos/{usuarioId}")]
        public async Task<IActionResult> ObtenerSaldos(int usuarioId)
        {
            var saldos = await _billeteraService.ObtenerSaldosUsuarioAsync(usuarioId);
            return Ok(saldos);
        }
    }
}