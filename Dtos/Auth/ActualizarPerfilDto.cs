using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class ActualizarPerfilDto
    {
        [MaxLength(20, ErrorMessage = "La cédula no puede exceder 20 caracteres.")]
        public string? Cedula { get; set; }

        [MaxLength(50, ErrorMessage = "El código MPPS no puede exceder 50 caracteres.")]
        public string? Mpps { get; set; }

        [MaxLength(50, ErrorMessage = "El código de Colegio de Bioanalistas no puede exceder 50 caracteres.")]
        public string? ColegioBioanalistas { get; set; }
    }
}
