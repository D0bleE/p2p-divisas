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

        // 1. ENDPOINT: SIMULAR RECARGA DE FONDOS
        [HttpPost("recargar")]
        public async Task<IActionResult> Recargar([FromBody] BilleteraOperacionDTO model)
        {
            if (model.Monto <= 0) return BadRequest(new { message = "El monto a recargar debe ser mayor a cero." });

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == model.UsuarioId);
            var monedaExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaId && m.Activo == true);
            if (!usuarioExiste || !monedaExiste) return BadRequest(new { message = "Usuario o Moneda no válidos." });

            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaId);

            if (billetera == null)
            {
                // CORREGIDO: Se inicializa con las nuevas columnas de custodia
                billetera = new Billeteras
                {
                    UsuarioId = model.UsuarioId,
                    MonedaId = model.MonedaId,
                    SaldoDisponible = model.Monto,
                    SaldoBloqueado = 0.00m
                };
                _context.Billeteras.Add(billetera);
            }
            else
            {
                // CORREGIDO: Las recargas externas suman al saldo disponible
                billetera.SaldoDisponible += model.Monto;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "¡Recarga simulada con éxito! Fondos agregados a tu saldo disponible." });
        }

        // 2. ENDPOINT: VER SALDOS DETALLADOS DEL USUARIO
        [HttpGet("saldos/{usuarioId}")]
        public async Task<IActionResult> ObtenerSaldos(int usuarioId)
        {
            var saldos = await _context.Billeteras
                .Include(b => b.Moneda)
                .Where(b => b.UsuarioId == usuarioId)
                .Select(b => new BilleteraSaldoDTO
                {
                    MonedaCodigo = b.Moneda.CodigoIso,
                    MonedaNombre = b.Moneda.Nombre,
                    MonedaSimbolo = b.Moneda.Simbolo,
                    MonedaBandera = b.Moneda.RutaBandera,
                    SaldoDisponible = b.SaldoDisponible, // CORREGIDO
                    SaldoBloqueado = b.SaldoBloqueado    // CORREGIDO
                }).ToListAsync();

            return Ok(saldos);
        }
    }
}