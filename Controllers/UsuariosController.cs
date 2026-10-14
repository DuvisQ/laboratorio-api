using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Usuarios;
using Laboratorio.Api.Security;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace Laboratorio.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Administrador}")]
    public class UsuariosController : BaseController
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> ObtenerUsuarios()
        {
            var tenantId = ObtenerTenantIdDelToken();

            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.NombreUsuario)
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    Email = u.Email,
                    Rol = u.Rol,
                    Activo = u.Activo,
                    TienePin = !string.IsNullOrEmpty(u.PinAutorizacion),
                    Cedula = u.Cedula,
                    Mpps = u.Mpps,
                    ColegioBioanalistas = u.ColegioBioanalistas,
                    AvatarUrl = u.AvatarUrl,
                    FechaCreacion = u.FechaCreacion
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET: api/Usuarios/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> ObtenerUsuarioPorId(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.UsuarioId == id)
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    Email = u.Email,
                    Rol = u.Rol,
                    Activo = u.Activo,
                    TienePin = !string.IsNullOrEmpty(u.PinAutorizacion),
                    Cedula = u.Cedula,
                    Mpps = u.Mpps,
                    ColegioBioanalistas = u.ColegioBioanalistas,
                    AvatarUrl = u.AvatarUrl,
                    FechaCreacion = u.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            return Ok(usuario);
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = ObtenerTenantIdDelToken();
            var rolOperador = ObtenerRolDelToken();

            if (!Roles.Todos.Contains(dto.Rol))
                return BadRequest(new { message = $"Rol inválido. Los roles permitidos son: {string.Join(", ", Roles.Todos)}." });

            if (dto.Rol == Roles.SuperAdmin && rolOperador != Roles.SuperAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo una cuenta SuperAdmin puede registrar a otro SuperAdmin." });

            var emailLimpio = dto.Email.Trim().ToLower();
            var nombreUsuarioLimpio = dto.NombreUsuario.Trim();

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailLimpio))
                return BadRequest(new { message = "El correo electrónico ya se encuentra registrado." });

            if (await _context.Usuarios.AnyAsync(u => u.TenantId == tenantId && u.NombreUsuario.ToLower() == nombreUsuarioLimpio.ToLower()))
                return BadRequest(new { message = "El nombre de usuario ya está en uso en esta institución." });

            var nuevoUsuario = new Usuario
            {
                UsuarioId = Guid.NewGuid(),
                TenantId = tenantId,
                NombreUsuario = nombreUsuarioLimpio,
                Email = emailLimpio,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = dto.Rol,
                PinAutorizacion = !string.IsNullOrWhiteSpace(dto.PinAutorizacion) 
                    ? BCrypt.Net.BCrypt.HashPassword(dto.PinAutorizacion.Trim()) 
                    : null,
                Cedula = dto.Cedula?.Trim().ToUpper(),
                Mpps = dto.Mpps?.Trim(),
                ColegioBioanalistas = dto.ColegioBioanalistas?.Trim(),
                AvatarUrl = dto.AvatarUrl?.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerUsuarioPorId), new { id = nuevoUsuario.UsuarioId }, new UsuarioDto
            {
                UsuarioId = nuevoUsuario.UsuarioId,
                NombreUsuario = nuevoUsuario.NombreUsuario,
                Email = nuevoUsuario.Email,
                Rol = nuevoUsuario.Rol,
                Activo = nuevoUsuario.Activo,
                TienePin = !string.IsNullOrEmpty(nuevoUsuario.PinAutorizacion),
                Cedula = nuevoUsuario.Cedula,
                Mpps = nuevoUsuario.Mpps,
                ColegioBioanalistas = nuevoUsuario.ColegioBioanalistas,
                AvatarUrl = nuevoUsuario.AvatarUrl,
                FechaCreacion = nuevoUsuario.FechaCreacion
            });
        }

        // PUT: api/Usuarios/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarUsuario(Guid id, [FromBody] ActualizarUsuarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = ObtenerTenantIdDelToken();
            var rolOperador = ObtenerRolDelToken();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.UsuarioId == id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            if (!Roles.Todos.Contains(dto.Rol))
                return BadRequest(new { message = $"Rol inválido. Los roles permitidos son: {string.Join(", ", Roles.Todos)}." });

            if (usuario.Rol == Roles.SuperAdmin && rolOperador != Roles.SuperAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo el SuperAdmin puede modificar una cuenta de rango SuperAdmin." });

            if (dto.Rol == Roles.SuperAdmin && rolOperador != Roles.SuperAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tiene privilegios para promover una cuenta a SuperAdmin." });

            var emailLimpio = dto.Email.Trim().ToLower();
            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailLimpio && u.UsuarioId != id))
                return BadRequest(new { message = "El correo electrónico ya está registrado por otro usuario." });

            usuario.NombreUsuario = dto.NombreUsuario.Trim();
            usuario.Email = emailLimpio;
            usuario.Rol = dto.Rol;
            usuario.Cedula = dto.Cedula?.Trim().ToUpper();
            usuario.Mpps = dto.Mpps?.Trim();
            usuario.ColegioBioanalistas = dto.ColegioBioanalistas?.Trim();
            if (dto.AvatarUrl != null)
            {
                usuario.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
            }
            usuario.Activo = dto.Activo;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuario actualizado exitosamente." });
        }

        // POST: api/Usuarios/{id}/resetear-password (Nivel 3: Respaldo Maestro del Administrador)
        [HttpPost("{id}/resetear-password")]
        public async Task<IActionResult> ResetearCredenciales(Guid id, [FromBody] ResetearCredencialesDto dto)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var rolOperador = ObtenerRolDelToken();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.UsuarioId == id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            if (usuario.Rol == Roles.SuperAdmin && rolOperador != Roles.SuperAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo el SuperAdmin puede restablecer credenciales de una cuenta SuperAdmin." });

            if (string.IsNullOrWhiteSpace(dto.NuevaPassword) && string.IsNullOrWhiteSpace(dto.NuevoPinAutorizacion))
                return BadRequest(new { message = "Debe proporcionar al menos una nueva contraseña o un nuevo PIN." });

            if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                if (dto.NuevaPassword.Length < 6)
                    return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            }

            if (!string.IsNullOrWhiteSpace(dto.NuevoPinAutorizacion))
            {
                usuario.PinAutorizacion = BCrypt.Net.BCrypt.HashPassword(dto.NuevoPinAutorizacion.Trim());
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Credenciales restablecidas exitosamente por el Administrador." });
        }

        // PATCH: api/Usuarios/{id}/toggle-estado
        [HttpPatch("{id}/toggle-estado")]
        public async Task<IActionResult> ToggleEstado(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var rolOperador = ObtenerRolDelToken();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.UsuarioId == id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            if (usuario.Rol == Roles.SuperAdmin && rolOperador != Roles.SuperAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tiene privilegios para desactivar una cuenta SuperAdmin." });

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Usuario {(usuario.Activo ? "activado" : "desactivado")} exitosamente.",
                activo = usuario.Activo 
            });
        }
    }
}
