using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Disputas
{
    public int Id { get; set; }

    public int TransaccionId { get; set; }

    public int UsuarioDemandanteId { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? Resolucion { get; set; }

    public DateTime? FechaApertura { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public virtual Transacciones Transaccion { get; set; } = null!;

    public virtual Usuarios UsuarioDemandante { get; set; } = null!;
}
