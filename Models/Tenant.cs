using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laboratorio.Api.Models
{
    [Table("Tenants")]
    public class Tenant
    {
       

        [Key]
        public Guid TenantId { get; set; } = Guid.NewGuid();
        
        [Required, MaxLength(150)]
        public string? NombreClinica { get; set; }
        
        [Required, MaxLength(20)]
        public string? Rif { get; set; }
        
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
        public ICollection<ExamenCatalogo> Examenes { get; set; } = new List<ExamenCatalogo>();
    }
}