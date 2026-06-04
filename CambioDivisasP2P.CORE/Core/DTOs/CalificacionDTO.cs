using System;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    public class CalificacionDTO
    {
        public int TransaccionId { get; set; }
        public int UsuarioEvaluadorId { get; set; }
        public int UsuarioEvaluadoId { get; set; }
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
    }
}
