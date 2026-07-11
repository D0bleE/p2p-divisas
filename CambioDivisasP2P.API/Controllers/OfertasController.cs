using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CambioDivisasP2P.CORE.Core.Entities;
using CambioDivisasP2P.CORE.Core.DTOs;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfertasController : ControllerBase
    {
        private readonly CambioDivisasP2PContext _context;

        public OfertasController(CambioDivisasP2PContext context)
        {
            _context = context;
        }

        // 1. PUBLICAR UNA OFERTA P2P
        [HttpPost("crear")]
        public async Task<IActionResult> CrearOferta([FromBody] OfertaCreateDTO model)
        {
            if (model.MontoOrigen <= 0 || model.TasaCambio <= 0)
            {
                return BadRequest(new { message = "El monto y la tasa de cambio deben ser mayores a cero." });
            }

            if (model.MonedaOrigenId == model.MonedaDestinoId)
            {
                return BadRequest(new { message = "La moneda de origen y destino no pueden ser iguales." });
            }

            // Verificar si el usuario tiene la billetera de la moneda que ofrece
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == model.UsuarioId && b.MonedaId == model.MonedaOrigenId);

            if (billetera == null || billetera.SaldoDisponible < model.MontoOrigen)
            {
                return BadRequest(new { message = "No tienes suficiente saldo disponible en tu billetera para publicar esta oferta." });
            }

            // REGLA DE NEGOCIO: Bloquear los fondos en la billetera
            billetera.SaldoDisponible -= model.MontoOrigen;
            billetera.SaldoBloqueado += model.MontoOrigen;

            // Mapear a la entidad de la base de datos (Ofertas)
            var nuevaOferta = new Ofertas
            {
                UsuarioId = model.UsuarioId,
                MonedaOrigenId = model.MonedaOrigenId,
                MonedaDestinoId = model.MonedaDestinoId,
                MontoOrigen = model.MontoOrigen,
                TasaCambio = model.TasaCambio,
                Estado = "DISPONIBLE",
                FechaPublicacion = DateTime.Now
                // Puedes concatenar la descripción opcional en algún campo de texto si tu tabla lo permite
            };

            _context.Ofertas.Add(nuevaOferta);
            await _context.SaveChangesAsync();

            // Cálculo instantáneo para la respuesta
            decimal montoRecibir = model.MontoOrigen * model.TasaCambio;

            return Ok(new
            {
                message = "Oferta publicada con éxito en el mercado P2P. Tus fondos han sido congelados en garantía.",
                OfertaId = nuevaOferta.Id,
                MontoARecibir = montoRecibir
            });
        }

        // 2. CANCELAR UNA OFERTA (Devuelve el dinero bloqueado al disponible)
        [HttpPost("cancelar/{id}")]
        public async Task<IActionResult> CancelarOferta(int id)
        {
            var oferta = await _context.Ofertas.FindAsync(id);

            if (oferta == null) return NotFound(new { message = "La oferta no existe." });

            if (oferta.Estado != "DISPONIBLE")
            {
                return BadRequest(new { message = $"No se puede cancelar esta oferta porque su estado actual es: {oferta.Estado}." });
            }

            // Buscar la billetera de origen del usuario para devolverle los fondos
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == oferta.UsuarioId && b.MonedaId == oferta.MonedaOrigenId);

            if (billetera != null)
            {
                // REGLA DE NEGOCIO INVERSA: Liberar los fondos bloqueados
                billetera.SaldoBloqueado -= oferta.MontoOrigen;
                billetera.SaldoDisponible += oferta.MontoOrigen;
            }

            // Actualizar estado de la oferta
            oferta.Estado = "CANCELADO";
            oferta.FechaTransaccion = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Oferta cancelada con éxito. Los fondos congelados han regresado a tu saldo disponible." });
        }

        // 3. VER MERCADO P2P (Optimizado para el frontend: Cero consultas extra)
        [HttpGet("mercado")]
        public async Task<IActionResult> ObtenerMercadoP2P()
        {
            var ofertasActivas = await _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o => o.Estado == "DISPONIBLE")
                .Select(o => new
                {
                    o.Id,
                    CreadorId = o.UsuarioId,
                    CreadorNombre = o.Usuario.NombreCompleto, // Nombre de quien la crea

                    MonedaOrigenId = o.MonedaOrigenId,
                    MonedaOrigenCodigo = o.MonedaOrigen.CodigoIso,
                    MonedaOrigenSimbolo = o.MonedaOrigen.Simbolo,
                    MontoOfrecido = o.MontoOrigen, // Lo que tiene el usuario

                    MonedaDestinoId = o.MonedaDestinoId,
                    MonedaDestinoCodigo = o.MonedaDestino.CodigoIso,
                    MonedaDestinoSimbolo = o.MonedaDestino.Simbolo,

                    TasaCambio = o.TasaCambio, // Tipo de cambio establecido por el creador

                    MontoARecibir = o.MontoOrigen * o.TasaCambio,

                    CalificacionPromedio = _context.Calificaciones
                    .Where(c => c.UsuarioEvaluadoId == o.UsuarioId)
                    .Average(c => (decimal?)c.Puntuacion) ?? 0,

                    TotalCalificaciones = _context.Calificaciones
                    .Count(c => c.UsuarioEvaluadoId == o.UsuarioId),

                    o.FechaPublicacion
                })
                .OrderByDescending(o => o.FechaPublicacion)
                .ToListAsync();

            return Ok(ofertasActivas);
        }

        // 4. BUSCADOR DE OFERTAS FILTRADO (Para la vista del Cliente comprador)
        // GET: api/Ofertas/buscar?tengoMonedaId=2&quieroMonedaId=1
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarOfertas([FromQuery] int tengoMonedaId, [FromQuery] int quieroMonedaId)
        {
            // Validar que no envíen las mismas monedas
            if (tengoMonedaId == quieroMonedaId)
            {
                return BadRequest(new { message = "La moneda que tienes y la que quieres no pueden ser iguales." });
            }

            // Buscamos en la base de datos cruzando la lógica P2P
            var ofertasFiltradas = await _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o => o.Estado == "DISPONIBLE" &&
                            o.MonedaOrigenId == quieroMonedaId &&  // Lo que el creador vende es lo que tú quieres
                            o.MonedaDestinoId == tengoMonedaId)    // Lo que el creador pide es lo que tú tienes
                .Select(o => new
                {
                    OfertaId = o.Id,

                    // 👤 Información del Creador de la Oferta
                    CreadorId = o.UsuarioId,
                    CreadorNombre = o.Usuario.NombreCompleto,

                    // 💵 Lo que el COMPRADOR va a RECIBIR (Lo que el creador está vendiendo)
                    RecibirMonto = o.MontoOrigen,
                    RecibirMonedaCodigo = o.MonedaOrigen.CodigoIso,
                    RecibirMonedaSimbolo = o.MonedaOrigen.Simbolo,

                    // 📊 Tasa de cambio fijada por el creador
                    TipoCambioP2P = o.TasaCambio,

                    // 💳 Lo que el COMPRADOR tiene que PAGAR (Calculado de inmediato en el Backend)
                    // Se calcula multiplicando el monto de origen por la tasa establecida
                    PagarMontoCalculado = o.MontoOrigen * o.TasaCambio,
                    PagarMonedaCodigo = o.MonedaDestino.CodigoIso,
                    PagarMonedaSimbolo = o.MonedaDestino.Simbolo,

                    FechaPublicacion = o.FechaPublicacion
                })
                .OrderBy(o => o.TipoCambioP2P) // Te lo ordena automáticamente de la tasa más barata a la más cara
                .ToListAsync();

            if (!ofertasFiltradas.Any())
            {
                return Ok(new
                {
                    message = "Actualmente no hay ofertas disponibles en el mercado que coincidan exactamente con tu criterio de búsqueda.",
                    ofertas = ofertasFiltradas
                });
            }

            return Ok(ofertasFiltradas);
        }

        // 5. ACEPTAR / TOMAR UNA OFERTA P2P (Intercambio directo y seguro de saldos)
        // POST: api/Ofertas/aceptar/12
        [HttpPost("aceptar/{id}")]
        public async Task<IActionResult> AceptarOferta(int id, [FromBody] OfertaAceptarDTO model)
        {
            var oferta = await _context.Ofertas.FindAsync(id);

            // 1. Validaciones básicas de la oferta
            if (oferta == null) return NotFound(new { message = "La oferta no existe." });

            if (oferta.Estado != "DISPONIBLE")
            {
                return BadRequest(new { message = "Esta oferta ya no está disponible, fue cancelada o tomada por otro usuario." });
            }

            if (oferta.UsuarioId == model.CompradorUsuarioId)
            {
                return BadRequest(new { message = "No puedes tomar tu propia oferta. Si ya no la deseas, puedes cancelarla." });
            }

            // Calcular cuánto tiene que pagar el comprador en la moneda destino del creador
            decimal montoAPagarComprador = oferta.MontoOrigen * oferta.TasaCambio;

            // Iniciamos una transacción médica/financiera para asegurar que todo pase completo o nada
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. VALIDAR Y ACTUALIZAR BILLETERA DEL COMPRADOR (El que toma la oferta)
                // El comprador debe tener saldo suficiente en la moneda que el creador QUIERE (MonedaDestinoId)
                var billeteraCompradorPaga = await _context.Billeteras
                    .FirstOrDefaultAsync(b => b.UsuarioId == model.CompradorUsuarioId && b.MonedaId == oferta.MonedaDestinoId);

                if (billeteraCompradorPaga == null || billeteraCompradorPaga.SaldoDisponible < montoAPagarComprador)
                {
                    return BadRequest(new { message = "No cuentas con el saldo disponible suficiente en tu billetera para tomar esta oferta." });
                }

                // El comprador recibe la moneda que el creador OFRECÍA (MonedaOrigenId)
                var billeteraCompradorRecibe = await _context.Billeteras
                    .FirstOrDefaultAsync(b => b.UsuarioId == model.CompradorUsuarioId && b.MonedaId == oferta.MonedaOrigenId);

                if (billeteraCompradorRecibe == null)
                {
                    // Si no tiene la billetera creada para esa moneda, se la inicializamos en 0
                    billeteraCompradorRecibe = new Billeteras
                    {
                        UsuarioId = model.CompradorUsuarioId,
                        MonedaId = oferta.MonedaOrigenId,
                        SaldoDisponible = 0,
                        SaldoBloqueado = 0
                    };
                    _context.Billeteras.Add(billeteraCompradorRecibe);
                }

                // 3. ACTUALIZAR BILLETERAS DEL CREADOR (El que publicó la oferta)
                // El creador ya tiene el dinero congelado en SaldoBloqueado (MonedaOrigenId)
                var billeteraCreadorBloqueada = await _context.Billeteras
                    .FirstOrDefaultAsync(b => b.UsuarioId == oferta.UsuarioId && b.MonedaId == oferta.MonedaOrigenId);

                // El creador recibe el dinero del comprador en su SaldoDisponible (MonedaDestinoId)
                var billeteraCreadorRecibe = await _context.Billeteras
                    .FirstOrDefaultAsync(b => b.UsuarioId == oferta.UsuarioId && b.MonedaId == oferta.MonedaDestinoId);

                if (billeteraCreadorRecibe == null)
                {
                    billeteraCreadorRecibe = new Billeteras
                    {
                        UsuarioId = oferta.UsuarioId,
                        MonedaId = oferta.MonedaDestinoId,
                        SaldoDisponible = 0,
                        SaldoBloqueado = 0
                    };
                    _context.Billeteras.Add(billeteraCreadorRecibe);
                }


                // ====================================================================
                // EFECTUAR LOS MOVIMIENTOS MATEMÁTICOS DIRECTOS
                // ====================================================================

                // A) Descontar y abonar la Moneda de Origen (Ej: El Dólar)
                billeteraCreadorBloqueada!.SaldoBloqueado -= oferta.MontoOrigen;
                billeteraCompradorRecibe.SaldoDisponible += oferta.MontoOrigen;

                // B) Descontar y abonar la Moneda de Destino (Ej: El Sol)
                billeteraCompradorPaga.SaldoDisponible -= montoAPagarComprador;
                billeteraCreadorRecibe.SaldoDisponible += montoAPagarComprador;

                oferta.UsuarioCompradorId = model.CompradorUsuarioId;

                // 4. Cambiar el estado de la oferta a COMPLETADO
                oferta.Estado = "COMPLETADO";
                oferta.FechaTransaccion = DateTime.Now; 

                // Guardar todos los cambios en la BD y confirmar la transacción
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "¡Transacción P2P exitosa! El intercambio de divisas se ha realizado de forma directa en las billeteras.",
                    Detalle = new
                    {
                        OfertaId = oferta.Id,
                        CompradorId = model.CompradorUsuarioId,
                        MontoEntregado = montoAPagarComprador,
                        MontoRecibido = oferta.MontoOrigen
                    }
                });
            }
            catch (Exception ex)
            {
                // Si algo falla catastróficamente, revertimos los saldos a como estaban antes del clic
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Error interno en la transacción segura: {ex.Message}" });
            }
        }
        [HttpGet("historial/{usuarioId}")]
        public async Task<IActionResult> ObtenerHistorialP2P(int usuarioId)
        {
            var historial = await _context.Ofertas
                .Include(o => o.Usuario)
                .Include(o => o.MonedaOrigen)
                .Include(o => o.MonedaDestino)
                .Where(o =>
                    o.UsuarioId == usuarioId ||
                    o.UsuarioCompradorId == usuarioId)
                .OrderByDescending(o => o.FechaTransaccion ?? o.FechaPublicacion)
                .Select(o => new
                {
                    o.Id,
                    CreadorId = o.UsuarioId,
                    CompradorId = o.UsuarioCompradorId,
                    CreadorNombre = o.Usuario.NombreCompleto,
                    MonedaOrigenCodigo = o.MonedaOrigen.CodigoIso,
                    MonedaOrigenSimbolo = o.MonedaOrigen.Simbolo,
                    MonedaDestinoCodigo = o.MonedaDestino.CodigoIso,
                    MonedaDestinoSimbolo = o.MonedaDestino.Simbolo,
                    o.MontoOrigen,
                    o.TasaCambio,
                    MontoDestino = o.MontoOrigen * o.TasaCambio,
                    o.Estado,
                    o.FechaPublicacion,
                    o.FechaTransaccion,
                    RolUsuario = o.UsuarioId == usuarioId ? "CREADOR" : "COMPRADOR"
                })
                .ToListAsync();

            return Ok(historial);
        }

    }
}