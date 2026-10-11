using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Examenes
{
    public class CrearExamenDto
    {
        [Required(ErrorMessage = "El código del examen es obligatorio (ej. SALV-00001).")]
        [MaxLength(50)]
        public string CodigoProveedor { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del examen o perfil es obligatorio.")]
        [MaxLength(200)]
        public string NombreExamen { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Observaciones { get; set; } // Tipo de muestra / instrucciones

        [Required(ErrorMessage = "La categoría clínica es obligatoria.")]
        [MaxLength(50)]
        public string Categoria { get; set; } = "General";

        [Range(0, 999999.99, ErrorMessage = "El costo no puede ser negativo.")]
        public decimal CostoDolares { get; set; } = 0;

        [MaxLength(50)]
        public string? TiempoRespuesta { get; set; } = "24 Horas";

        [Range(0, 999999.99, ErrorMessage = "El precio al público no puede ser negativo.")]
        public decimal PrecioDolares { get; set; } = 0;

        public bool EsTercerizado { get; set; } = false;

        [MaxLength(150)]
        public string? LaboratorioDestino { get; set; }

        public List<ExamenParametroDto>? Parametros { get; set; } = new List<ExamenParametroDto>();
    }
}
