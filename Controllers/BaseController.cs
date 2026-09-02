using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Laboratorio.Api.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected Guid ObtenerTenantIdDelToken()
        {
            var tenantClaim = User.FindFirst("tenant_id") ?? User.FindFirst(ClaimTypes.GroupSid);
            
            if (tenantClaim == null || !Guid.TryParse(tenantClaim.Value, out var tenantId))
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