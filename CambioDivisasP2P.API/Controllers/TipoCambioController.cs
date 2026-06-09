using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CambioDivisasP2P.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoCambioController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public TipoCambioController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        // GET: api/TipoCambio/en-vivo?desde=USD&hacia=PEN
        [HttpGet("en-vivo")]
        public async Task<IActionResult> ObtenerTipoCambio([FromQuery] string desde, [FromQuery] string hacia)
        {
            if (string.IsNullOrWhiteSpace(desde) || string.IsNullOrWhiteSpace(hacia))
            {
                return BadRequest(new { message = "Los códigos de moneda 'desde' y 'hacia' son obligatorios." });
            }

            string apiKey = _configuration["ExchangeRateApi:ApiKey"]!;

            // Construimos la URL oficial de ExchangeRate-API para consultar la moneda base
            string url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/{desde.ToUpper()}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "Error al consultar el servicio externo de tipo de cambio." });
                }

                // Leer y procesar la respuesta JSON de la API externa
                string jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // Verificamos si la API externa respondió de manera exitosa internamente
                if (root.GetProperty("result").GetString() == "success")
                {
                    var conversionRates = root.GetProperty("conversion_rates");

                    // Intentamos buscar la tasa de cambio de la moneda destino
                    if (conversionRates.TryGetProperty(hacia.ToUpper(), out var tasaElement))
                    {
                        decimal tasaCambio = tasaElement.GetDecimal();

                        return Ok(new
                        {
                            MonedaOrigen = desde.ToUpper(),
                            MonedaDestino = hacia.ToUpper(),
                            TasaCambioReferencial = tasaCambio,
                            FechaActualizacion = DateTime.Now,
                            Mensaje = $"1 {desde.ToUpper()} equivale actualmente a {tasaCambio} {hacia.ToUpper()}"
                        });
                    }
                    else
                    {
                        return BadRequest(new { message = $"No se encontró soporte de tasa de cambio para la moneda destino: {hacia}" });
                    }
                }

                return BadRequest(new { message = "La API externa devolvió un estado fallido." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Excepción interna: {ex.Message}" });
            }
        }
    }
}