using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class CambiarPasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida.")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida.")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        public string NuevaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [Compare("NuevaPassword", ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
