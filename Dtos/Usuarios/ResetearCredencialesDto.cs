using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Usuarios
{
    public class ResetearCredencialesDto
    {
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? NuevaPassword { get; set; }

        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "El PIN debe ser un código numérico de 4 a 6 dígitos.")]
        public string? NuevoPinAutorizacion { get; set; }
    }
}
