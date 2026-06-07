using System;
using System.Collections.Generic;
using System.Text;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    public class CuentaBancariaCrearDTO
    {
        public int UsuarioId { get; set; }
        public int MonedaId { get; set; }
        public string Banco { get; set; } = null!;
        public string NumeroCuenta { get; set; } = null!;
        public string NumeroCCI { get; set; } = null!;
        public string TitularNombre { get; set; } = null!; 
    }

    public class SolicitudMovimientoDTO
    {
        public int UsuarioId { get; set; }
        public int MonedaId { get; set; }
        public decimal Monto { get; set; }
        public string? RutaVoucher { get; set; }
    }
}