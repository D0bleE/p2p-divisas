using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientosFondosController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public MovimientosFondosController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // ==========================================
        // FLUX DE USUARIO: SOLICITUDES
        // ==========================================

        // 1. SOLICITAR RECARGA (Usuario sube su voucher) //CAMBIOOO
        [HttpPost("solicitar-recarga")]
        public async Task<IActionResult> SolicitarRecarga(
            [FromForm] int usuarioId,
            [FromForm] int monedaId,
            [FromForm] decimal monto,
            [FromForm(Name ="voucher")] IFormFile voucher)//CAMBIOOOOOOOOOOOO
        {
            if (monto <= 0)
                return BadRequest(new { message = "El monto debe ser mayor a cero." });

            if (voucher == null || voucher.Length == 0)
                return BadRequest(new { message = "Debes adjuntar un voucher." });

            var extension = Path.GetExtension(voucher.FileName).ToLower();

            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                return BadRequest(new { message = "Solo se permiten archivos JPG o PNG." });

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vouchers");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"recarga_{usuarioId}_{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await voucher.CopyToAsync(stream);
            }

            var rutaVoucher = $"/vouchers/{nombreArchivo}";

            var nuevoMovimiento = new MovimientosFondos
            {
                UsuarioId = usuarioId,
                MonedaId = monedaId,
                TipoMovimiento = "RECARGA",
                Monto = monto,
                RutaVoucher = rutaVoucher,
                Estado = "PENDIENTE",
                FechaSolicitud = DateTime.Now
            };

            _context.MovimientosFondos.Add(nuevoMovimiento);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Solicitud de recarga enviada. Esperando aprobación del administrador.",
                rutaVoucher
            });
        }

        // 2. SOLICITAR RETIRO (Bloquea el saldo inmediatamente si tiene cuenta bancaria registrada)
        [HttpPost("solicitar-retiro")]
        public async Task<IActionResult> SolicitarRetiro([FromBody] SolicitudMovimientoDTO model)
        {
            if (model.Monto <= 0) return BadRequest(new { message = "El monto debe ser mayor a cero." });

            // 🔥 NUEVA VALIDACIÓN: Verificar si el usuario tiene al menos una cuenta bancaria registrada para esta moneda
            var tieneCuentaBancaria = await _context.CuentasBancarias
                .AnyAsync(cb => cb.UsuarioId == model.UsuarioId && cb.MonedaId == model.MonedaId);

            if (!tieneCuentaBancaria)
            {
                return BadRequest(new { message = "No puedes solicitar un retiro porque no tienes registrada ninguna cuenta bancaria en esta moneda para depositarte los fondos." });
            }

            // Verificar si tiene billetera de esa moneda y saldo suficiente
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaId);

            if (billetera == null || billetera.SaldoDisponible < model.Monto)
            {
                return BadRequest(new { message = "No cuentas con saldo disponible suficiente en esta moneda para efectuar el retiro." });
            }

            // Aplicar la lógica de negocio: Pasar de disponible a bloqueado temporalmente
            billetera.SaldoDisponible -= model.Monto;
            billetera.SaldoBloqueado += model.Monto;

            var nuevoMovimiento = new MovimientosFondos
            {
                UsuarioId = model.UsuarioId,
                MonedaId = model.MonedaId,
                TipoMovimiento = "RETIRO",
                Monto = model.Monto,
                RutaVoucher = null, // No aplica para retiros en la solicitud inicial
                Estado = "PENDIENTE",
                FechaSolicitud = DateTime.Now
            };

            _context.MovimientosFondos.Add(nuevoMovimiento);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Solicitud de retiro enviada. Tus fondos se han congelado en garantía hasta la validación." });
        }

        // ==========================================
        // FLUX DE ADMINISTRADOR: VALIDACIONES
        // ==========================================

        // 3. PROCESAR SOLICITUD (Aceptar o Rechazar)
        [HttpPost("procesar-solicitud/{id}")]
        public async Task<IActionResult> ProcesarSolicitud(int id, [FromQuery] string accion)
        {
            var movimiento = await _context.MovimientosFondos.FindAsync(id);
            if (movimiento == null || movimiento.Estado != "PENDIENTE")
            {
                return BadRequest(new { message = "La solicitud no existe o ya ha sido procesada." });
            }

            // Buscar o inicializar la billetera del usuario involucrado
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == movimiento.UsuarioId && b.MonedaId == movimiento.MonedaId);

            if (billetera == null && movimiento.TipoMovimiento == "RECARGA")
            {
                billetera = new Billeteras
                {
                    UsuarioId = movimiento.UsuarioId,
                    MonedaId = movimiento.MonedaId,
                    SaldoDisponible = 0,
                    SaldoBloqueado = 0
                };
                _context.Billeteras.Add(billetera);
            }

            if (accion.ToUpper() == "ACEPTAR")
            {
                movimiento.Estado = "APROBADO";

                if (movimiento.TipoMovimiento == "RECARGA")
                {
                    billetera!.SaldoDisponible += movimiento.Monto;
                }
                else if (movimiento.TipoMovimiento == "RETIRO")
                {
                    // El dinero sale definitivamente del sistema
                    billetera!.SaldoBloqueado -= movimiento.Monto;
                }
            }
            else if (accion.ToUpper() == "RECHAZAR")
            {
                movimiento.Estado = "RECHAZADO";

                if (movimiento.TipoMovimiento == "RETIRO")
                {
                    // El dinero regresa a su saldo libre para que pueda usarlo
                    billetera!.SaldoBloqueado -= movimiento.Monto;
                    billetera.SaldoDisponible += movimiento.Monto;
                }
                // Si se rechaza una recarga, no se altera ningún saldo.
            }
            else
            {
                return BadRequest(new { message = "Acción no válida. Use 'ACEPTAR' o 'RECHAZAR'." });
            }
            movimiento.FechaProcesado = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"La solicitud de {movimiento.TipoMovimiento} ha sido {movimiento.Estado} con éxito." });
        }

        // 4. VER TODAS LAS SOLICITUDES PENDIENTES (Para la vista del Admin)
        // 4. VER TODAS LAS SOLICITUDES PENDIENTES (Para la vista del Admin)
        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerPendientes()
        {
            var pendientes = await _context.MovimientosFondos
                .Include(m => m.Usuario)
                .Include(m => m.Moneda)
                .Where(m => m.Estado == "PENDIENTE")
                .Select(m => new
                {
                    m.Id,
                    UsuarioId = m.UsuarioId,
                    UsuarioNombre = m.Usuario.NombreCompleto,
                    MonedaId = m.MonedaId,
                    MonedaCodigo = m.Moneda.CodigoIso, // Asegúrate si es CodigoIso o CodigoISO según tu scaffold
                    m.TipoMovimiento,
                    m.Monto,
                    m.RutaVoucher,
                    m.FechaSolicitud,

                    // 🔥 NUEVA SECCIÓN: Datos de la cuenta destino si es un RETIRO
                    CuentaBancariaDestino = m.TipoMovimiento == "RETIRO"
                        ? _context.CuentasBancarias
                            .Where(cb => cb.UsuarioId == m.UsuarioId && cb.MonedaId == m.MonedaId)
                            .Select(cb => new
                            {
                                cb.Id,
                                cb.Banco,
                                cb.TitularNombre, // Tu campo físico confirmado
                                cb.NumeroCuenta,
                                cb.NumeroCCI // Asegúrate si es NumeroCci o NumeroCCI según tu scaffold
                            })
                            .FirstOrDefault() // Trae la primera cuenta que encuentre de esa moneda
                        : null // Si es RECARGA, devuelve null porque no se necesita cuenta de destino
                }).ToListAsync();

            return Ok(pendientes);
      
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerMovimientosPorUsuario(int usuarioId)
        {
            var movimientos = await _context.MovimientosFondos
                .Include(m => m.Moneda)
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.FechaSolicitud)
                .Select(m => new
                {
                    m.Id,
                    m.TipoMovimiento,
                    m.Monto,
                    m.Estado,
                    m.RutaVoucher,
                    m.FechaSolicitud,
                    m.FechaProcesado,
                    MonedaCodigo = m.Moneda.CodigoIso,
                    MonedaSimbolo = m.Moneda.Simbolo
                })
                .ToListAsync();

            return Ok(movimientos);
        }

    }
}