using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Laboratorio.Api.Models
{
    public class Factura
    {
        [Key]
        public Guid FacturaId { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        // El puente hacia lo clínico
        [Required]
        public Guid OrdenLaboratorioId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoDescuento { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")] 
        public decimal PorcentajeIva { get; set; } = 0; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoIva { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalNeto { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")] 
        public decimal TasaCambio { get; set; } 

        [MaxLength(50)]
        public string Estado { get; set; } = "Pendiente"; 

        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? MotivoDescuento { get; set; }
        public Guid? DescuentoAplicadoPor { get; set; } 
        public Guid? DescuentoAutorizadoPor { get; set; }
        public DateTime? FechaDescuento { get; set; }

        // Propiedades de Navegación
        [JsonIgnore]
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [JsonIgnore]
        [ForeignKey("OrdenLaboratorioId")]
        public OrdenLaboratorio? OrdenLaboratorio { get; set; }

        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}