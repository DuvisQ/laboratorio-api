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
        public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validamos que el paciente exista en esa clínica
            var pacienteExiste = await _context.Pacientes
                .AnyAsync(p => p.PacienteId == dto.PacienteId && p.TenantId == dto.TenantId);
            
            if (!pacienteExiste)
                return NotFound("El paciente no existe o no pertenece a esta clínica.");

            // Iniciamos la Transacción Segura
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Calcular el Correlativo Diario (el número de tubo del día)
                // Obtenemos la fecha de hoy. EF Core traducirá esto a la BD.
                var hoy = DateTime.UtcNow.Date;
                
                var ultimoCorrelativo = await _context.OrdenesLaboratorio
                    .Where(o => o.TenantId == dto.TenantId && o.FechaOrden.Date == hoy)
                    .MaxAsync(o => (int?)o.CorrelativoDiario) ?? 0;

                var nuevoCorrelativo = ultimoCorrelativo + 1;

                // 2. Crear la Orden (Cabecera)
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
                await _context.SaveChangesAsync(); // Guardamos para que se genere el OrdenId

                // 3. Crear los Resultados (Detalles de los exámenes seleccionados)
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
                await _context.SaveChangesAsync(); // Guardamos los hijos

                // 4. Confirmamos la transacción (¡Todo salió perfecto!)
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
                // Si algo explota (falla de BD, llave foránea mala, etc.), deshacemos todo
                await transaction.RollbackAsync();
                
                // Nota: En producción podríamos usar un ILogger aquí
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