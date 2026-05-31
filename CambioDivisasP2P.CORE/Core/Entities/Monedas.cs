using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Monedas
{
    public int Id { get; set; }

    public string CodigoIso { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Simbolo { get; set; } = null!;

    public string RutaBandera { get; set; } = null!;

    public bool? Activo { get; set; }

    public virtual ICollection<Billeteras> Billeteras { get; set; } = new List<Billeteras>();

    public virtual ICollection<CuentasBancarias> CuentasBancarias { get; set; } = new List<CuentasBancarias>();

    public virtual ICollection<MovimientosFondos> MovimientosFondos { get; set; } = new List<MovimientosFondos>();

    public virtual ICollection<Ofertas> OfertasMonedaDestino { get; set; } = new List<Ofertas>();

    public virtual ICollection<Ofertas> OfertasMonedaOrigen { get; set; } = new List<Ofertas>();

    public virtual ICollection<Transacciones> TransaccionesMonedaDestino { get; set; } = new List<Transacciones>();

    public virtual ICollection<Transacciones> TransaccionesMonedaOrigen { get; set; } = new List<Transacciones>();
}
