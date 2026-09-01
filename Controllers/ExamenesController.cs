using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Microsoft.AspNetCore.Authorization;


namespace Laboratorio.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExamenesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExamenesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/examenes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamenCatalogo>>> GetExamenes()
        {
            return await _context.ExamenesCatalogo.Include(e => e.Tenant).ToListAsync();
        }

        // GET: api/examenes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExamenCatalogo>> GetExamen(Guid id)
        {
            var examen = await _context.ExamenesCatalogo
                .Include(e => e.Tenant)
                .FirstOrDefaultAsync(e => e.ExamenId == id);

            if (examen == null)
            {
                return NotFound();
            }

            return examen;
        }

        // POST: api/examenes
        [HttpPost]
        public async Task<ActionResult<ExamenCatalogo>> PostExamen(ExamenCatalogo examen)
        {
            // Validar que el Tenant exista
            var tenantExists = await _context.Tenants.AnyAsync(t => t.TenantId == examen.TenantId);
            if (!tenantExists)
            {
                return BadRequest(new { message = "El Tenant especificado no existe." });
            }

            examen.ExamenId = Guid.NewGuid();

            _context.ExamenesCatalogo.Add(examen);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExamen), new { id = examen.ExamenId }, examen);
        }
    }
}