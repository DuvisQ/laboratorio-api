using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laboratorio.Api.Models
{
    [Table("Ordenes_Laboratorio")]
    public class OrdenLaboratorio
    {
        [Key]
        public Guid OrdenId { get; set; } = Guid.NewGuid();

       [Required]
        public Guid TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        
        [Required]
        public Guid PacienteId { get; set; }
        [ForeignKey("PacienteId")]
        public Paciente? Paciente { get; set; }

        public int CorrelativoDiario { get; set; } 
        
        [Required, MaxLength(30)]
        public string Estado { get; set; } = "Registrada"; 

        [MaxLength(255)]
        public string? MotivoExamen { get; set; } 
        
        [MaxLength(255)]
        public string? ObservacionBioanalista { get; set; }

        public bool PermitirEnvioParcial { get; set; } = false;
        
        [MaxLength(255)]
        public string? RutaArchivoExterno { get; set; } = string.Empty;

        public DateTime FechaOrden { get; set; } = DateTime.UtcNow;
        public DateTime? FechaValidacionFinal { get; set; }

        public ICollection<ResultadoDetalle>? Resultados { get; set; }
    }
}