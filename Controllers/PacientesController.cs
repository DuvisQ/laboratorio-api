using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using System.IO;

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

        // GET: api/Pacientes/buscar
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarPacientes([FromQuery] Guid tenantId, [FromQuery] string termino)
        {
            if (tenantId == Guid.Empty)
                return BadRequest("El TenantId es obligatorio.");

            if (string.IsNullOrWhiteSpace(termino))
                return BadRequest("Debe ingresar un término (cédula, nombre o apellido) para buscar.");

            // 1. Limpiamos el término de búsqueda de entrada (le quitamos espacios, puntos y guiones)
            var busqueda = termino.Trim().ToLower();
            var busquedaLimpia = busqueda.Replace(".", "").Replace("-", "").Replace(" ", "");

            var query = _context.Pacientes.Where(p => p.TenantId == tenantId).AsQueryable();

            // 2. Comparamos limpiando también la cédula de la base de datos "al vuelo"
            // PostgreSQL traducirá estos Replace a funciones SQL nativas súper rápidas
            var pacientes = await query
                .Where(p => p.Cedula.ToLower().Replace(".", "").Replace("-", "").Replace(" ", "").Contains(busquedaLimpia) 
                         || p.NombreCompleto.ToLower().Contains(busqueda))
                .OrderBy(p => p.NombreCompleto)
                .Take(20)
                .ToListAsync();

            if (!pacientes.Any())
                return NotFound(new { message = "No se encontraron pacientes que coincidan con la búsqueda." });

            return Ok(pacientes);
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

        // POST: api/pacientes/importar
        // Este endpoint es temporal para cargar la data histórica del Excel.
        [HttpPost("importar")]
        public async Task<IActionResult> ImportarPacientesExcel(IFormFile archivoExcel, [FromQuery] Guid tenantId)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
                return BadRequest("Por favor, seleccione un archivo válido.");

            if (tenantId == Guid.Empty)
                return BadRequest("El TenantId es obligatorio.");

            var pacientesNuevos = new List<Paciente>();
            int filasProcesadas = 0;

            try
            {
                // ¡NUEVO! Obtenemos todas las cédulas que ya existen en la BD para esta clínica
                var cedulasExistentes = _context.Pacientes
                    .Where(p => p.TenantId == tenantId)
                    .Select(p => p.Cedula)
                    .ToList();

                using (var stream = new MemoryStream())
                {
                    await archivoExcel.CopyToAsync(stream);
                    
                    using (var workbook = new XLWorkbook(stream))
                    {
                        foreach (var worksheet in workbook.Worksheets)
                        {
                            var nombrePestana = worksheet.Name;
                            var rangoUsado = worksheet.RangeUsed();
                            
                            if (rangoUsado == null) continue; 

                            var filas = rangoUsado.RowsUsed().Skip(1); 

                            foreach (var fila in filas)
                            {
                                var apellidos = fila.Cell(1).GetString().Trim();
                                var nombres = fila.Cell(2).GetString().Trim();
                                var cedula = fila.Cell(3).GetString().Trim();

                                if (string.IsNullOrEmpty(apellidos) && string.IsNullOrEmpty(nombres)) continue;

                                // ¡NUEVO! Si no tiene cédula, generamos una única (SC = Sin Cédula + letras aleatorias)
                                if (string.IsNullOrEmpty(cedula)) 
                                {
                                    cedula = $"SC-{Guid.NewGuid().ToString().Substring(0, 8)}";
                                }
                                
                                if (cedula.Length > 20) cedula = cedula.Substring(0, 20);

                                // ¡NUEVO! Validación anti-duplicados (BD y Excel actual)
                                if (cedulasExistentes.Contains(cedula) || pacientesNuevos.Any(p => p.Cedula == cedula))
                                {
                                    Console.WriteLine($"⚠️ Duplicado saltado: Cédula '{cedula}' - {apellidos} {nombres}");
                                    continue;
                                }

                                var telefono = fila.Cell(4).GetString().Trim();
                                if (telefono.Length > 20) telefono = telefono.Substring(0, 20);

                                var ubicacionFisica = string.IsNullOrEmpty(fila.Cell(8).GetString().Trim()) 
                                                           ? nombrePestana 
                                                           : fila.Cell(8).GetString().Trim();
                                if (ubicacionFisica.Length > 50) ubicacionFisica = ubicacionFisica.Substring(0, 50);

                                var paciente = new Paciente
                                {
                                    PacienteId = Guid.NewGuid(),
                                    TenantId = tenantId,
                                    NombreCompleto = $"{apellidos} {nombres}".Trim(),
                                    Cedula = cedula,
                                    TelefonoPrincipal = telefono,
                                    NumeroHistoria = fila.Cell(5).GetString().Trim(),
                                    NumeroHistoriaFisica = ubicacionFisica,
                                    Sexo = "X", 
                                    FechaRegistro = DateTime.UtcNow
                                };

                                pacientesNuevos.Add(paciente);
                                filasProcesadas++;
                            }
                        }
                    }
                }

                if (pacientesNuevos.Any())
                {
                    await _context.Pacientes.AddRangeAsync(pacientesNuevos);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = $"Importación exitosa. Se procesaron y guardaron {filasProcesadas} pacientes." });
            }
            catch (Exception ex)
            {
                var mensajeError = ex.Message;
                if (ex.InnerException != null) mensajeError += $" | Detalle interno: {ex.InnerException.Message}";
                return StatusCode(500, $"Error interno durante la importación: {mensajeError}");
            }
        }
       
    }
}