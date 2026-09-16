using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data; 
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Auth;
using Laboratorio.Api.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization; // ¡Importante para los permisos!

namespace Laboratorio.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController // Heredamos de BaseController
    {
        private readonly AppDbContext _context; 
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        [AllowAnonymous] // Únicamente AllowAnonymous para este bootstrap inicial
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Lista de roles permitidos usando la clase centralizada de Roles
            if (!Laboratorio.Api.Security.Roles.Todos.Contains(dto.Rol))
                return BadRequest(new { message = $"Rol no permitido. Los roles permitidos son: {string.Join(", ", Laboratorio.Api.Security.Roles.Todos)}." });

            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El correo ya está registrado." });

            var usuario = new Usuario
            {
                TenantId = dto.TenantId, // Tomamos el TenantId directamente del JSON enviado
                NombreUsuario = dto.NombreUsuario,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = dto.Rol,
                PinAutorizacion = !string.IsNullOrEmpty(dto.PinAutorizacion) ? BCrypt.Net.BCrypt.HashPassword(dto.PinAutorizacion) : null,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Usuario {dto.Rol} creado exitosamente." });
        }


        // El Login sigue siendo público (sin [Authorize]) porque se necesita para obtener el primer token
        [HttpPost("login")]
        [AllowAnonymous] // Para que el endpoint sea público
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null || !usuario.Activo)
                return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
                return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });

            var token = _tokenService.GenerarToken(usuario);

            var userPayload = new 
            { 
                id = usuario.UsuarioId,
                nombre = usuario.NombreUsuario,
                nombreUsuario = usuario.NombreUsuario,
                email = usuario.Email, 
                rol = usuario.Rol,
                tenantId = usuario.TenantId
            };

            return Ok(new 
            { 
                message = "Autenticación exitosa",
                token = token,
                user = userPayload,
                usuario = userPayload
            });
        }
    }
}