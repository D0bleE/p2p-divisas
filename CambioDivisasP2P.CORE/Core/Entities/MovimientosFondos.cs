using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class MovimientosFondos
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int MonedaId { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? RutaVoucher { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaSolicitud { get; set; }

    public DateTime? FechaProcesado { get; set; }

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
