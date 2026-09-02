using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        [Required]
        public Guid TenantId { get; set; }

        [JsonIgnore]
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        
        [MaxLength(255)]
        public string? PinAutorizacion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}