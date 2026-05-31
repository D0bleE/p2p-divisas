using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Ofertas
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int MonedaOrigenId { get; set; }

    public int MonedaDestinoId { get; set; }

    public decimal MontoOrigen { get; set; }

    public decimal TasaCambio { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaPublicacion { get; set; }

    public virtual Monedas MonedaDestino { get; set; } = null!;

    public virtual Monedas MonedaOrigen { get; set; } = null!;

    public virtual ICollection<Transacciones> Transacciones { get; set; } = new List<Transacciones>();

    public virtual Usuarios Usuario { get; set; } = null!;
}
