using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Laboratorio.Api.Models
{
    public class Pago
    {
        [Key]
        public Guid PagoId { get; set; }

        [Required]
        public Guid TenantId { get; set; } // Obligatorio para el multi-tenant

       [Required]
        public Guid FacturaId { get; set; } // La orden clínica a la que pertenece el pago

        [Required]
        public Guid UsuarioId { get; set; } // El Cajero o Administrador que recibe el dinero

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(3)]
        public string Moneda { get; set; } = "USD"; // Valor por defecto // Ej: "VES", "USD", "EUR"

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; } = "Efectivo"; // Valor por defecto // Ej: "Efectivo", "Pago Móvil", "Zelle", "Punto de Venta"

        [MaxLength(100)]
        public string? Referencia { get; set; } // Número de recibo, referencia de Zelle o de Pago Móvil

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Propiedades de navegación (Opcionales, pero recomendadas para Entity Framework)
        [JsonIgnore]
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [JsonIgnore]
        [ForeignKey("FacturaId")]
        public Factura? Factura { get; set; }
    }
}