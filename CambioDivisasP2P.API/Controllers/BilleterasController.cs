using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BilleterasController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public BilleterasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        

        // 2. ENDPOINT: VER SALDOS DETALLADOS DEL USUARIO
        [HttpGet("saldos/{usuarioId}")]
        public async Task<IActionResult> ObtenerSaldos(int usuarioId)
        {
            string urlBase = $"{Request.Scheme}://{Request.Host}"; //CAMBIO2

            var saldos = await _context.Billeteras
                .Include(b => b.Moneda)
                .Where(b => b.UsuarioId == usuarioId)
                .Select(b => new BilleteraSaldoDTO
                {
                    MonedaId = b.MonedaId,  // CAMBIOOOOOOOOOO
                    MonedaActiva = b.Moneda.Activo ?? false,
                    MonedaCodigo = b.Moneda.CodigoIso,
                    MonedaNombre = b.Moneda.Nombre,
                    MonedaSimbolo = b.Moneda.Simbolo,
                    MonedaBandera = $"{urlBase}{b.Moneda.RutaBandera}",//CAMBIO2
                    SaldoDisponible = b.SaldoDisponible, // CORREGIDO
                    SaldoBloqueado = b.SaldoBloqueado    // CORREGIDO
                }).ToListAsync();

            return Ok(saldos);
    
        }



    }
}