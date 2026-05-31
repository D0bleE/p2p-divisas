using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class Usuarios
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? FechaRegistro { get; set; }

    public bool? Activo { get; set; }

    public string Rol { get; set; } = null!;

    public virtual ICollection<Billeteras> Billeteras { get; set; } = new List<Billeteras>();

    public virtual ICollection<Calificaciones> CalificacionesUsuarioEvaluado { get; set; } = new List<Calificaciones>();

    public virtual ICollection<Calificaciones> CalificacionesUsuarioEvaluador { get; set; } = new List<Calificaciones>();

    public virtual ICollection<CuentasBancarias> CuentasBancarias { get; set; } = new List<CuentasBancarias>();

    public virtual ICollection<Disputas> Disputas { get; set; } = new List<Disputas>();

    public virtual ICollection<MovimientosFondos> MovimientosFondos { get; set; } = new List<MovimientosFondos>();

    public virtual ICollection<Ofertas> Ofertas { get; set; } = new List<Ofertas>();

    public virtual Roles RolNavigation { get; set; } = null!;

    public virtual ICollection<Transacciones> Transacciones { get; set; } = new List<Transacciones>();
}
