using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class ActualizarResultadoDto
    {
        [Required]
        public Guid ResultadoId { get; set; }
        public string? ValorResultado { get; set; }
    }

    public class IngresarResultadosDto
    {
        public string? ObservacionBioanalista { get; set; }

        [Required]
        public List<ActualizarResultadoDto> Resultados { get; set; } = new List<ActualizarResultadoDto>();
    }
}