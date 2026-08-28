using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laboratorio.Api.Models
{
    [Table("Pacientes")]
    public class Paciente
    {
        [Key]
        public Guid PacienteId { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required, MaxLength(20)]
        public string Cedula { get; set; } 

        [Required, MaxLength(150)]
        public string NombreCompleto { get; set; }

        [Required, MaxLength(1)]
        public string Sexo { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaNacimiento { get; set; }

        [Required, MaxLength(20)]
        public string TelefonoPrincipal { get; set; }
        
        [MaxLength(20)]
        public string TelefonoRepresentante { get; set; }
        
        [MaxLength(255)]
        public string Direccion { get; set; }
        
        [MaxLength(50)]
        public string? NumeroHistoriaFisica { get; set; }
        
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}