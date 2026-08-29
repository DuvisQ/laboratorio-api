using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Laboratorio.Api.Dtos.Ordenes
{
    public class CrearOrdenDto
    {
        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid PacienteId { get; set; }

        public string? MotivoExamen { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un examen.")]
        public List<Guid> ExamenesIds { get; set; } = new List<Guid>();
    }
}