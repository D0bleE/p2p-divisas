using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfertasController : ControllerBase
    {
        private readonly IOfertaService _ofertaService;

        // Inyectamos la interfaz del servicio en lugar de la base de datos directa
        public OfertasController(IOfertaService ofertaService)
        {
            _ofertaService = ofertaService;
        }

        // 1. ENDPOINT: PUBLICAR OFERTA
        [HttpPost("crear")]
        public async Task<IActionResult> CrearOferta([FromBody] OfertaCreateDTO model)
        {
            var result = await _ofertaService.CrearOfertaAsync(model);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message, ofertaId = result.Data });
        }

        // 2. ENDPOINT: OBTENER PIZARRA DE MERCADO
        [HttpGet("pizarra")]
        public async Task<IActionResult> ObtenerPizarraMercado()
        {
            var ofertas = await _ofertaService.ObtenerPizarraMercadoAsync();
            return Ok(ofertas);
        }
    }
}