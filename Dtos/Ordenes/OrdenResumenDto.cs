using System;
using System.Collections.Generic;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class OrdenResumenDto
    {
        public Guid OrdenId { get; set; }
        public int CorrelativoDiario { get; set; }
        public string NumeroOrdenFormateado => $"#{CorrelativoDiario:D3}-{FechaOrden:yyyy}";
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaValidacionFinal { get; set; }
        public string Estado { get; set; } = "Registrada"; // Registrada, Procesada, Validada
        public string? MotivoExamen { get; set; }
        public string? ObservacionBioanalista { get; set; }

        // Datos del Paciente
        public Guid PacienteId { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public string PacienteCedula { get; set; } = string.Empty;
        public string PacienteSexo { get; set; } = string.Empty;
        public int PacienteEdad { get; set; }
        public string? PacienteTelefono { get; set; }

        // Datos de Exámenes
        public int TotalExamenes { get; set; }
        public List<string> ExamenesNombres { get; set; } = new List<string>();

        // Datos de Factura / Liquidación
        public Guid? FacturaId { get; set; }
        public decimal TotalNetoUSD { get; set; }
        public decimal TotalNetoVES { get; set; }
        public decimal TotalPagadoUSD { get; set; }
        public decimal SaldoPendienteUSD { get; set; }
        public decimal TasaCambio { get; set; }
        public string EstadoFactura { get; set; } = "Pendiente"; // Pendiente, Abonada, Pagada
    }
}
