using System;

namespace Laboratorio.Api.Dtos.Usuarios
{
    public class UsuarioDto
    {
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public bool TienePin { get; set; }
        public string? Cedula { get; set; }
        public string? Mpps { get; set; }
        public string? ColegioBioanalistas { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
