using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.API.Interfaces;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Services
{
    public class OfertaService : IOfertaService
    {
        private readonly CambioDivisasP2PContext _context;

        public OfertaService(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<int>> CrearOfertaAsync(OfertaCreateDTO model)
        {
            // 1. Validaciones básicas de negocio
            if (model.MontoOrigen <= 0 || model.TasaCambio <= 0)
            {
                return new ServiceResult<int> { Success = false, Message = "El monto y la tasa de cambio deben ser mayores a cero." };
            }

            if (model.MonedaOrigenId == model.MonedaDestinoId)
            {
                return new ServiceResult<int> { Success = false, Message = "No puedes intercambiar una moneda por sí misma." };
            }

            // 2. Verificar que existan las monedas seleccionadas
            var monedaOrigenExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaOrigenId && m.Activo == true);
            var monedaDestinoExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaDestinoId && m.Activo == true);
            if (!monedaOrigenExiste || !monedaDestinoExiste)
            {
                return new ServiceResult<int> { Success = false, Message = "Una o ambas monedas seleccionadas no son válidas." };
            }

            // 3. Validar saldo disponible en la billetera interna del usuario
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaOrigenId);

            if (billetera == null || billetera.SaldoDisponible < model.MontoOrigen)
            {
                return new ServiceResult<int> { Success = false, Message = "No cuentas con saldo disponible suficiente en tu billetera interna para respaldar esta oferta." };
            }

            // 4. Aplicar lógica ESCROW: Congelar los fondos del usuario
            billetera.SaldoDisponible -= model.MontoOrigen;
            billetera.SaldoBloqueado += model.MontoOrigen;

            // 5. Registrar la oferta en la base de datos
            var nuevaOferta = new Ofertas
            {
                UsuarioId = model.UsuarioId,
                MonedaOrigenId = model.MonedaOrigenId,
                MonedaDestinoId = model.MonedaDestinoId,
                MontoOrigen = model.MontoOrigen,
                TasaCambio = model.TasaCambio,
                Estado = "ACTIVA",
                FechaPublicacion = DateTime.Now
            };

            _context.Ofertas.Add(nuevaOferta);
            await _context.SaveChangesAsync();

            return new ServiceResult<int>
            {
                Success = true,
                Message = "¡Oferta publicada exitosamente! Tus fondos han sido retenidos en garantía de forma segura.",
                Data = nuevaOferta.Id
            };
        }

        public async Task<List<OfertaDetalleDTO>> ObtenerPizarraMercadoAsync()
        {
            return await _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o => o.Estado == "ACTIVA")
                .OrderByDescending(o => o.FechaPublicacion)
                .Select(o => new OfertaDetalleDTO
                {
                    Id = o.Id,
                    UsuarioId = o.UsuarioId,
                    NombreUsuario = o.Usuario.NombreCompleto,

                    MonedaOrigenCodigo = o.MonedaOrigen.CodigoIso,
                    MonedaOrigenSimbolo = o.MonedaOrigen.Simbolo,
                    MonedaOrigenBandera = o.MonedaOrigen.RutaBandera,
                    MontoOrigen = o.MontoOrigen,

                    MonedaDestinoCodigo = o.MonedaDestino.CodigoIso,
                    MonedaDestinoSimbolo = o.MonedaDestino.Simbolo,
                    MonedaDestinoBandera = o.MonedaDestino.RutaBandera,

                    TasaCambio = o.TasaCambio,
                    MontoDestinoCalculado = Math.Round(o.MontoOrigen * o.TasaCambio, 2),

                    Estado = o.Estado,
                    FechaPublicacion = (DateTime)o.FechaPublicacion
                })
                .ToListAsync();
        }
    }
}