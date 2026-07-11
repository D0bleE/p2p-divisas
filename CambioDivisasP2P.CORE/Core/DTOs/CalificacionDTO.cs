using System.ComponentModel.DataAnnotations;

namespace CambioDivisasP2P.CORE.Core.DTOs
{
    public class CalificacionDTO
    {
        [Required]
        public int OfertaId { get; set; }

        [Required]
        public int UsuarioEvaluadorId { get; set; } // El que está dando clic en enviar

        [Required]
        [Range(1, 5, ErrorMessage = "La puntuación debe estar entre 1 y 5 estrellas.")]
        public decimal Puntuacion { get; set; } // Soporta enteros y decimales (ej: 4.5)

        public string? Comentario { get; set; } // El signo '?' lo hace opcional en C#
    }
}