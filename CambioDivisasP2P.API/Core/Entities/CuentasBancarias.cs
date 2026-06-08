using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.API.Core.Entities;

public partial class CuentasBancarias
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int MonedaId { get; set; }

    public string Banco { get; set; } = null!;

    public string NumeroCuenta { get; set; } = null!;

    public string? NumeroCci { get; set; }

    public string TitularNombre { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
