using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Laboratorio.Api.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected Guid ObtenerTenantIdDelToken()
{
    var tenantClaim = User.FindFirst("TenantId")?.Value;
    
    // Imprimimos en la consola de la terminal para depurar
    Console.WriteLine($"🔍 Valor del claim TenantId en el token: '{tenantClaim}'");

    if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
    {
        throw new UnauthorizedAccessException("El token no contiene un TenantId válido.");
    }

    return tenantId;
}

        protected string ObtenerUsuarioIdDelToken()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? User.FindFirst("sub")?.Value 
                   ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                   ?? throw new UnauthorizedAccessException("El token no contiene la identidad del usuario.");
        }

        protected Guid ObtenerUsuarioGuidDelToken()
        {
            var idStr = ObtenerUsuarioIdDelToken();
            if (Guid.TryParse(idStr, out var usuarioId))
            {
                return usuarioId;
            }
            throw new UnauthorizedAccessException($"El identificador de usuario '{idStr}' en el token no es un Guid válido.");
        }

        protected string ObtenerRolDelToken()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value 
                   ?? User.FindFirst("role")?.Value 
                   ?? string.Empty;
        }
    }
}