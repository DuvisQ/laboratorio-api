using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Usuarios
{
    public class CrearUsuarioDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(100)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string Rol { get; set; } = string.Empty; // Administrador, Bioanalista, Secretaria, Cajero

        [Required(ErrorMessage = "El PIN de seguridad es obligatorio.")]
        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "El PIN debe ser un código numérico de 4 a 6 dígitos.")]
        public string PinAutorizacion { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Cedula { get; set; }

        [MaxLength(50)]
        public string? Mpps { get; set; }

        [MaxLength(50)]
        public string? ColegioBioanalistas { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
