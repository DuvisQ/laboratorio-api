using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using OfficeOpenXml;

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
            // Configurar licencia no comercial para EPPlus 8
            ExcelPackage.License.SetNonCommercialOrganization("BitCore");
        }

        // GET: api/examenes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamenCatalogo>>> GetExamenes()
        {
            return await _context.ExamenesCatalogo
                .Include(e => e.Parametros)
                .Include(e => e.Tenant)
                .Where(e => e.Activo)
                .ToListAsync();
        }

        // GET: api/examenes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExamenCatalogo>> GetExamen(Guid id)
        {
            var examen = await _context.ExamenesCatalogo
                .Include(e => e.Parametros)
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

        // POST: api/examenes/previsualizar-excel
        [HttpPost("previsualizar-excel")]
        public async Task<IActionResult> PrevisualizarExcelPrecios(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Por favor, seleccione un archivo Excel válido." });

            var cambiosPrevistos = new List<object>();

            using (var stream = new MemoryStream())
            {
                await archivo.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 6; row <= rowCount; row++)
                    {
                        var codigo = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var descripcion = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        
                        if (string.IsNullOrEmpty(codigo)) continue;

                        var observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out decimal costoDolares);
                        var tiempoRespuesta = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out decimal precioDolares);

                        var examenExistente = await _context.ExamenesCatalogo
                            .FirstOrDefaultAsync(e => e.CodigoProveedor == codigo);

                        if (examenExistente != null)
                        {
                            bool precioCambio = examenExistente.PrecioDolares != precioDolares || examenExistente.CostoDolares != costoDolares;

                            cambiosPrevistos.Add(new
                            {
                                codigo,
                                descripcion = examenExistente.NombreExamen,
                                precioAnterior = examenExistente.PrecioDolares,
                                precioNuevo = precioDolares,
                                costoNuevo = costoDolares,
                                estado = precioCambio ? "Modificado" : "Sin Cambios"
                            });
                        }
                        else
                        {
                            cambiosPrevistos.Add(new
                            {
                                codigo,
                                descripcion = descripcion ?? "Nuevo Examen",
                                precioAnterior = 0.0m,
                                precioNuevo = precioDolares,
                                costoNuevo = costoDolares,
                                estado = "Nuevo"
                            });
                        }
                    }
                }
            }

            return Ok(new { message = "Vista previa generada exitosamente", totalRegistros = cambiosPrevistos.Count, cambios = cambiosPrevistos });
        }

        // POST: api/examenes/sincronizar-precios
        [HttpPost("sincronizar-precios")]
        public async Task<IActionResult> SincronizarPreciosExcel(IFormFile archivo, [FromQuery] Guid tenantId)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Seleccione un archivo Excel." });

            int actualizados = 0;
            int insertados = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await archivo.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 4; row <= rowCount; row++)
                        {
                            var codigo = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(codigo)) continue;

                            var descripcion = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "Examen sin descripción";
                            var observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                            
                            decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out decimal costoDolares);
                            var tiempoRespuesta = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                            decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out decimal precioDolares);

                            var examen = await _context.ExamenesCatalogo
                                .FirstOrDefaultAsync(e => e.CodigoProveedor == codigo && e.TenantId == tenantId);

                            if (examen != null)
                            {
                                examen.CostoDolares = costoDolares;
                                examen.PrecioDolares = precioDolares;
                                examen.Observaciones = observaciones;
                                examen.TiempoRespuesta = tiempoRespuesta;
                                actualizados++;
                            }
                            else
                            {
                                var nuevoExamen = new ExamenCatalogo
                                {
                                    TenantId = tenantId,
                                    CodigoProveedor = codigo,
                                    NombreExamen = descripcion,
                                    Observaciones = observaciones,
                                    Categoria = "General",
                                    CostoDolares = costoDolares,
                                    PrecioDolares = precioDolares,
                                    TiempoRespuesta = tiempoRespuesta,
                                    Activo = true
                                };
                                _context.ExamenesCatalogo.Add(nuevoExamen);
                                insertados++;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new 
                { 
                    message = "Sincronización de precios completada con éxito.", 
                    registrosActualizados = actualizados, 
                    registrosNuevos = insertados 
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error procesando el archivo: {ex.Message}");
            }
        }
    }
}