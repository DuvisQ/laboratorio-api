using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Usuarios
{
    public class ActualizarUsuarioDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(100)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string Rol { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Cedula { get; set; }

        [MaxLength(50)]
        public string? Mpps { get; set; }

        [MaxLength(50)]
        public string? ColegioBioanalistas { get; set; }

        public string? AvatarUrl { get; set; }

        public bool Activo { get; set; } = true;
    }
}
