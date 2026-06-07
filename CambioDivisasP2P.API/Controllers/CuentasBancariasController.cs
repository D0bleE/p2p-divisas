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
        public async Task<IActionResult> RegistrarCuenta([FromBody] CuentaBancariaCrearDTO model)
        {
            // Validar que el usuario y la moneda existan
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            var monedaExiste = await _context.Monedas.AnyAsync(m => m.Id == model.MonedaId && m.Activo == true);

            if (usuario == null || !monedaExiste)
                return BadRequest(new { message = "Usuario o Moneda no válidos." });

            // Si por alguna razón el frontend manda el TitularNombre vacío, 
            // usamos como respaldo de seguridad el NombreCompleto del propio usuario
            string titularFinal = string.IsNullOrWhiteSpace(model.TitularNombre)
                ? usuario.NombreCompleto
                : model.TitularNombre;

            var nuevaCuenta = new CuentasBancarias
            {
                UsuarioId = model.UsuarioId,
                MonedaId = model.MonedaId,
                Banco = model.Banco,
                TitularNombre = titularFinal, // 🔥 Ahora se guarda de forma nativa en tu columna de SQL Server
                NumeroCuenta = model.NumeroCuenta,
                NumeroCCI = model.NumeroCCI
            };

            _context.CuentasBancarias.Add(nuevaCuenta);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Cuenta bancaria registrada con éxito a nombre de: {titularFinal}." });
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