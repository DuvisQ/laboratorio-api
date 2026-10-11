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
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public AuthController(
            AppDbContext context, 
            TokenService tokenService, 
            IEmailService emailService, 
            IWebHostEnvironment env)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _env = env;
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
            var identificador = dto.Email?.Trim().ToLower() ?? string.Empty;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == identificador || u.NombreUsuario.ToLower() == identificador);

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
                tenantId = usuario.TenantId,
                cedula = usuario.Cedula,
                mpps = usuario.Mpps,
                colegioBioanalistas = usuario.ColegioBioanalistas,
                tienePin = !string.IsNullOrEmpty(usuario.PinAutorizacion)
            };

            return Ok(new 
            { 
                message = "Autenticación exitosa",
                token = token,
                user = userPayload,
                usuario = userPayload
            });
        }

        // Endpoint de autoservicio para cambiar la contraseña usando el PIN de autorización
        [HttpPost("recuperar-password")]
        [AllowAnonymous]
        public async Task<IActionResult> RecuperarPassword([FromBody] RecuperarPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailOUsuario) || string.IsNullOrWhiteSpace(dto.PinAutorizacion) || string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            if (dto.NuevaPassword.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });
            }

            var identificador = dto.EmailOUsuario.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == identificador || u.NombreUsuario.ToLower() == identificador);

            if (usuario == null || !usuario.Activo)
            {
                return BadRequest(new { message = "Los datos ingresados no coinciden con ningún usuario activo." });
            }

            if (string.IsNullOrEmpty(usuario.PinAutorizacion))
            {
                return BadRequest(new { message = "Este usuario no posee un PIN de autorización configurado. Por favor, comuníquese con el administrador del sistema." });
            }

            // Validar PIN de autorización con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(dto.PinAutorizacion.Trim(), usuario.PinAutorizacion))
            {
                return Unauthorized(new { message = "El PIN de autorización ingresado es incorrecto." });
            }

            // Hashear y actualizar contraseña
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña restablecida exitosamente. Ya puede iniciar sesión con su nueva clave." });
        }

        // =========================================================================================
        // NIVEL 2: RECUPERACIÓN POR CÓDIGO OTP ENVIADO AL CORREO ELECTRÓNICO
        // =========================================================================================

        // POST: api/Auth/solicitar-codigo-otp
        [HttpPost("solicitar-codigo-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SolicitarCodigoOtp([FromBody] SolicitarOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailOUsuario))
                return BadRequest(new { message = "Debe indicar el correo electrónico o nombre de usuario." });

            var identificador = dto.EmailOUsuario.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == identificador || u.NombreUsuario.ToLower() == identificador);

            if (usuario == null || !usuario.Activo)
            {
                // Mensaje genérico para no filtrar existencia de cuentas
                return Ok(new 
                { 
                    message = "Si la cuenta se encuentra registrada y activa, se ha enviado un código de seguridad a su correo.",
                    emailOfuscado = "co***@***.com"
                });
            }

            // Generar código numérico criptográfico de 6 dígitos
            var codigoOtp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            usuario.CodigoRecuperacion = codigoOtp;
            usuario.FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            // Despachar correo institucional
            await _emailService.EnviarCodigoOtpAsync(usuario.Email, usuario.NombreUsuario, codigoOtp);

            var emailOfuscado = OfuscarCorreo(usuario.Email);

            return Ok(new 
            { 
                message = $"Código de seguridad generado y enviado exitosamente a {emailOfuscado}.",
                emailOfuscado = emailOfuscado,
                // Pista de desarrollo para pruebas rápidas
                codigoDev = _env.IsDevelopment() ? codigoOtp : null
            });
        }

        // POST: api/Auth/recuperar-con-otp
        [HttpPost("recuperar-con-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RecuperarConOtp([FromBody] RecuperarConOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.NuevaPassword.Length < 6)
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });

            var identificador = dto.EmailOUsuario.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == identificador || u.NombreUsuario.ToLower() == identificador);

            if (usuario == null || !usuario.Activo)
                return BadRequest(new { message = "Usuario no válido o inactivo." });

            if (string.IsNullOrWhiteSpace(usuario.CodigoRecuperacion) || usuario.FechaExpiracionCodigo == null)
                return BadRequest(new { message = "No hay una solicitud de código activa para este usuario o ya fue utilizado." });

            if (usuario.FechaExpiracionCodigo < DateTime.UtcNow)
                return BadRequest(new { message = "El código de seguridad ha expirado (validez máxima de 15 minutos). Por favor solicite uno nuevo." });

            if (usuario.CodigoRecuperacion != dto.CodigoOtp.Trim())
                return BadRequest(new { message = "El código de seguridad ingresado es incorrecto." });

            // Actualizar contraseña
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);

            // Si proporcionó un nuevo PIN, actualizarlo también
            if (!string.IsNullOrWhiteSpace(dto.NuevoPinAutorizacion))
            {
                usuario.PinAutorizacion = BCrypt.Net.BCrypt.HashPassword(dto.NuevoPinAutorizacion.Trim());
            }

            // Consumir el código
            usuario.CodigoRecuperacion = null;
            usuario.FechaExpiracionCodigo = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "¡Contraseña actualizada exitosamente! Ya puede iniciar sesión con su nueva clave." });
        }

        // =========================================================================================
        // MI PERFIL: AUTOGESTIÓN DE CREDENCIALES Y DATOS PERSONALES DEL USUARIO AUTENTICADO
        // =========================================================================================

        // GET: api/Auth/perfil
        [HttpGet("perfil")]
        [Authorize]
        public async Task<ActionResult<PerfilUsuarioDto>> ObtenerPerfil()
        {
            var usuarioIdGuid = ObtenerUsuarioGuidDelToken();
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioIdGuid);

            if (usuario == null || !usuario.Activo)
                return NotFound(new { message = "Usuario no encontrado o inactivo en el sistema." });

            var dto = new PerfilUsuarioDto
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                Email = usuario.Email,
                Rol = usuario.Rol,
                Cedula = usuario.Cedula,
                Mpps = usuario.Mpps,
                ColegioBioanalistas = usuario.ColegioBioanalistas,
                TienePin = !string.IsNullOrEmpty(usuario.PinAutorizacion),
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion
            };

            return Ok(dto);
        }

        // PUT: api/Auth/cambiar-password
        [HttpPut("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioIdGuid = ObtenerUsuarioGuidDelToken();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioIdGuid);

            if (usuario == null || !usuario.Activo)
                return NotFound(new { message = "Usuario no encontrado o inactivo." });

            // Verificar contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta. Por favor verifíquela." });

            if (dto.NuevaPassword != dto.ConfirmarPassword)
                return BadRequest(new { message = "La confirmación no coincide con la nueva contraseña." });

            if (dto.NuevaPassword.Length < 6)
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "¡Contraseña actualizada con éxito!" });
        }

        // PUT: api/Auth/cambiar-pin
        [HttpPut("cambiar-pin")]
        [Authorize]
        public async Task<IActionResult> CambiarPin([FromBody] CambiarPinDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioIdGuid = ObtenerUsuarioGuidDelToken();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioIdGuid);

            if (usuario == null || !usuario.Activo)
                return NotFound(new { message = "Usuario no encontrado o inactivo." });

            // Por seguridad, requerimos la contraseña del usuario para cambiar su PIN
            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta. Debe confirmarla para autorizar el cambio de PIN." });

            if (dto.NuevoPin != dto.ConfirmarNuevoPin)
                return BadRequest(new { message = "La confirmación no coincide con el nuevo PIN." });

            var pinLimpio = dto.NuevoPin.Trim();
            if (pinLimpio.Length < 4 || pinLimpio.Length > 6 || !pinLimpio.All(char.IsDigit))
                return BadRequest(new { message = "El PIN debe tener entre 4 y 6 dígitos numéricos exclusivamente." });

            usuario.PinAutorizacion = BCrypt.Net.BCrypt.HashPassword(pinLimpio);
            await _context.SaveChangesAsync();

            return Ok(new { message = "¡PIN de autorización asignado/actualizado con éxito!" });
        }

        // PUT: api/Auth/actualizar-datos
        [HttpPut("actualizar-datos")]
        [Authorize]
        public async Task<IActionResult> ActualizarDatos([FromBody] ActualizarPerfilDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioIdGuid = ObtenerUsuarioGuidDelToken();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioIdGuid);

            if (usuario == null || !usuario.Activo)
                return NotFound(new { message = "Usuario no encontrado o inactivo." });

            usuario.Cedula = dto.Cedula?.Trim();
            if (usuario.Rol == Laboratorio.Api.Security.Roles.Bioanalista)
            {
                usuario.Mpps = dto.Mpps?.Trim();
                usuario.ColegioBioanalistas = dto.ColegioBioanalistas?.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Datos de perfil actualizados exitosamente.",
                usuario = new PerfilUsuarioDto
                {
                    UsuarioId = usuario.UsuarioId,
                    NombreUsuario = usuario.NombreUsuario,
                    Email = usuario.Email,
                    Rol = usuario.Rol,
                    Cedula = usuario.Cedula,
                    Mpps = usuario.Mpps,
                    ColegioBioanalistas = usuario.ColegioBioanalistas,
                    TienePin = !string.IsNullOrEmpty(usuario.PinAutorizacion),
                    Activo = usuario.Activo,
                    FechaCreacion = usuario.FechaCreacion
                }
            });
        }

        private static string OfuscarCorreo(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return email;
            var partes = email.Split('@');
            var u = partes[0];
            var d = partes[1];
            if (u.Length <= 2)
                return u[0] + "***@" + d;
            return u[0] + new string('*', u.Length - 2) + u[^1] + "@" + d;
        }
    }
}