using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Microsoft.AspNetCore.Authorization; // Importación obligatoria para la seguridad

namespace Laboratorio.Api.Controllers
{
    // Bloqueamos el acceso: solo los administradores autenticados pueden entrar aquí
    [Authorize(Roles = "Administrador")]
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : BaseController // Heredamos de nuestra clase base segura
    {
        private readonly AppDbContext _context;

        public TenantsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tenants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants()
        {
            // Extraemos el ID de forma segura. Un admin solo debe ver SU clínica, no las de los demás.
            var tenantId = ObtenerTenantIdDelToken();

            var tenants = await _context.Tenants
                .Where(t => t.TenantId == tenantId)
                .ToListAsync();

            return Ok(tenants);
        }

        // POST: api/tenants
        [HttpPost]
        public async Task<ActionResult<Tenant>> PostTenant(Tenant tenant)
        {
            tenant.TenantId = Guid.NewGuid();
            tenant.FechaRegistro = DateTime.UtcNow;
            
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTenants), new { id = tenant.TenantId }, tenant);
        }
    }
}