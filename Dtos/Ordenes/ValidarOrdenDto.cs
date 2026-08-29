using System;
using System.Collections.Generic;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class ValidacionOrdenDto
    {
        public string ObservacionBioanalista { get; set; } = string.Empty;
        public Guid UuidBioanalista { get; set; }
        public List<ResultadoValidacionDto>? Resultados { get; set; }
    }

    public class ResultadoValidacionDto
    {
        public Guid ResultadoId { get; set; }
        public string? ValorResultado { get; set; }
        public string? RangoReferenciaAplicado { get; set; }
        public string? TecnicaAplicada { get; set; }
        public bool FueraDeRango { get; set; }
    }
}