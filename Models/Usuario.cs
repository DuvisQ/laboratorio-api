using System;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Models
{
    public class Usuario
    {
        [Key]
        public Guid UsuarioId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Rol { get; set; } = "Recepcionista"; // Roles: Administrador, Recepcionista, Bioanalista

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}