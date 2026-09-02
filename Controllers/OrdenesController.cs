using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Ordenes;
using Microsoft.AspNetCore.Authorization;

namespace Laboratorio.Api.Controllers
{
    [Authorize] // Requiere estar logueado al menos
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
        public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pacienteExiste = await _context.Pacientes
                .AnyAsync(p => p.PacienteId == dto.PacienteId && p.TenantId == dto.TenantId);
            
            if (!pacienteExiste)
                return NotFound("El paciente no existe o no pertenece a esta clínica.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var hoy = DateTime.UtcNow.Date;
                
                var ultimoCorrelativo = await _context.OrdenesLaboratorio
                    .Where(o => o.TenantId == dto.TenantId && o.FechaOrden.Date == hoy)
                    .MaxAsync(o => (int?)o.CorrelativoDiario) ?? 0;

                var nuevoCorrelativo = ultimoCorrelativo + 1;

                var nuevaOrden = new OrdenLaboratorio
                {
                    TenantId = dto.TenantId,
                    PacienteId = dto.PacienteId,
                    MotivoExamen = dto.MotivoExamen,
                    CorrelativoDiario = nuevoCorrelativo,
                    Estado = "Registrada",
                    FechaOrden = DateTime.UtcNow
                };

                _context.OrdenesLaboratorio.Add(nuevaOrden);
                await _context.SaveChangesAsync();

                var detalles = new List<ResultadoDetalle>();
                foreach (var examenId in dto.ExamenesIds)
                {
                    detalles.Add(new ResultadoDetalle
                    {
                        TenantId = dto.TenantId,
                        OrdenId = nuevaOrden.OrdenId,
                        ExamenId = examenId,
                        EstadoTercerizado = "Interno",
                        FechaCarga = DateTime.UtcNow
                    });
                }

                _context.ResultadosDetalle.AddRange(detalles);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new 
                { 
                    message = "Orden creada exitosamente", 
                    ordenId = nuevaOrden.OrdenId, 
                    correlativo = nuevaOrden.CorrelativoDiario 
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno al crear la orden: {ex.Message}");
            }
        }

        // PATCH: api/ordenes/{id}/validar
        [HttpPatch("{id}/validar")]
        public async Task<IActionResult> ValidarOrden(Guid id, [FromBody] ValidacionOrdenDto dto)
        {
            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Resultados)
                .FirstOrDefaultAsync(o => o.OrdenId == id);

            if (orden == null)
                return NotFound(new { message = "La orden no existe." });

            if (orden.Estado == "Validada")
                return BadRequest(new { message = "Esta orden ya se encuentra validada y bloqueada." });

            if (orden.Estado != "Procesada")
                return BadRequest(new { message = "La orden debe estar 'Procesada' para poder validarse." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!string.IsNullOrEmpty(dto.ObservacionBioanalista))
                {
                    orden.ObservacionBioanalista = dto.ObservacionBioanalista;
                }

                if (dto.Resultados != null && orden.Resultados != null)
                {
                    foreach (var itemValidado in dto.Resultados)
                    {
                        var resultadoBd = orden.Resultados.FirstOrDefault(r => r.ResultadoId == itemValidado.ResultadoId);
                        
                        if (resultadoBd != null)
                        {
                            resultadoBd.ValorResultado = itemValidado.ValorResultado;
                            resultadoBd.RangoReferenciaAplicado = itemValidado.RangoReferenciaAplicado;
                            resultadoBd.TecnicaAplicada = itemValidado.TecnicaAplicada;
                            resultadoBd.FueraDeRango = itemValidado.FueraDeRango;
                            resultadoBd.UuidBioanalista = dto.UuidBioanalista;
                        }
                    }
                }

                orden.Estado = "Validada";
                orden.FechaValidacionFinal = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new 
                { 
                    message = "Orden validada exitosamente. Lista para el reporte final.", 
                    ordenId = orden.OrdenId 
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno al validar: {ex.Message}");
            }
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
                PacienteSexo = orden.Paciente?.Sexo ?? "N/A"
            };

            if (orden.Resultados != null && orden.Resultados.Any())
            {
                reporte.Categorias = orden.Resultados
                    .Where(r => r.Examen != null)
                    .GroupBy(r => r.Examen!.Categoria ?? "Sin Categoría")
                    .Select(grupo => new CategoriaReporteDto
                    {
                        Categoria = grupo.Key,
                        Resultados = grupo.Select(r => new ResultadoReporteDto
                        {
                            // Nota: Si el resultado está vinculado a un parámetro específico del examen, 
                            // aquí accedemos a través de la colección de parámetros o del parámetro asociado.
                            ExamenNombre = r.Examen!.NombreExamen ?? "Desconocido",
                            Valor = r.ValorResultado ?? string.Empty,
                            Unidades = string.Empty, // Se mapeará desde el parámetro clínico correspondiente
                            RangoReferencia = r.RangoReferenciaAplicado ?? string.Empty,
                            Tecnica = r.TecnicaAplicada ?? string.Empty,
                            FueraDeRango = r.FueraDeRango
                        }).ToList()
                    }).ToList();
            }

            return Ok(reporte);
        }

        // PUT: api/ordenes/{id}/resultados
        [HttpPut("{id}/resultados")]
        public async Task<IActionResult> IngresarResultados(Guid id, [FromBody] IngresarResultadosDto dto)
        {
            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Resultados)
                .FirstOrDefaultAsync(o => o.OrdenId == id);

            if (orden == null)
                return NotFound(new { message = "La orden no existe." });

            if (orden.Estado == "Validada")
                return BadRequest(new { message = "Esta orden ya fue validada y no se puede modificar." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                orden.ObservacionBioanalista = dto.ObservacionBioanalista;
                orden.Estado = "Procesada";

                foreach (var item in dto.Resultados)
                {
                    var resultadoBd = orden.Resultados?.FirstOrDefault(r => r.ResultadoId == item.ResultadoId);
                    if (resultadoBd != null)
                    {
                        resultadoBd.ValorResultado = item.ValorResultado;

                        // Actualizamos el estado de la muestra (si la trajo o quedó pendiente)
                        resultadoBd.MuestraEntregada = item.MuestraEntregada;
                        
                        if (!string.IsNullOrEmpty(item.ValorResultado))
                        {
                            resultadoBd.FechaCarga = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Resultados guardados exitosamente", ordenId = orden.OrdenId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al guardar los resultados: {ex.Message}");
            }
        }
    }
}