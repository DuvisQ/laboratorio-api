using System;
using System.Collections.Generic;

namespace Laboratorio.Api.Dtos.Examenes
{
    public class ExamenDto
    {
        public Guid ExamenId { get; set; }
        public string CodigoProveedor { get; set; } = string.Empty; // Código interno SALV o código proveedor
        public string NombreExamen { get; set; } = string.Empty;
        public string? Observaciones { get; set; } // Tipo de muestra / instrucciones al paciente
        public string Categoria { get; set; } = string.Empty;
        public decimal CostoDolares { get; set; }
        public string? TiempoRespuesta { get; set; }
        public decimal PrecioDolares { get; set; }
        public bool EsTercerizado { get; set; }
        public string? LaboratorioDestino { get; set; }
        public bool Activo { get; set; }
        public int TotalParametros => Parametros?.Count ?? 0;
        public List<ExamenParametroDto> Parametros { get; set; } = new List<ExamenParametroDto>();
    }
}
