using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laboratorio.Api.Models
{
    [Table("Examenes_Catalogo")]
    public class ExamenCatalogo
    {
        [Key]
        public Guid ExamenId { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required, MaxLength(50)]
        public string? Categoria { get; set; }
        
        [Required, MaxLength(100)]
        public string? NombreParametro { get; set; }
        
        [MaxLength(20)]
        public string? Unidades { get; set; }
        
        [MaxLength(100)]
        public string? RangoReferenciaDefecto { get; set; }
        
        [MaxLength(100)]
        public string? TecnicaDefecto { get; set; }
        
        public bool EsTercerizado { get; set; } = false;
        
        [MaxLength(150)]
        public string? LaboratorioDestino { get; set; }

        public bool Activo { get; set; } = true;
    }
}
