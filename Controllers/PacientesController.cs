using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;

namespace Laboratorio.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PacientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/pacientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes()
        {
            return await _context.Pacientes.Include(p => p.Tenant).ToListAsync();
        }

        // GET: api/pacientes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Paciente>> GetPaciente(Guid id)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.PacienteId == id);

            if (paciente == null)
            {
                return NotFound();
            }

            return paciente;
        }

        // POST: api/pacientes
        [HttpPost]
        public async Task<ActionResult<Paciente>> PostPaciente(Paciente paciente)
        {
            // Validar que el Tenant exista antes de asociar al paciente
            var tenantExists = await _context.Tenants.AnyAsync(t => t.TenantId == paciente.TenantId);
            if (!tenantExists)
            {
                return BadRequest(new { message = "El Tenant especificado no existe." });
            }

            paciente.PacienteId = Guid.NewGuid();
            paciente.FechaRegistro = DateTime.UtcNow;

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaciente), new { id = paciente.PacienteId }, paciente);
        }
    }
}