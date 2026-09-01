using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laboratorio.Api.Models
{
    [Table("Resultados_Detalle")]
    public class ResultadoDetalle
    {
        [Key]
        public Guid ResultadoId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid OrdenId { get; set; }
        [ForeignKey("OrdenId")]
        [System.Text.Json.Serialization.JsonIgnore]
        public OrdenLaboratorio? Orden { get; set; }

        [Required]
        public Guid ExamenId { get; set; }
        [ForeignKey("ExamenId")]
        public ExamenCatalogo? Examen { get; set; }

        [MaxLength(255)]
        public string? ValorResultado { get; set; } 

        [MaxLength(100)]
        public string? RangoReferenciaAplicado { get; set; }
        
        [MaxLength(100)]
        public string? TecnicaAplicada { get; set; }

        [MaxLength(50)]
        public string? EstadoTercerizado { get; set; } = "Interno"; 

        public bool FueraDeRango { get; set; } = false;
        public bool MuestraEntregada { get; set; } = true;

        public Guid? UuidBioanalista { get; set; }
        public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    }
}