using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Vouchers
{
    public int Id { get; set; }

    public int TransaccionId { get; set; }

    public string RutaImagen { get; set; } = null!;

    public DateTime? FechaSubida { get; set; }

    public virtual Transacciones Transaccion { get; set; } = null!;
}
