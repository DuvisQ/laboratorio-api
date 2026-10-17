using System;
using System.Collections.Generic;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class OrdenDetalleCompletoDto
    {
        public Guid OrdenId { get; set; }
        public int CorrelativoDiario { get; set; }
        public string NumeroOrdenFormateado => $"#{CorrelativoDiario:D3}-{FechaOrden:yyyy}";
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaValidacionFinal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoExamen { get; set; }
        public string? ObservacionBioanalista { get; set; }

        // Datos del Paciente
        public Guid PacienteId { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public string PacienteCedula { get; set; } = string.Empty;
        public string PacienteSexo { get; set; } = string.Empty;
        public DateTime PacienteFechaNacimiento { get; set; }
        public int PacienteEdad { get; set; }
        public string? PacienteTelefono { get; set; }
        public string? PacienteDireccion { get; set; }
        public string? PacienteNumeroHistoria { get; set; }

        // Factura y Pagos
        public FacturaDetalleDto? Factura { get; set; }

        // Resultados agrupados por examen y categoría
        public List<ExamenOrdenDetalleDto> Examenes { get; set; } = new List<ExamenOrdenDetalleDto>();

        // Datos del Bioanalista validador si aplica
        public BioanalistaValidadorDto? Bioanalista { get; set; }

        // Datos institucionales de la clínica
        public ClinicaCabeceraDto? Clinica { get; set; }
    }

    public class ClinicaCabeceraDto
    {
        public string NombreClinica { get; set; } = string.Empty;
        public string Rif { get; set; } = string.Empty;
        public string Direccion { get; set; } = "La Victoria, Edo. Aragua";
        public string Telefono { get; set; } = string.Empty;
    }

    public class FacturaDetalleDto
    {
        public Guid FacturaId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal MontoDescuento { get; set; }
        public string? MotivoDescuento { get; set; }
        public decimal PorcentajeIva { get; set; }
        public decimal MontoIva { get; set; }
        public decimal TotalNeto { get; set; }
        public decimal TasaCambio { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime FechaEmision { get; set; }
        public decimal TotalPagadoUSD { get; set; }
        public decimal TotalPagadoVES { get; set; }
        public decimal SaldoPendienteUSD { get; set; }
        public List<PagoItemDto> Pagos { get; set; } = new List<PagoItemDto>();
    }

    public class PagoItemDto
    {
        public Guid PagoId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "USD";
        public string MetodoPago { get; set; } = "Efectivo";
        public string? Referencia { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class ExamenOrdenDetalleDto
    {
        public Guid ResultadoId { get; set; }
        public Guid ExamenId { get; set; }
        public string CodigoProveedor { get; set; } = string.Empty;
        public string NombreExamen { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal PrecioDolares { get; set; }
        public bool EsTercerizado { get; set; }
        public string? Muestra { get; set; }
        public bool MuestraEntregada { get; set; } = true;
        public string? ValorResultado { get; set; }
        public string? RangoReferenciaAplicado { get; set; }
        public string? TecnicaAplicada { get; set; }
        public bool FueraDeRango { get; set; }
        public DateTime FechaCarga { get; set; }
        public List<ExamenParametroDetalleDto> Parametros { get; set; } = new List<ExamenParametroDetalleDto>();
    }

    public class ExamenParametroDetalleDto
    {
        public Guid ParametroId { get; set; }
        public string NombreParametro { get; set; } = string.Empty;
        public string? Unidades { get; set; }
        public string? RangoReferenciaDefecto { get; set; }
        public string? TecnicaDefecto { get; set; }
        public int Orden { get; set; }
    }

    public class BioanalistaValidadorDto
    {
        public Guid UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? CodigoColegioBioanalistas { get; set; }
        public string? NumeroMpps { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
