using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class SolicitarOtpDto
    {
        [Required(ErrorMessage = "Debe ingresar el correo electrónico o nombre de usuario.")]
        public string EmailOUsuario { get; set; } = string.Empty;
    }
}
