using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class CrearOrdenConFacturaDto
    {
        [Required(ErrorMessage = "El paciente es obligatorio.")]
        public Guid PacienteId { get; set; }

        public string? MotivoExamen { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un examen.")]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un examen.")]
        public List<Guid> ExamenesIds { get; set; } = new List<Guid>();

        [Required(ErrorMessage = "La tasa de cambio oficial es obligatoria.")]
        public decimal TasaCambio { get; set; }

        public decimal MontoDescuento { get; set; } = 0;
        public string? MotivoDescuento { get; set; }
        public string? PinAdministrador { get; set; }

        public List<CrearPagoInicialDto>? Pagos { get; set; }
    }

    public class CrearPagoInicialDto
    {
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "USD"; // "USD" o "VES"
        public string MetodoPago { get; set; } = "Efectivo"; // "Efectivo", "Pago Móvil", "Punto de Venta", "Zelle", "Efectivo Bs", "Transferencia"
        public string? Referencia { get; set; }
    }
}
