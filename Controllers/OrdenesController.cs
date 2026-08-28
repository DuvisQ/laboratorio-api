using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;

namespace Laboratorio.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdenesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ordenes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrdenLaboratorio>>> GetOrdenes()
        {
            return await _context.OrdenesLaboratorio
                .Include(o => o.Paciente)
                .Include(o => o.Resultados!)
                    .ThenInclude(r => r.Examen)
                .ToListAsync();
        }

        // GET: api/ordenes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<OrdenLaboratorio>> GetOrden(Guid id)
        {
            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Paciente)
                .Include(o => o.Resultados!)
                    .ThenInclude(r => r.Examen)
                .FirstOrDefaultAsync(o => o.OrdenId == id);

            if (orden == null)
            {
                return NotFound();
            }

            return orden;
        }

        // POST: api/ordenes
        [HttpPost]
        public async Task<ActionResult<OrdenLaboratorio>> PostOrden(OrdenLaboratorio orden)
        {
            // Validar que el Tenant exista
            var tenantExists = await _context.Tenants.AnyAsync(t => t.TenantId == orden.TenantId);
            if (!tenantExists)
            {
                return BadRequest(new { message = "El Tenant especificado no existe." });
            }

            // Validar que el Paciente exista
            var pacienteExists = await _context.Pacientes.AnyAsync(p => p.PacienteId == orden.PacienteId);
            if (!pacienteExists)
            {
                return BadRequest(new { message = "El Paciente especificado no existe." });
            }

            orden.OrdenId = Guid.NewGuid();
            orden.FechaOrden = DateTime.UtcNow;
            
            // Asegurar que la ruta no sea nula para cumplir con la restricción de base de datos
            orden.RutaArchivoExterno ??= string.Empty;

            // Asegurar IDs en los detalles si vienen vacíos
            if (orden.Resultados != null)
            {
                foreach (var resultado in orden.Resultados)
                {
                    resultado.ResultadoId = Guid.NewGuid();
                    resultado.OrdenId = orden.OrdenId;
                    resultado.TenantId = orden.TenantId;
                    resultado.FechaCarga = DateTime.UtcNow;
                }
            }

            _context.OrdenesLaboratorio.Add(orden);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrden), new { id = orden.OrdenId }, orden);
        }
    }
}