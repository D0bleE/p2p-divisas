using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentasBancariasController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public CuentasBancariasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // POST: api/CuentasBancarias/registrar
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarCuenta(
    [FromBody] CuentaBancariaCrearDTO model)
        {
            if (model.UsuarioId <= 0 || model.MonedaId <= 0)
            {
                return BadRequest(new
                {
                    message = "El usuario y la moneda son obligatorios."
                });
            }

            if (string.IsNullOrWhiteSpace(model.Banco) ||
                model.Banco.Trim().Length < 2)
            {
                return BadRequest(new
                {
                    message = "Ingresa un nombre de banco válido."
                });
            }

            if (string.IsNullOrWhiteSpace(model.TitularNombre) ||
                model.TitularNombre.Trim().Length < 3)
            {
                return BadRequest(new
                {
                    message = "Ingresa el nombre completo del titular."
                });
            }

            string numeroCuenta = new string(
                (model.NumeroCuenta ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray()
            );

            if (numeroCuenta.Length == 0 || numeroCuenta.Length > 19)
            {
                return BadRequest(new
                {
                    message = "El número de cuenta debe contener entre 1 y 19 dígitos."
                });
            }

            string numeroCCI = new string(
                (model.NumeroCCI ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray()
            );

            if (numeroCCI.Length != 20)
            {
                return BadRequest(new
                {
                    message = "El CCI debe contener exactamente 20 dígitos."
                });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == model.UsuarioId &&
                    u.Activo == true
                );

            if (usuario == null)
            {
                return BadRequest(new
                {
                    message = "El usuario no existe o está deshabilitado."
                });
            }

            var monedaExiste = await _context.Monedas
                .AnyAsync(m =>
                    m.Id == model.MonedaId &&
                    m.Activo == true
                );

            if (!monedaExiste)
            {
                return BadRequest(new
                {
                    message = "La moneda seleccionada no existe o está deshabilitada."
                });
            }

            var cuentaDuplicada = await _context.CuentasBancarias
                .AnyAsync(c =>
                    c.UsuarioId == model.UsuarioId &&
                    (
                        c.NumeroCuenta == numeroCuenta ||
                        c.NumeroCCI == numeroCCI
                    )
                );

            if (cuentaDuplicada)
            {
                return BadRequest(new
                {
                    message = "Ya tienes registrada una cuenta con ese número o CCI."
                });
            }

            var nuevaCuenta = new CuentasBancarias
            {
                UsuarioId = model.UsuarioId,
                MonedaId = model.MonedaId,
                Banco = model.Banco.Trim(),
                TitularNombre = model.TitularNombre.Trim(),
                NumeroCuenta = numeroCuenta,
                NumeroCCI = numeroCCI
            };

            _context.CuentasBancarias.Add(nuevaCuenta);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cuenta bancaria registrada correctamente.",
                cuentaId = nuevaCuenta.Id
            });
        }

        // GET: api/CuentasBancarias/usuario/1
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerCuentasPorUsuario(int usuarioId)
        {
            var cuentas = await _context.CuentasBancarias
                .Include(c => c.Moneda)
                .Where(c => c.UsuarioId == usuarioId)
                .Select(c => new
                {
                    c.Id,
                    c.Banco,
                    c.NumeroCuenta,
                    c.NumeroCCI,
                    MonedaCodigo = c.Moneda.CodigoIso,
                    MonedaSimbolo = c.Moneda.Simbolo,
                    c.TitularNombre
                }).ToListAsync();

            return Ok(cuentas);
        }
    }
}