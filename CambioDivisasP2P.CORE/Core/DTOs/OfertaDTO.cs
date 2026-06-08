using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    // DTO para capturar los datos desde el formulario web (Vue.js) al publicar
    public class OfertaCreateDTO
    {
        public int UsuarioId { get; set; }
        public int MonedaOrigenId { get; set; }   // Moneda que el usuario tiene y ofrece
        public int MonedaDestinoId { get; set; }  // Moneda que el usuario quiere recibir
        public decimal MontoOrigen { get; set; }   // Cantidad a cambiar
        public decimal TasaCambio { get; set; }    // Tipo de cambio fijado por el usuario
        public string? Descripcion { get; set; }   // Breve descripción opcional
    }

    // DTO estructurado para la Pizarra del Mercado (Carga banderas, siglas y cálculos)
    public class OfertaDetalleDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = null!;

        // Información de lo que se Vende / Ofrece
        public string MonedaOrigenCodigo { get; set; } = null!;
        public string MonedaOrigenSimbolo { get; set; } = null!;
        public string MonedaOrigenBandera { get; set; } = null!;
        public decimal MontoOrigen { get; set; }

        // Información de lo que se Busca / Recibe
        public string MonedaDestinoCodigo { get; set; } = null!;
        public string MonedaDestinoSimbolo { get; set; } = null!;
        public string MonedaDestinoBandera { get; set; } = null!;

        public decimal TasaCambio { get; set; }
        public decimal MontoDestinoCalculado { get; set; } // MontoOrigen * TasaCambio

        public string Estado { get; set; } = null!;
        public DateTime FechaPublicacion { get; set; }
    }
}