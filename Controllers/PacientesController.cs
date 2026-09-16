using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace Laboratorio.Api.Controllers
{
    [Authorize(Roles = "Administrador,Bioanalista,Secretaria")]
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : BaseController // ¡Hereda de BaseController!
    {
        private readonly AppDbContext _context;

        public PacientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/pacientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes([FromQuery] int limite = 50)
        {
            // El filtro por TenantId se aplica de forma transparente con el Global Query Filter de AppDbContext
            // Se elimina el Include(Tenant) que serializaba grafos cíclicos pesados y causaba demoras de 19s
            return await _context.Pacientes
                .AsNoTracking()
                .OrderByDescending(p => p.FechaRegistro)
                .Take(limite > 200 ? 200 : limite)
                .ToListAsync();
        }

        // GET: api/pacientes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Paciente>> GetPaciente(Guid id)
        {
            var paciente = await _context.Pacientes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PacienteId == id);

            if (paciente == null)
            {
                return NotFound();
            }

            return paciente;
        }

        // GET: api/Pacientes/buscar
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarPacientes([FromQuery] string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return BadRequest("Debe ingresar un término (cédula, nombre o apellido) para buscar.");

            var busqueda = termino.Trim().ToLower();
            var busquedaLimpia = busqueda.Replace(".", "").Replace("-", "").Replace(" ", "");

            var pacientes = await _context.Pacientes
                .AsNoTracking()
                .Where(p => p.Cedula.ToLower().Replace(".", "").Replace("-", "").Replace(" ", "").Contains(busquedaLimpia) 
                         || p.NombreCompleto.ToLower().Contains(busqueda))
                .OrderBy(p => p.NombreCompleto)
                .Take(30)
                .ToListAsync();

            if (!pacientes.Any())
                return NotFound(new { message = "No se encontraron pacientes que coincidan con la búsqueda." });

            return Ok(pacientes);
        }

        private static void NormalizarPacienteMayusculas(Paciente paciente)
        {
            if (paciente == null) return;

            paciente.Cedula = paciente.Cedula?.Trim().ToUpper() ?? string.Empty;
            paciente.NombreCompleto = paciente.NombreCompleto?.Trim().ToUpper() ?? string.Empty;
            paciente.Sexo = paciente.Sexo?.Trim().ToUpper() ?? string.Empty;
            paciente.TelefonoPrincipal = paciente.TelefonoPrincipal?.Trim() ?? string.Empty;
            paciente.TelefonoRepresentante = paciente.TelefonoRepresentante?.Trim() ?? string.Empty;
            paciente.Direccion = paciente.Direccion?.Trim().ToUpper() ?? string.Empty;
            paciente.NumeroHistoria = string.IsNullOrWhiteSpace(paciente.NumeroHistoria) ? null : paciente.NumeroHistoria.Trim().ToUpper();
            paciente.NumeroHistoriaFisica = string.IsNullOrWhiteSpace(paciente.NumeroHistoriaFisica) ? null : paciente.NumeroHistoriaFisica.Trim().ToUpper();
            paciente.NombreRepresentante = string.IsNullOrWhiteSpace(paciente.NombreRepresentante) ? null : paciente.NombreRepresentante.Trim().ToUpper();
            paciente.CedulaRepresentante = string.IsNullOrWhiteSpace(paciente.CedulaRepresentante) ? null : paciente.CedulaRepresentante.Trim().ToUpper();
            paciente.ParentescoRepresentante = string.IsNullOrWhiteSpace(paciente.ParentescoRepresentante) ? null : paciente.ParentescoRepresentante.Trim().ToUpper();
        }

        // POST: api/pacientes
        [HttpPost]
        public async Task<ActionResult<Paciente>> PostPaciente(Paciente paciente)
        {
            var tenantId = ObtenerTenantIdDelToken(); 
            paciente.TenantId = tenantId;

            NormalizarPacienteMayusculas(paciente);

            // Validar cédula única por clínica (Tenant)
            var yaExiste = await _context.Pacientes.AnyAsync(p => p.Cedula == paciente.Cedula);
            if (yaExiste)
            {
                return BadRequest(new { message = $"Ya existe un paciente registrado con la cédula {paciente.Cedula}." });
            }

            // Validar que el Tenant exista en la base de datos
            var tenantExists = await _context.Tenants.AnyAsync(t => t.TenantId == tenantId);
            if (!tenantExists)
            {
                return BadRequest(new { message = "La clínica asociada a este usuario no existe." });
            }

            paciente.PacienteId = Guid.NewGuid();
            paciente.FechaRegistro = DateTime.UtcNow;

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaciente), new { id = paciente.PacienteId }, paciente);
        }

        // PUT: api/pacientes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaciente(Guid id, Paciente paciente)
        {
            var pacienteExistente = await _context.Pacientes.FirstOrDefaultAsync(p => p.PacienteId == id);
            if (pacienteExistente == null)
            {
                return NotFound(new { message = "Paciente no encontrado." });
            }

            NormalizarPacienteMayusculas(paciente);

            // Si cambió la cédula (ej. menor que obtuvo su cédula oficial), verificar que no esté repetida
            if (pacienteExistente.Cedula != paciente.Cedula)
            {
                var yaExiste = await _context.Pacientes.AnyAsync(p => p.Cedula == paciente.Cedula && p.PacienteId != id);
                if (yaExiste)
                {
                    return BadRequest(new { message = $"Ya existe otro paciente registrado con la cédula {paciente.Cedula}." });
                }
                pacienteExistente.Cedula = paciente.Cedula;
            }

            pacienteExistente.NombreCompleto = paciente.NombreCompleto;
            pacienteExistente.Sexo = paciente.Sexo;
            pacienteExistente.FechaNacimiento = paciente.FechaNacimiento;
            pacienteExistente.TelefonoPrincipal = paciente.TelefonoPrincipal;
            pacienteExistente.TelefonoRepresentante = paciente.TelefonoRepresentante;
            pacienteExistente.Direccion = paciente.Direccion;
            pacienteExistente.NumeroHistoria = paciente.NumeroHistoria;
            pacienteExistente.NumeroHistoriaFisica = paciente.NumeroHistoriaFisica;
            pacienteExistente.NombreRepresentante = paciente.NombreRepresentante;
            pacienteExistente.CedulaRepresentante = paciente.CedulaRepresentante;
            pacienteExistente.ParentescoRepresentante = paciente.ParentescoRepresentante;

            await _context.SaveChangesAsync();

            return Ok(pacienteExistente);
        }

        // POST: api/pacientes/importar
        // Este endpoint es temporal para cargar la data histórica del Excel.
        [HttpPost("importar")]
        public async Task<IActionResult> ImportarPacientesExcel(IFormFile archivoExcel) // Eliminado el [FromQuery] tenantId
        {
            var tenantId = ObtenerTenantIdDelToken(); // Extraído del token

            if (archivoExcel == null || archivoExcel.Length == 0)
                return BadRequest("Por favor, seleccione un archivo válido.");

            var pacientesNuevos = new List<Paciente>();
            int filasProcesadas = 0;

            try
            {
                // Obtenemos todas las cédulas que ya existen en la BD para esta clínica
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

                                // Si no tiene cédula, generamos una única (SC = Sin Cédula + letras aleatorias)
                                if (string.IsNullOrEmpty(cedula)) 
                                {
                                    cedula = $"SC-{Guid.NewGuid().ToString().Substring(0, 8)}";
                                }
                                
                                if (cedula.Length > 20) cedula = cedula.Substring(0, 20);

                                // Validación anti-duplicados (BD y Excel actual)
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
                                    TenantId = tenantId, // Forzado desde el token
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