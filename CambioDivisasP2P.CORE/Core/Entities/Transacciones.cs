using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Transacciones
{
    public int Id { get; set; }

    public int OfertaId { get; set; }

    public int UsuarioContraparteId { get; set; }

    public int MonedaOrigenId { get; set; }

    public decimal MontoOrigen { get; set; }

    public int MonedaDestinoId { get; set; }

    public decimal MontoDestino { get; set; }

    public decimal TasaCambioPactada { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<Calificaciones> Calificaciones { get; set; } = new List<Calificaciones>();

    public virtual ICollection<Disputas> Disputas { get; set; } = new List<Disputas>();

    public virtual Monedas MonedaDestino { get; set; } = null!;

    public virtual Monedas MonedaOrigen { get; set; } = null!;

    public virtual Ofertas Oferta { get; set; } = null!;

    public virtual Usuarios UsuarioContraparte { get; set; } = null!;

    public virtual Vouchers? Vouchers { get; set; }
}
