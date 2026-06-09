using System;
using System.Collections.Generic;

namespace CambioDivisasP2P.API.Core.Entities;

public partial class Calificaciones
{
    public int Id { get; set; }

    public int UsuarioEvaluadorId { get; set; }

    public int UsuarioEvaluadoId { get; set; }

    public int Puntuacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime? Fecha { get; set; }

    public int? OfertaId { get; set; }

    public virtual Ofertas? Oferta { get; set; }

    public virtual Usuarios UsuarioEvaluado { get; set; } = null!;

    public virtual Usuarios UsuarioEvaluador { get; set; } = null!;
}
