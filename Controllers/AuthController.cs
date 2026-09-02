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

        // AHORA PROTEGIDO: Solo un Administrador autenticado puede registrar nuevos usuarios
        [HttpPost("register")]
        [Authorize(Roles = "Administrador")] 
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Extraemos la clínica del administrador que está haciendo la petición
            var tenantIdAdmin = ObtenerTenantIdDelToken();

            // Lista de roles permitidos
            var allowedRoles = new List<string> { "Administrador", "Bioanalista", "Secretaria", "Cajero" };

            // Primero valida el rol
            if (!allowedRoles.Contains(dto.Rol))
                return BadRequest(new { message = "Rol no permitido. Los roles permitidos son: Administrador, Bioanalista, Secretaria y Cajero." });

            // Luego valida el correo
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El correo ya está registrado." });

            var usuario = new Usuario
            {
                TenantId = tenantIdAdmin, // ¡CRÍTICO! El usuario queda amarrado a la clínica de quien lo creó
                NombreUsuario = dto.NombreUsuario,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = dto.Rol,
                Activo = true // Asumimos que un usuario nuevo entra activo
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Mensaje de éxito exactamente como se pide
            return Ok(new { message = $"Usuario {dto.Rol} creado exitosamente." });
        }


        // El Login sigue siendo público (sin [Authorize]) porque se necesita para obtener el primer token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null || !usuario.Activo)
                return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
                return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });

            var token = _tokenService.GenerarToken(usuario);

            return Ok(new 
            { 
                message = "Autenticación exitosa",
                token = token,
                usuario = new { usuario.NombreUsuario, usuario.Email, usuario.Rol }
            });
        }
    }
}