using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Examenes;
using OfficeOpenXml;

namespace Laboratorio.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExamenesController : BaseController
    {
        private readonly AppDbContext _context;

        public ExamenesController(AppDbContext context)
        {
            _context = context;
            ExcelPackage.License.SetNonCommercialOrganization("BitCore");
        }

        // GET: api/Examenes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamenDto>>> GetExamenes(
            [FromQuery] string? buscar,
            [FromQuery] string? categoria,
            [FromQuery] string? tipo,
            [FromQuery] bool? activo)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var query = _context.ExamenesCatalogo
                .AsNoTracking()
                .Include(e => e.Parametros)
                .Where(e => e.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                query = query.Where(e => e.CodigoProveedor.ToLower().Contains(b) || e.NombreExamen.ToLower().Contains(b));
            }

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                var cat = categoria.Trim().ToLower();
                query = query.Where(e => e.Categoria.ToLower() == cat);
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                var t = tipo.Trim().ToLower();
                if (t == "rutina")
                    query = query.Where(e => !e.EsTercerizado);
                else if (t == "especial")
                    query = query.Where(e => e.EsTercerizado);
            }

            if (activo.HasValue)
            {
                query = query.Where(e => e.Activo == activo.Value);
            }

            var examenes = await query
                .OrderBy(e => e.CodigoProveedor)
                .Select(e => new ExamenDto
                {
                    ExamenId = e.ExamenId,
                    CodigoProveedor = e.CodigoProveedor,
                    NombreExamen = e.NombreExamen,
                    Observaciones = e.Observaciones,
                    Categoria = e.Categoria,
                    CostoDolares = e.CostoDolares,
                    TiempoRespuesta = e.TiempoRespuesta,
                    PrecioDolares = e.PrecioDolares,
                    EsTercerizado = e.EsTercerizado,
                    LaboratorioDestino = e.LaboratorioDestino,
                    Activo = e.Activo,
                    Parametros = e.Parametros.OrderBy(p => p.Orden).Select(p => new ExamenParametroDto
                    {
                        ExamenParametroId = p.ExamenParametroId,
                        NombreParametro = p.NombreParametro,
                        Unidades = p.Unidades,
                        RangoReferenciaDefecto = p.RangoReferenciaDefecto,
                        TecnicaDefecto = p.TecnicaDefecto,
                        Orden = p.Orden
                    }).ToList()
                })
                .ToListAsync();

            return Ok(examenes);
        }

        // GET: api/Examenes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExamenDto>> GetExamen(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var examen = await _context.ExamenesCatalogo
                .AsNoTracking()
                .Include(e => e.Parametros)
                .FirstOrDefaultAsync(e => e.ExamenId == id && e.TenantId == tenantId);

            if (examen == null)
                return NotFound(new { message = "Examen no encontrado." });

            var dto = new ExamenDto
            {
                ExamenId = examen.ExamenId,
                CodigoProveedor = examen.CodigoProveedor,
                NombreExamen = examen.NombreExamen,
                Observaciones = examen.Observaciones,
                Categoria = examen.Categoria,
                CostoDolares = examen.CostoDolares,
                TiempoRespuesta = examen.TiempoRespuesta,
                PrecioDolares = examen.PrecioDolares,
                EsTercerizado = examen.EsTercerizado,
                LaboratorioDestino = examen.LaboratorioDestino,
                Activo = examen.Activo,
                Parametros = examen.Parametros.OrderBy(p => p.Orden).Select(p => new ExamenParametroDto
                {
                    ExamenParametroId = p.ExamenParametroId,
                    NombreParametro = p.NombreParametro,
                    Unidades = p.Unidades,
                    RangoReferenciaDefecto = p.RangoReferenciaDefecto,
                    TecnicaDefecto = p.TecnicaDefecto,
                    Orden = p.Orden
                }).ToList()
            };

            return Ok(dto);
        }

        // GET: api/Examenes/categorias
        [HttpGet("categorias")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategorias()
        {
            var tenantId = ObtenerTenantIdDelToken();

            var categorias = await _context.ExamenesCatalogo
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId && !string.IsNullOrWhiteSpace(e.Categoria))
                .Select(e => e.Categoria.Trim())
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Lista base predeterminada si no hay registros
            if (!categorias.Any())
            {
                categorias = new List<string>
                {
                    "Hematología",
                    "Química Sanguínea",
                    "Uroanálisis",
                    "Coproanálisis",
                    "Inmunología y Serología",
                    "Hormonas",
                    "Marcadores Tumorales",
                    "Perfiles Especiales"
                };
            }

            return Ok(categorias);
        }

        // GET: api/Examenes/siguiente-codigo
        [HttpGet("siguiente-codigo")]
        public async Task<ActionResult<object>> ObtenerSiguienteCodigo([FromQuery] string prefijo = "SALV")
        {
            var tenantId = ObtenerTenantIdDelToken();
            var pref = string.IsNullOrWhiteSpace(prefijo) ? "SALV" : prefijo.Trim().ToUpper();

            var codigos = await _context.ExamenesCatalogo
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.CodigoProveedor.StartsWith(pref + "-"))
                .Select(e => e.CodigoProveedor)
                .ToListAsync();

            int maxNumero = 0;
            var regex = new Regex($@"^{Regex.Escape(pref)}-(\d+)$", RegexOptions.IgnoreCase);

            foreach (var cod in codigos)
            {
                var match = regex.Match(cod.Trim());
                if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
                {
                    if (n > maxNumero) maxNumero = n;
                }
            }

            int siguiente = maxNumero + 1;
            string siguienteCodigo = $"{pref}-{siguiente:D5}";

            return Ok(new { siguienteCodigo, ultimoNumero = maxNumero });
        }

        // POST: api/Examenes
        [HttpPost]
        public async Task<ActionResult<ExamenDto>> PostExamen([FromBody] CrearExamenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = ObtenerTenantIdDelToken();
            var codigoLimpio = dto.CodigoProveedor.Trim().ToUpper();

            bool existeCodigo = await _context.ExamenesCatalogo
                .AnyAsync(e => e.TenantId == tenantId && e.CodigoProveedor.ToUpper() == codigoLimpio);

            if (existeCodigo)
                return BadRequest(new { message = $"El código '{codigoLimpio}' ya se encuentra registrado en el catálogo." });

            var nuevoExamen = new ExamenCatalogo
            {
                ExamenId = Guid.NewGuid(),
                TenantId = tenantId,
                CodigoProveedor = codigoLimpio,
                NombreExamen = dto.NombreExamen.Trim().ToUpper(),
                Observaciones = dto.Observaciones?.Trim(),
                Categoria = dto.Categoria?.Trim() ?? "General",
                CostoDolares = dto.CostoDolares,
                TiempoRespuesta = string.IsNullOrWhiteSpace(dto.TiempoRespuesta) ? "24 Horas" : dto.TiempoRespuesta.Trim(),
                PrecioDolares = dto.PrecioDolares,
                EsTercerizado = dto.EsTercerizado,
                LaboratorioDestino = dto.EsTercerizado ? dto.LaboratorioDestino?.Trim() : null,
                Activo = true
            };

            if (dto.Parametros != null && dto.Parametros.Any())
            {
                int orden = 1;
                foreach (var p in dto.Parametros)
                {
                    if (!string.IsNullOrWhiteSpace(p.NombreParametro))
                    {
                        nuevoExamen.Parametros.Add(new ExamenParametro
                        {
                            ExamenParametroId = Guid.NewGuid(),
                            ExamenId = nuevoExamen.ExamenId,
                            NombreParametro = p.NombreParametro.Trim(),
                            Unidades = p.Unidades?.Trim(),
                            RangoReferenciaDefecto = p.RangoReferenciaDefecto?.Trim(),
                            TecnicaDefecto = p.TecnicaDefecto?.Trim(),
                            Orden = p.Orden > 0 ? p.Orden : orden++
                        });
                    }
                }
            }

            _context.ExamenesCatalogo.Add(nuevoExamen);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExamen), new { id = nuevoExamen.ExamenId }, new ExamenDto
            {
                ExamenId = nuevoExamen.ExamenId,
                CodigoProveedor = nuevoExamen.CodigoProveedor,
                NombreExamen = nuevoExamen.NombreExamen,
                Observaciones = nuevoExamen.Observaciones,
                Categoria = nuevoExamen.Categoria,
                CostoDolares = nuevoExamen.CostoDolares,
                TiempoRespuesta = nuevoExamen.TiempoRespuesta,
                PrecioDolares = nuevoExamen.PrecioDolares,
                EsTercerizado = nuevoExamen.EsTercerizado,
                LaboratorioDestino = nuevoExamen.LaboratorioDestino,
                Activo = nuevoExamen.Activo,
                Parametros = nuevoExamen.Parametros.OrderBy(p => p.Orden).Select(p => new ExamenParametroDto
                {
                    ExamenParametroId = p.ExamenParametroId,
                    NombreParametro = p.NombreParametro,
                    Unidades = p.Unidades,
                    RangoReferenciaDefecto = p.RangoReferenciaDefecto,
                    TecnicaDefecto = p.TecnicaDefecto,
                    Orden = p.Orden
                }).ToList()
            });
        }

        // PUT: api/Examenes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExamen(Guid id, [FromBody] ActualizarExamenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = ObtenerTenantIdDelToken();
            var examen = await _context.ExamenesCatalogo
                .Include(e => e.Parametros)
                .FirstOrDefaultAsync(e => e.ExamenId == id && e.TenantId == tenantId);

            if (examen == null)
                return NotFound(new { message = "Examen no encontrado." });

            var codigoLimpio = dto.CodigoProveedor.Trim().ToUpper();
            bool existeOtro = await _context.ExamenesCatalogo
                .AnyAsync(e => e.TenantId == tenantId && e.ExamenId != id && e.CodigoProveedor.ToUpper() == codigoLimpio);

            if (existeOtro)
                return BadRequest(new { message = $"El código '{codigoLimpio}' ya pertenece a otro examen." });

            examen.CodigoProveedor = codigoLimpio;
            examen.NombreExamen = dto.NombreExamen.Trim().ToUpper();
            examen.Observaciones = dto.Observaciones?.Trim();
            examen.Categoria = dto.Categoria?.Trim() ?? "General";
            examen.CostoDolares = dto.CostoDolares;
            examen.TiempoRespuesta = string.IsNullOrWhiteSpace(dto.TiempoRespuesta) ? "24 Horas" : dto.TiempoRespuesta.Trim();
            examen.PrecioDolares = dto.PrecioDolares;
            examen.EsTercerizado = dto.EsTercerizado;
            examen.LaboratorioDestino = dto.EsTercerizado ? dto.LaboratorioDestino?.Trim() : null;
            examen.Activo = dto.Activo;

            // Actualizar parámetros analíticos
            _context.ExamenParametros.RemoveRange(examen.Parametros);

            if (dto.Parametros != null && dto.Parametros.Any())
            {
                int orden = 1;
                foreach (var p in dto.Parametros)
                {
                    if (!string.IsNullOrWhiteSpace(p.NombreParametro))
                    {
                        examen.Parametros.Add(new ExamenParametro
                        {
                            ExamenParametroId = Guid.NewGuid(),
                            ExamenId = examen.ExamenId,
                            NombreParametro = p.NombreParametro.Trim(),
                            Unidades = p.Unidades?.Trim(),
                            RangoReferenciaDefecto = p.RangoReferenciaDefecto?.Trim(),
                            TecnicaDefecto = p.TecnicaDefecto?.Trim(),
                            Orden = p.Orden > 0 ? p.Orden : orden++
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Examen actualizado exitosamente." });
        }

        // PATCH: api/Examenes/{id}/toggle-estado
        [HttpPatch("{id}/toggle-estado")]
        public async Task<IActionResult> ToggleEstado(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var examen = await _context.ExamenesCatalogo
                .FirstOrDefaultAsync(e => e.ExamenId == id && e.TenantId == tenantId);

            if (examen == null)
                return NotFound(new { message = "Examen no encontrado." });

            examen.Activo = !examen.Activo;
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = examen.Activo ? "Examen activado exitosamente." : "Examen desactivado del catálogo.",
                activo = examen.Activo 
            });
        }

        // DELETE: api/Examenes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarExamen(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var examen = await _context.ExamenesCatalogo
                .Include(e => e.Parametros)
                .FirstOrDefaultAsync(e => e.ExamenId == id && e.TenantId == tenantId);

            if (examen == null)
                return NotFound(new { message = "Examen no encontrado." });

            bool tieneResultados = await _context.ResultadosDetalle.AnyAsync(r => r.ExamenId == id);
            if (tieneResultados)
            {
                examen.Activo = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "El examen posee órdenes históricas asociadas. Se ha desactivado en su lugar.", desactivado = true });
            }

            _context.ExamenParametros.RemoveRange(examen.Parametros);
            _context.ExamenesCatalogo.Remove(examen);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Examen eliminado del catálogo exitosamente." });
        }

        // =========================================================================================
        // SEED INSTITUCIONAL: EXÁMENES DE RUTINA INTERNOS (SALV-00001 A SALV-00012)
        // =========================================================================================
        [HttpPost("seed-rutina-salv")]
        public async Task<IActionResult> SeedRutinaSalv()
        {
            var tenantId = ObtenerTenantIdDelToken();

            var examenesBase = new List<(string codigo, string nombre, string cat, decimal precio, string obs, List<(string param, string uni, string refVal)> parametros)>
            {
                ("SALV-00001", "PERFIL 20 COMPLETO", "Perfiles Especiales", 25.00m, "Ayunas estricto de 12 horas", new List<(string, string, string)>
                {
                    ("Hemoglobina", "g/dL", "H: 13.0 - 17.0 | M: 12.0 - 16.0"),
                    ("Hematocrito", "%", "H: 40 - 52 | M: 36 - 48"),
                    ("Leucocitos Totales", "/mm3", "5.000 - 10.000"),
                    ("Plaquetas", "/mm3", "150.000 - 450.000"),
                    ("Glicemia Basal", "mg/dL", "70 - 110"),
                    ("Urea", "mg/dL", "15 - 45"),
                    ("Creatinina", "mg/dL", "H: 0.7 - 1.3 | M: 0.6 - 1.1"),
                    ("Ácido Úrico", "mg/dL", "H: 3.5 - 7.2 | M: 2.6 - 6.0"),
                    ("Colesterol Total", "mg/dL", "< 200 Deseable"),
                    ("Triglicéridos", "mg/dL", "< 150 Normal"),
                    ("HDL Colesterol", "mg/dL", "> 40 Normal"),
                    ("LDL Colesterol", "mg/dL", "< 100 Óptimo"),
                    ("VLDL Colesterol", "mg/dL", "5 - 30"),
                    ("Bilirrubina Total", "mg/dL", "0.2 - 1.2"),
                    ("Bilirrubina Directa", "mg/dL", "0.0 - 0.3"),
                    ("Bilirrubina Indirecta", "mg/dL", "0.1 - 0.9"),
                    ("Transaminasa GOT (AST)", "U/L", "Hasta 40"),
                    ("Transaminasa GPT (ALT)", "U/L", "Hasta 41"),
                    ("Fosfatasa Alcalina", "U/L", "40 - 129"),
                    ("Examen General de Orina", "Aspecto", "Físico, Químico y Microscópico")
                }),

                ("SALV-00002", "HEMATOLOGÍA COMPLETA (HC)", "Hematología", 4.50m, "Muestra con EDTA. Dos horas después de comer o ayunas", new List<(string, string, string)>
                {
                    ("Hemoglobina", "g/dL", "H: 13.0 - 17.0 | M: 12.0 - 16.0"),
                    ("Hematocrito", "%", "H: 40 - 52 | M: 36 - 48"),
                    ("Cuenta Blanca (Leucocitos)", "/mm3", "5.000 - 10.000"),
                    ("Segmentados", "%", "55 - 65"),
                    ("Linfocitos", "%", "25 - 35"),
                    ("Monocitos", "%", "2 - 8"),
                    ("Eosinófilos", "%", "1 - 4"),
                    ("Basófilos", "%", "0 - 1"),
                    ("Recuento Plaquetario", "/mm3", "150.000 - 450.000"),
                    ("VCM", "fL", "80 - 96"),
                    ("HCM", "pg", "27 - 33"),
                    ("CHCM", "g/dL", "32 - 36")
                }),

                ("SALV-00003", "QUÍMICA SANGUÍNEA BASAL", "Química Sanguínea", 15.00m, "Suero libre de hemólisis. Ayunas", new List<(string, string, string)>
                {
                    ("Glicemia", "mg/dL", "70 - 110"),
                    ("Urea", "mg/dL", "15 - 45"),
                    ("Creatinina", "mg/dL", "H: 0.7 - 1.3 | M: 0.6 - 1.1"),
                    ("Ácido Úrico", "mg/dL", "H: 3.5 - 7.2 | M: 2.6 - 6.0"),
                    ("Colesterol Total", "mg/dL", "< 200"),
                    ("Triglicéridos", "mg/dL", "< 150")
                }),

                ("SALV-00004", "EXAMEN GENERAL DE ORINA Y HECES", "Uroanálisis", 7.00m, "Primera orina de la mañana y muestra fecal reciente", new List<(string, string, string)>
                {
                    ("Color Orina", "-", "Amarillo / Ámbar"),
                    ("Aspecto Orina", "-", "Límpido / Ligeramente turbio"),
                    ("Densidad Orina", "-", "1.010 - 1.025"),
                    ("pH Orina", "-", "5.0 - 7.0"),
                    ("Proteínas / Glucosa", "-", "Negativo"),
                    ("Sedimento: Leucocitos", "x cpo", "0 - 4"),
                    ("Sedimento: Hematíes", "x cpo", "0 - 2"),
                    ("Heces: Consistencia", "-", "Pastosa / Blanda"),
                    ("Heces: Examen Microscópico", "-", "No se observan parásitos quísticos ni trofozoitos")
                }),

                ("SALV-00005", "TIEMPOS DE COAGULACIÓN Y SEROLOGÍA (PT, PTT, HIV, VDRL)", "Hematología", 18.00m, "Plasma citratado y suero", new List<(string, string, string)>
                {
                    ("Tiempo de Protrombina (TP) Control", "seg", "12.0"),
                    ("Tiempo de Protrombina (TP) Paciente", "seg", "12.0 - 14.0"),
                    ("INR", "-", "0.9 - 1.2"),
                    ("Tiempo Tromboplastina Parcial (PTT) Control", "seg", "30.0"),
                    ("Tiempo Tromboplastina Parcial (PTT) Paciente", "seg", "28.0 - 35.0"),
                    ("HIV 1 y 2 (Prueba Rápida Inmunocromatográfica)", "-", "NO REACTIVO"),
                    ("VDRL (Serología RPR)", "-", "NO REACTIVO")
                }),

                ("SALV-00006", "SANGRE OCULTA EN HECES (PREVENCIÓN ONCOLÓGICA)", "Coproanálisis", 6.00m, "Muestra fecal sin ingesta previa de carnes rojas", new List<(string, string, string)>
                {
                    ("Sangre Oculta en Heces", "-", "NEGATIVO")
                }),

                ("SALV-00007", "TIPIAJE SANGUÍNEO (GRUPO Y FACTOR RH)", "Hematología", 5.00m, "Muestra con EDTA", new List<(string, string, string)>
                {
                    ("Grupo Sanguíneo", "-", "ABO"),
                    ("Factor Rh", "-", "Positivo / Negativo")
                }),

                ("SALV-00008", "SEROLOGÍA GENERAL (VDRL / HIV)", "Inmunología y Serología", 10.00m, "Suero", new List<(string, string, string)>
                {
                    ("VDRL", "-", "NO REACTIVO"),
                    ("HIV 1 y 2", "-", "NO REACTIVO")
                }),

                ("SALV-00009", "PRUEBA RÁPIDA DE DENGUE (NS1 / IgG / IgM)", "Inmunología y Serología", 12.00m, "Suero o plasma", new List<(string, string, string)>
                {
                    ("Antígeno Dengue NS1", "-", "NEGATIVO"),
                    ("Anticuerpos Dengue IgM", "-", "NEGATIVO"),
                    ("Anticuerpos Dengue IgG", "-", "NEGATIVO")
                }),

                ("SALV-00010", "REACCIONES DE WIDAL (TÍFICO Y PARATÍFICO)", "Inmunología y Serología", 8.00m, "Suero", new List<(string, string, string)>
                {
                    ("Antígeno Tífico 'O'", "Título", "< 1:80"),
                    ("Antígeno Tífico 'H'", "Título", "< 1:80"),
                    ("Antígeno Paratífico 'A'", "Título", "< 1:80"),
                    ("Antígeno Paratífico 'B'", "Título", "< 1:80")
                }),

                ("SALV-00011", "DEPURACIÓN DE CREATININA (ORINA 24 HORAS)", "Química Sanguínea", 10.00m, "Orina de 24 horas y muestra de suero en ayunas", new List<(string, string, string)>
                {
                    ("Creatinina en Suero", "mg/dL", "0.7 - 1.3"),
                    ("Creatinina en Orina", "mg/24h", "1000 - 2000"),
                    ("Volumen Urinario 24h", "mL", "800 - 2000"),
                    ("Aclaramiento de Creatinina", "mL/min", "H: 97 - 137 | M: 88 - 128")
                }),

                ("SALV-00012", "PRUEBA DE EMBARAZO (HCG)", "Inmunología y Serología", 6.00m, "Suero o primera orina de la mañana", new List<(string, string, string)>
                {
                    ("Fracción Beta HCG Cualitativa", "-", "NEGATIVO")
                })
            };

            int creados = 0;
            int omitidos = 0;

            foreach (var b in examenesBase)
            {
                bool existe = await _context.ExamenesCatalogo
                    .AnyAsync(e => e.TenantId == tenantId && e.CodigoProveedor == b.codigo);

                if (existe)
                {
                    omitidos++;
                    continue;
                }

                var nuevo = new ExamenCatalogo
                {
                    ExamenId = Guid.NewGuid(),
                    TenantId = tenantId,
                    CodigoProveedor = b.codigo,
                    NombreExamen = b.nombre,
                    Categoria = b.cat,
                    PrecioDolares = b.precio,
                    CostoDolares = Math.Round(b.precio * 0.40m, 2), // Costo estimado de reactivos ~40%
                    Observaciones = b.obs,
                    TiempoRespuesta = b.codigo == "SALV-00001" ? "24 Horas" : "Mismo Día",
                    EsTercerizado = false,
                    Activo = true
                };

                int orden = 1;
                foreach (var p in b.parametros)
                {
                    nuevo.Parametros.Add(new ExamenParametro
                    {
                        ExamenParametroId = Guid.NewGuid(),
                        ExamenId = nuevo.ExamenId,
                        NombreParametro = p.param,
                        Unidades = p.uni,
                        RangoReferenciaDefecto = p.refVal,
                        Orden = orden++
                    });
                }

                _context.ExamenesCatalogo.Add(nuevo);
                creados++;
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Catálogo de rutina SALV procesado exitosamente: {creados} exámenes creados, {omitidos} ya existían.",
                creados,
                omitidos
            });
        }

        // =========================================================================================
        // SINCRONIZACIÓN EXCEL DE PRUEBAS ESPECIALES TERCERIZADAS
        // =========================================================================================

        // POST: api/Examenes/previsualizar-excel
        [HttpPost("previsualizar-excel")]
        public async Task<IActionResult> PrevisualizarExcelPrecios(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Por favor, seleccione un archivo Excel válido." });

            var tenantId = ObtenerTenantIdDelToken();
            var cambiosPrevistos = new List<object>();

            using (var stream = new MemoryStream())
            {
                await archivo.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var col1 = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(col1) || col1.ToUpper() == "NOMBRE" || col1.ToUpper() == "CODIGO") continue;

                        string codigo;
                        string descripcion;
                        string observaciones = "";
                        decimal precioDolares = 0;
                        decimal costoDolares = 0;

                        // Detectar si columna 2 es precio directamente (como en precios_lab.xlsx) o descripcion
                        if (decimal.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out decimal precioDetectado))
                        {
                            descripcion = col1;
                            codigo = $"ESP-{row:D4}";
                            precioDolares = precioDetectado;
                            observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                        }
                        else
                        {
                            codigo = col1;
                            descripcion = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "Examen sin descripción";
                            observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                            decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out costoDolares);
                            decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out precioDolares);
                        }

                        // Proteger exámenes de rutina SALV
                        if (codigo.StartsWith("SALV-", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Omitir para no sobreescribir rutina
                        }

                        var examenExistente = await _context.ExamenesCatalogo
                            .FirstOrDefaultAsync(e => e.TenantId == tenantId && (e.CodigoProveedor == codigo || e.NombreExamen == descripcion));

                        if (examenExistente != null)
                        {
                            bool precioCambio = examenExistente.PrecioDolares != precioDolares;
                            cambiosPrevistos.Add(new
                            {
                                codigo = examenExistente.CodigoProveedor,
                                descripcion = examenExistente.NombreExamen,
                                precioAnterior = examenExistente.PrecioDolares,
                                precioNuevo = precioDolares,
                                estado = precioCambio ? "Tarifa Modificada" : "Sin Cambios"
                            });
                        }
                        else
                        {
                            cambiosPrevistos.Add(new
                            {
                                codigo,
                                descripcion,
                                precioAnterior = 0.0m,
                                precioNuevo = precioDolares,
                                estado = "Nuevo Examen Especial"
                            });
                        }
                    }
                }
            }

            return Ok(new 
            { 
                message = "Vista previa de pruebas especiales generada exitosamente.", 
                totalRegistros = cambiosPrevistos.Count, 
                cambios = cambiosPrevistos 
            });
        }

        // POST: api/Examenes/sincronizar-precios
        [HttpPost("sincronizar-precios")]
        public async Task<IActionResult> SincronizarPreciosExcel(IFormFile archivo, [FromQuery] string? laboratorioDestino)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Seleccione un archivo Excel válido." });

            var tenantId = ObtenerTenantIdDelToken();
            int actualizados = 0;
            int insertados = 0;
            string labDestino = string.IsNullOrWhiteSpace(laboratorioDestino) ? "Laboratorio de Referencia Externo" : laboratorioDestino.Trim();

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

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var col1 = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(col1) || col1.ToUpper() == "NOMBRE" || col1.ToUpper() == "CODIGO") continue;

                            string codigo;
                            string descripcion;
                            string observaciones = "";
                            decimal precioDolares = 0;
                            decimal costoDolares = 0;

                            if (decimal.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out decimal precioDetectado))
                            {
                                descripcion = col1;
                                codigo = $"ESP-{row:D4}";
                                precioDolares = precioDetectado;
                                observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                            }
                            else
                            {
                                codigo = col1;
                                descripcion = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "Examen sin descripción";
                                observaciones = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                                decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out costoDolares);
                                decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out precioDolares);
                            }

                            // Protección estricta: NO tocar exámenes de rutina SALV
                            if (codigo.StartsWith("SALV-", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var examen = await _context.ExamenesCatalogo
                                .FirstOrDefaultAsync(e => e.TenantId == tenantId && (e.CodigoProveedor == codigo || e.NombreExamen == descripcion));

                            if (examen != null)
                            {
                                if (examen.EsTercerizado) // Solo actualizar si es tercerizado
                                {
                                    examen.PrecioDolares = precioDolares;
                                    if (costoDolares > 0) examen.CostoDolares = costoDolares;
                                    if (!string.IsNullOrWhiteSpace(observaciones)) examen.Observaciones = observaciones;
                                    actualizados++;
                                }
                            }
                            else
                            {
                                var nuevoExamen = new ExamenCatalogo
                                {
                                    ExamenId = Guid.NewGuid(),
                                    TenantId = tenantId,
                                    CodigoProveedor = codigo,
                                    NombreExamen = descripcion.ToUpper(),
                                    Observaciones = observaciones,
                                    Categoria = "Pruebas Especiales",
                                    CostoDolares = costoDolares,
                                    PrecioDolares = precioDolares,
                                    TiempoRespuesta = "72 Horas",
                                    EsTercerizado = true,
                                    LaboratorioDestino = labDestino,
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
                    message = "Sincronización de tarifas de pruebas especiales completada.", 
                    registrosActualizados = actualizados, 
                    registrosNuevos = insertados 
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error procesando el archivo Excel: {ex.Message}");
            }
        }
    }
}