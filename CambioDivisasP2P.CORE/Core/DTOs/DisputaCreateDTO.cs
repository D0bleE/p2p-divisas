using System;
using System.Collections.Generic;
using System.Text;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    public class DisputaCreateDTO
    {
        public int OfertaId { get; set; }

        public int UsuarioDemandanteId { get; set; }

        public string Motivo { get; set; } = string.Empty;
    }
}
