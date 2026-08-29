using System;
using System.Collections.Generic;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class ResultadoReporteDto
    {
        public string ExamenNombre { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Unidades { get; set; } = string.Empty;
        public string RangoReferencia { get; set; } = string.Empty;
        public string Tecnica { get; set; } = string.Empty;
        public bool FueraDeRango { get; set; }
    }

    public class CategoriaReporteDto
    {
        public string Categoria { get; set; } = string.Empty;
        public List<ResultadoReporteDto> Resultados { get; set; } = new List<ResultadoReporteDto>();
    }

    public class ReporteOrdenDto
    {
        public Guid OrdenId { get; set; }
        public int CorrelativoDiario { get; set; }
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaValidacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string ObservacionBioanalista { get; set; } = string.Empty;
        
        public string PacienteNombre { get; set; } = string.Empty;
        public string PacienteCedula { get; set; } = string.Empty;
        public string PacienteSexo { get; set; } = string.Empty;

        public List<CategoriaReporteDto> Categorias { get; set; } = new List<CategoriaReporteDto>();
    }
}