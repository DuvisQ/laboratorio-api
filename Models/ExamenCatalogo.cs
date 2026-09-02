using System;
using System.Collections.Generic;
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
        public string CodigoProveedor { get; set; } = string.Empty; // Columna A: Ej. BL081101

        [Required, MaxLength(200)]
        public string NombreExamen { get; set; } = string.Empty; // Columna B: Descripción

        [MaxLength(150)]
        public string? Observaciones { get; set; } = string.Empty; // Columna C: Muestra / Presentación (Suero, Orina 24h)

        [Required, MaxLength(50)]
        public string Categoria { get; set; } = string.Empty; // Ej. Hematología, Química Sanguínea

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoDolares { get; set; } // Columna D: Costo para la clínica

        [MaxLength(50)]
        public string? TiempoRespuesta { get; set; } = string.Empty; // Columna E: Tiempo de respuesta (Ej. 72 HORAS)

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioDolares { get; set; } // Columna F: Precio al público en $ (Administración)

        public bool EsTercerizado { get; set; } = false;
        
        [MaxLength(150)]
        public string? LaboratorioDestino { get; set; }

        public bool Activo { get; set; } = true;

        // Relación 1:N con los parámetros clínicos (El mundo del bionalista: unidades, rangos, etc.)
        public ICollection<ExamenParametro> Parametros { get; set; } = new List<ExamenParametro>();
    }

    [Table("Examenes_Parametros")]
    public class ExamenParametro
    {
        [Key]
        public Guid ExamenParametroId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ExamenId { get; set; }
        [ForeignKey("ExamenId")]
        public ExamenCatalogo? ExamenCatalogo { get; set; }

        [Required, MaxLength(150)]
        public string NombreParametro { get; set; } = string.Empty; // Ej. Hemoglobina, Color, Glicemia

        [MaxLength(30)]
        public string? Unidades { get; set; } // Ej. g/dL, mg/dl, mm3
        
        [MaxLength(150)]
        public string? RangoReferenciaDefecto { get; set; } // Ej. H: 13-18 g/dL, M: 12-16 g/dL
        
        [MaxLength(100)]
        public string? TecnicaDefecto { get; set; }
        
        public int Orden { get; set; } // Orden visual de impresión en el reporte clínico
    }
}