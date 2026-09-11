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
                   ?? throw new UnauthorizedAccessException("El token no contiene la identidad del usuario.");
        }
    }
}