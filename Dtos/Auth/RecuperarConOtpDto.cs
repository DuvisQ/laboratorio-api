using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class RecuperarConOtpDto
    {
        [Required(ErrorMessage = "Debe ingresar el correo electrónico o nombre de usuario.")]
        public string EmailOUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar el código OTP de 6 dígitos recibido por correo.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código OTP debe ser un número de 6 dígitos.")]
        public string CodigoOtp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar la nueva contraseña.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string NuevaPassword { get; set; } = string.Empty;

        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "El nuevo PIN debe ser un código numérico de 4 a 6 dígitos.")]
        public string? NuevoPinAutorizacion { get; set; }
    }
}
