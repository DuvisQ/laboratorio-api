using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Auth
{
    public class CambiarPinDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida para verificar su identidad.")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nuevo PIN es requerido.")]
        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "El PIN debe contener entre 4 y 6 dígitos numéricos.")]
        public string NuevoPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar el nuevo PIN.")]
        [Compare("NuevoPin", ErrorMessage = "La confirmación no coincide con el nuevo PIN.")]
        public string ConfirmarNuevoPin { get; set; } = string.Empty;
    }
}
