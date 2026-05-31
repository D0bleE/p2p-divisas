using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    // Se queda exactamente igual
    public class BilleteraOperacionDTO
    {
        public int UsuarioId { get; set; }
        public int MonedaId { get; set; }
        public decimal Monto { get; set; }
    }

    // ACTUALIZADO: Ahora mapea el modelo Escrow de la base de datos
    public class BilleteraSaldoDTO
    {
        public string MonedaCodigo { get; set; } = null!;
        public string MonedaNombre { get; set; } = null!;
        public string MonedaSimbolo { get; set; } = null!;
        public string MonedaBandera { get; set; } = null!;
        public decimal SaldoDisponible { get; set; } // Dinero libre para usar
        public decimal SaldoBloqueado { get; set; }  // Dinero congelado en la pizarra
    }
}