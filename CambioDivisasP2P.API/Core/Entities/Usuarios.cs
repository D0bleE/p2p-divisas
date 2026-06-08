using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.API.Core.Entities;

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

    public virtual ICollection<Ofertas> OfertasUsuario { get; set; } = new List<Ofertas>();

    public virtual ICollection<Ofertas> OfertasUsuarioComprador { get; set; } = new List<Ofertas>();

    public virtual Roles RolNavigation { get; set; } = null!;
}
