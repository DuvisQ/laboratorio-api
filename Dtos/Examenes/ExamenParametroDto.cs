using System;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Examenes
{
    public class ExamenParametroDto
    {
        public Guid? ExamenParametroId { get; set; }
        
        [Required(ErrorMessage = "El nombre del parámetro analítico es obligatorio.")]
        [MaxLength(150)]
        public string NombreParametro { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Unidades { get; set; }

        [MaxLength(150)]
        public string? RangoReferenciaDefecto { get; set; }

        [MaxLength(100)]
        public string? TecnicaDefecto { get; set; }

        public int Orden { get; set; } = 1;
    }
}
