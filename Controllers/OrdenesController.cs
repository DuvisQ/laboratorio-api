using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Ordenes;

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

            // --- INICIO DE LA SOLUCIÓN DEL CORRELATIVO ---
            // 1. Definimos el rango del día actual (00:00 a 23:59)
            var inicioDia = DateTime.UtcNow.Date;
            var finDia = inicioDia.AddDays(1);

            // 2. Buscamos el número más alto de hoy para esta clínica específica
            var ultimoCorrelativo = await _context.OrdenesLaboratorio
                .Where(o => o.TenantId == orden.TenantId && o.FechaOrden >= inicioDia && o.FechaOrden < finDia)
                .MaxAsync(o => (int?)o.CorrelativoDiario) ?? 0;

            // 3. Asignamos el siguiente número (ignorando lo que venga del JSON)
            orden.CorrelativoDiario = ultimoCorrelativo + 1;
            // --- FIN DE LA SOLUCIÓN ---

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

        // PATCH: api/ordenes/{id}/validar
        [HttpPatch("{id}/validar")]
        public async Task<IActionResult> ValidarOrden(Guid id, [FromBody] ValidacionOrdenDto dto)
        {
            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Resultados)
                .FirstOrDefaultAsync(o => o.OrdenId == id);

            if (orden == null)
            {
                return NotFound(new { message = "La orden de laboratorio especificada no existe." });
            }

            orden.Estado = "Validada";
            orden.ObservacionBioanalista = dto.ObservacionBioanalista;
            orden.FechaValidacionFinal = DateTime.UtcNow;

            if (dto.Resultados != null)
            {
                foreach (var resDto in dto.Resultados)
                {
                    var resultadoExistente = orden.Resultados?.FirstOrDefault(r => r.ResultadoId == resDto.ResultadoId);
                    if (resultadoExistente != null)
                    {
                        resultadoExistente.ValorResultado = resDto.ValorResultado;
                        resultadoExistente.RangoReferenciaAplicado = resDto.RangoReferenciaAplicado;
                        resultadoExistente.TecnicaAplicada = resDto.TecnicaAplicada;
                        resultadoExistente.FueraDeRango = resDto.FueraDeRango;
                        resultadoExistente.UuidBioanalista = dto.UuidBioanalista;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Orden validada exitosamente por el bioanalista.", ordenId = orden.OrdenId, estado = orden.Estado });
        }

        // GET: api/ordenes/{id}/reporte
        [HttpGet("{id}/reporte")]
        public async Task<ActionResult<ReporteOrdenDto>> GetReporteOrden(Guid id)
        {
            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Paciente)
                .Include(o => o.Resultados!)
                    .ThenInclude(r => r.Examen)
                .FirstOrDefaultAsync(o => o.OrdenId == id);

            if (orden == null)
            {
                return NotFound(new { message = "La orden de laboratorio especificada no existe." });
            }

            // Mapeamos las entidades a nuestro DTO de reporte
            var reporte = new ReporteOrdenDto
            {
                OrdenId = orden.OrdenId,
                CorrelativoDiario = orden.CorrelativoDiario,
                FechaOrden = orden.FechaOrden,
                FechaValidacion = orden.FechaValidacionFinal,
                Estado = orden.Estado,
                ObservacionBioanalista = orden.ObservacionBioanalista ?? string.Empty,
                
                PacienteNombre = orden.Paciente?.NombreCompleto ?? "Desconocido",
                PacienteCedula = orden.Paciente?.Cedula ?? "N/A",
                PacienteSexo = orden.Paciente?.Sexo ?? "N/A",

                Resultados = orden.Resultados?.Select(r => new ResultadoReporteDto
                {
                    Categoria = r.Examen?.Categoria ?? "Sin Categoría",
                    ExamenNombre = r.Examen?.NombreParametro ?? "Desconocido",
                    Valor = r.ValorResultado ?? string.Empty,
                    Unidades = r.Examen?.Unidades ?? string.Empty,
                    RangoReferencia = r.RangoReferenciaAplicado ?? r.Examen?.RangoReferenciaDefecto ?? string.Empty,
                    Tecnica = r.TecnicaAplicada ?? r.Examen?.TecnicaDefecto ?? string.Empty,
                    FueraDeRango = r.FueraDeRango
                }).ToList() ?? new List<ResultadoReporteDto>()
            };

            return Ok(reporte);
        }
    }
}