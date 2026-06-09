using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.API.Core.Entities;

public partial class Billeteras
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int MonedaId { get; set; }

    public decimal SaldoDisponible { get; set; }

    public decimal SaldoBloqueado { get; set; }

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
