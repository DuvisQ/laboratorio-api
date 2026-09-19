using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class RecuperarPasswordDto
    {
        [Required(ErrorMessage = "Debe ingresar el correo o nombre de usuario.")]
        public string EmailOUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar el PIN de autorización.")]
        public string PinAutorizacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar la nueva contraseña.")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
