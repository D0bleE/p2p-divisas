using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Services
{
    public class BilleteraService : IBilleteraService
    {
        private readonly CambioDivisasP2PContext _context;

        public BilleteraService(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<bool>> RecargarFondosAsync(BilleteraOperacionDTO model)
        {
            // 1. Validaciones de negocio
            if (model.Monto <= 0)
                return new ServiceResult<bool> { Success = false, Message = "El monto a recargar debe ser mayor a cero." };

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == model.UsuarioId);
            var monedaExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaId && m.Activo == true);

            if (!usuarioExiste || !monedaExiste)
                return new ServiceResult<bool> { Success = false, Message = "Usuario o Moneda no válidos." };

            // 2. Buscar o crear la billetera interna
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaId);

            if (billetera == null)
            {
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
                billetera.SaldoDisponible += model.Monto;
            }

            await _context.SaveChangesAsync();

            return new ServiceResult<bool>
            {
                Success = true,
                Message = "¡Recarga simulada con éxito! Fondos agregados a tu saldo disponible."
            };
        }

        public async Task<List<BilleteraSaldoDTO>> ObtenerSaldosUsuarioAsync(int usuarioId)
        {
            return await _context.Billeteras
                .Include(b => b.Moneda)
                .Where(b => b.UsuarioId == usuarioId)
                .Select(b => new BilleteraSaldoDTO
                {
                    MonedaCodigo = b.Moneda.CodigoIso,
                    MonedaNombre = b.Moneda.Nombre,
                    MonedaSimbolo = b.Moneda.Simbolo,
                    MonedaBandera = b.Moneda.RutaBandera,
                    SaldoDisponible = b.SaldoDisponible,
                    SaldoBloqueado = b.SaldoBloqueado
                }).ToListAsync();
        }
    }
}