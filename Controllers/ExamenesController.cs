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
using Laboratorio.Api.Security;
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
            var rolOperador = ObtenerRolDelToken();
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

            // Regla de Permisos: Caja y Recepción pueden dar de alta exámenes para facturar,
            // pero los analitos y valores de referencia clínicos son exclusividad de Bioanálisis.
            bool puedeGestionarParametros = rolOperador != Roles.Cajero && rolOperador != Roles.Secretaria;

            if (puedeGestionarParametros && dto.Parametros != null && dto.Parametros.Any())
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
            var rolOperador = ObtenerRolDelToken();

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
            examen.TiempoRespuesta = string.IsNullOrWhiteSpace(dto.TiempoRespuesta) ? "24 Horas" : dto.TiempoRespuesta.Trim();
            examen.EsTercerizado = dto.EsTercerizado;
            examen.LaboratorioDestino = dto.EsTercerizado ? dto.LaboratorioDestino?.Trim() : null;
            examen.Activo = dto.Activo;

            // Regla de Precios: Bioanálisis no puede alterar las tarifas fijadas por Administración/Caja (Solo Lectura)
            if (rolOperador != Roles.Bioanalista)
            {
                examen.CostoDolares = dto.CostoDolares;
                examen.PrecioDolares = dto.PrecioDolares;
            }

            // Regla de Parámetros: Caja y Secretaría no pueden modificar componentes clínicos (Exclusivo Bioanálisis/Admin)
            bool puedeGestionarParametros = rolOperador != Roles.Cajero && rolOperador != Roles.Secretaria;
            if (puedeGestionarParametros)
            {
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
        // SEED PROMOCIONAL: COMBOS Y PERFILES OCTUBRE ROSA (SOCIEDAD ANTICANCEROSA)
        // =========================================================================================
        [HttpPost("seed-promociones-octubre")]
        public async Task<IActionResult> SeedPromocionesOctubre()
        {
            var tenantId = ObtenerTenantIdDelToken();

            var promociones = new List<(string codigo, string nombre, string cat, decimal precio, string obs, List<(string param, string uni, string refVal)> parametros)>
            {
                ("ROSA-00001", "COMBO HIV + VDRL (OCTUBRE ROSA)", "Promociones Octubre Rosa", 10.00m, "Muestra de suero en ayunas", new List<(string, string, string)>
                {
                    ("HIV 1 y 2 (Prueba Rápida Inmunocromatográfica)", "-", "NO REACTIVO"),
                    ("VDRL (Serología RPR)", "-", "NO REACTIVO")
                }),

                ("ROSA-00002", "HE4 - MARCADOR TUMORAL OVÁRICO (OCTUBRE ROSA)", "Promociones Octubre Rosa", 18.00m, "Suero libre de hemólisis", new List<(string, string, string)>
                {
                    ("Antígeno HE4 (EIA/Quimioluminiscencia)", "pmol/L", "Pre-menopausia: < 70 | Post-menopausia: < 140")
                }),

                ("ROSA-00003", "COMBO MARCADORES TUMORALES (CEA + CA125 + CA19-9 + AFP)", "Promociones Octubre Rosa", 28.00m, "Suero libre de hemólisis y lipemia", new List<(string, string, string)>
                {
                    ("Antígeno Carcinoembrionario (CEA)", "ng/mL", "< 5.0 (No fumador: < 3.0)"),
                    ("Antígeno CA-125 (Ovario)", "U/mL", "< 35.0"),
                    ("Antígeno CA 19-9 (Gastrointestinal / Páncreas)", "U/mL", "< 37.0"),
                    ("Alfafetoproteína (AFP)", "ng/mL", "< 7.0")
                }),

                ("ROSA-00004", "PERFIL 20 ADULTO MAYOR (OCTUBRE ROSA)", "Promociones Octubre Rosa", 20.00m, "Ayunas estricto de 12 horas", new List<(string, string, string)>
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
                    ("Examen General de Orina", "-", "Límpido / Sin sedimentos patológicos")
                }),

                ("ROSA-00005", "PERFIL PARA PAREJAS (5 EN 1 - OCTUBRE ROSA)", "Promociones Octubre Rosa", 50.00m, "Muestra de suero en ayunas y primera orina", new List<(string, string, string)>
                {
                    ("HIV 1 y 2", "-", "NO REACTIVO"),
                    ("VDRL", "-", "NO REACTIVO"),
                    ("Serología Hepatitis B (HBsAg)", "-", "NO REACTIVO"),
                    ("Serología Hepatitis C (Anti-HCV)", "-", "NO REACTIVO"),
                    ("Hematología Completa", "-", "Parámetros normales"),
                    ("Examen de Orina", "-", "Límpido / Normal")
                })
            };

            int creados = 0;
            int omitidos = 0;
            foreach (var item in promociones)
            {
                var existe = await _context.ExamenesCatalogo
                    .AnyAsync(e => e.TenantId == tenantId && e.CodigoProveedor == item.codigo);

                if (!existe)
                {
                    var nuevoExamen = new ExamenCatalogo
                    {
                        ExamenId = Guid.NewGuid(),
                        TenantId = tenantId,
                        CodigoProveedor = item.codigo,
                        NombreExamen = item.nombre,
                        Categoria = item.cat,
                        PrecioDolares = item.precio,
                        CostoDolares = item.precio * 0.65m,
                        Observaciones = item.obs,
                        TiempoRespuesta = "24 Horas",
                        EsTercerizado = false,
                        Activo = true
                    };

                    int ord = 1;
                    foreach (var p in item.parametros)
                    {
                        nuevoExamen.Parametros.Add(new ExamenParametro
                        {
                            ExamenParametroId = Guid.NewGuid(),
                            ExamenId = nuevoExamen.ExamenId,
                            NombreParametro = p.param,
                            Unidades = p.uni,
                            RangoReferenciaDefecto = p.refVal,
                            TecnicaDefecto = "Estándar",
                            Orden = ord++
                        });
                    }

                    _context.ExamenesCatalogo.Add(nuevoExamen);
                    creados++;
                }
                else
                {
                    omitidos++;
                }
            }

            if (creados > 0)
                await _context.SaveChangesAsync();

            return Ok(new 
            { 
                mensaje = $"Catálogo de Octubre Rosa procesado: {creados} combos creados, {omitidos} ya existían.",
                creados,
                omitidos
            });
        }

        // =========================================================================================
        // SINCRONIZACIÓN EXCEL DE PRUEBAS ESPECIALES TERCERIZADAS
        // =========================================================================================

        private class FilaExcelExamen
        {
            public string Codigo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Observaciones { get; set; } = "";
            public decimal Precio { get; set; }
            public decimal Costo { get; set; }
        }

        private static string ClasificarCategoria(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Pruebas Especiales";
            var n = nombre.ToUpper();
            if (n.Contains("PERFIL")) return "Perfiles Especiales";
            if (n.Contains("HEMO") || n.Contains("COAGUL") || n.Contains("PLAQUET") || n.Contains("FIBRIN")) return "Hematología";
            if (n.Contains("GLIC") || n.Contains("GLUC") || n.Contains("UREA") || n.Contains("CREAT") || n.Contains("COLEST") || n.Contains("TRIGLIC") || n.Contains("ACIDO") || n.Contains("BILIRRUB")) return "Química Sanguínea";
            if (n.Contains("HIV") || n.Contains("VDRL") || n.Contains("HEPATIT") || n.Contains("COVID") || n.Contains("DENGUE") || n.Contains("ANTIGENO") || n.Contains("ANTICUERPO") || n.Contains("INMUNO") || n.Contains("SEROLOG")) return "Inmunología y Serología";
            if (n.Contains("TSH") || n.Contains("T3") || n.Contains("T4") || n.Contains("HORMON") || n.Contains("INSULIN") || n.Contains("PROLACT") || n.Contains("ESTRADIOL") || n.Contains("TESTOST") || n.Contains("CORTISOL")) return "Endocrinología y Hormonas";
            if (n.Contains("PSA") || n.Contains("CEA") || n.Contains("CA-125") || n.Contains("CA 125") || n.Contains("CA-19") || n.Contains("CA 19") || n.Contains("CA-15") || n.Contains("CA 15") || n.Contains("ALFAFETO")) return "Marcadores Tumorales";
            if (n.Contains("ORINA") || n.Contains("UROCULT")) return "Uroanálisis";
            if (n.Contains("HECES") || n.Contains("COPRO")) return "Coproanálisis";
            return "Pruebas Especiales";
        }

        private static List<FilaExcelExamen> ExtraerFilasExcel(ExcelWorksheet worksheet)
        {
            var resultado = new List<FilaExcelExamen>();
            if (worksheet.Dimension == null) return resultado;

            int totalRows = worksheet.Dimension.End.Row;
            int totalCols = worksheet.Dimension.End.Column;

            int colNombre = -1;
            int colPrecio = -1;
            int colCodigo = -1;
            int colObs = -1;
            int colCosto = -1;
            int startRow = 1;

            // 1. Escanear cabeceras en las primeras 10 filas
            for (int r = 1; r <= Math.Min(10, totalRows); r++)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var val = worksheet.Cells[r, c].Value?.ToString()?.Trim().ToUpper() ?? "";
                    if (val == "NOMBRE" || val == "DESCRIPCION" || val == "EXAMEN" || val == "PRUEBA")
                        colNombre = c;
                    else if (val == "PRECIO" || val == "TARIFA" || val == "MONTO" || val == "PVP")
                        colPrecio = c;
                    else if (val == "CODIGO" || val == "COD")
                        colCodigo = c;
                    else if (val.StartsWith("OBSER"))
                        colObs = c;
                    else if (val == "COSTO")
                        colCosto = c;
                }

                if (colNombre != -1 && colPrecio != -1)
                {
                    startRow = r + 1;
                    break;
                }
            }

            // 2. Si no encontró cabecera explícita, fallback inteligente (en precios_lab.xlsx es B=2, C=3, D=4)
            if (colNombre == -1 || colPrecio == -1)
            {
                var b3 = worksheet.Cells[3, 2].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(b3))
                {
                    colNombre = 2;
                    colPrecio = 3;
                    colObs = 4;
                    startRow = 3;
                }
                else
                {
                    colNombre = 1;
                    colPrecio = 2;
                    colObs = 3;
                    startRow = 2;
                }
            }

            int seq = 1;
            for (int r = startRow; r <= totalRows; r++)
            {
                var nombre = worksheet.Cells[r, colNombre].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(nombre) || nombre.ToUpper() == "NOMBRE" || nombre.ToUpper() == "EXAMEN")
                    continue;

                decimal precio = 0;
                var precioStr = worksheet.Cells[r, colPrecio].Value?.ToString()?.Trim()?.Replace(",", ".");
                if (!string.IsNullOrEmpty(precioStr))
                {
                    decimal.TryParse(precioStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out precio);
                }

                string codigo = "";
                if (colCodigo != -1)
                {
                    codigo = worksheet.Cells[r, colCodigo].Value?.ToString()?.Trim() ?? "";
                }
                if (string.IsNullOrEmpty(codigo))
                {
                    codigo = $"ESP-{seq:D4}";
                }

                string obs = colObs != -1 ? worksheet.Cells[r, colObs].Value?.ToString()?.Trim() ?? "" : "";
                decimal costo = 0;
                if (colCosto != -1)
                {
                    var costoStr = worksheet.Cells[r, colCosto].Value?.ToString()?.Trim()?.Replace(",", ".");
                    if (!string.IsNullOrEmpty(costoStr))
                    {
                        decimal.TryParse(costoStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out costo);
                    }
                }

                resultado.Add(new FilaExcelExamen
                {
                    Codigo = codigo,
                    Descripcion = nombre.ToUpper(),
                    Observaciones = obs,
                    Precio = precio,
                    Costo = costo > 0 ? costo : Math.Round(precio * 0.70m, 2)
                });

                seq++;
            }

            return resultado;
        }

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
                    var filas = ExtraerFilasExcel(worksheet);

                    var examenesExistentes = await _context.ExamenesCatalogo
                        .AsNoTracking()
                        .Where(e => e.TenantId == tenantId)
                        .ToListAsync();

                    var dictPorNombre = examenesExistentes
                        .GroupBy(e => e.NombreExamen.Trim().ToUpper())
                        .ToDictionary(g => g.Key, g => g.First());

                    var dictPorCodigo = examenesExistentes
                        .GroupBy(e => e.CodigoProveedor.Trim().ToUpper())
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var fila in filas)
                    {
                        if (fila.Codigo.StartsWith("SALV-", StringComparison.OrdinalIgnoreCase))
                            continue;

                        dictPorNombre.TryGetValue(fila.Descripcion.Trim().ToUpper(), out var examenPorNombre);
                        dictPorCodigo.TryGetValue(fila.Codigo.Trim().ToUpper(), out var examenPorCodigo);
                        var examenExistente = examenPorNombre ?? examenPorCodigo;

                        if (examenExistente != null)
                        {
                            if (!examenExistente.EsTercerizado)
                            {
                                // Omitir exámenes de rutina propios
                                continue;
                            }

                            bool precioCambio = examenExistente.PrecioDolares != fila.Precio;
                            cambiosPrevistos.Add(new
                            {
                                codigo = examenExistente.CodigoProveedor,
                                descripcion = examenExistente.NombreExamen,
                                precioAnterior = examenExistente.PrecioDolares,
                                precioNuevo = fila.Precio,
                                estado = precioCambio ? "Tarifa Modificada" : "Sin Cambios"
                            });
                        }
                        else
                        {
                            cambiosPrevistos.Add(new
                            {
                                codigo = fila.Codigo,
                                descripcion = fila.Descripcion,
                                precioAnterior = 0.0m,
                                precioNuevo = fila.Precio,
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
                        var filas = ExtraerFilasExcel(worksheet);

                        var examenesExistentes = await _context.ExamenesCatalogo
                            .Where(e => e.TenantId == tenantId)
                            .ToListAsync();

                        var dictPorNombre = examenesExistentes
                            .GroupBy(e => e.NombreExamen.Trim().ToUpper())
                            .ToDictionary(g => g.Key, g => g.First());

                        var dictPorCodigo = examenesExistentes
                            .GroupBy(e => e.CodigoProveedor.Trim().ToUpper())
                            .ToDictionary(g => g.Key, g => g.First());

                        foreach (var fila in filas)
                        {
                            if (fila.Codigo.StartsWith("SALV-", StringComparison.OrdinalIgnoreCase))
                                continue;

                            dictPorNombre.TryGetValue(fila.Descripcion.Trim().ToUpper(), out var examenPorNombre);
                            dictPorCodigo.TryGetValue(fila.Codigo.Trim().ToUpper(), out var examenPorCodigo);
                            var examen = examenPorNombre ?? examenPorCodigo;

                            if (examen != null)
                            {
                                if (examen.EsTercerizado)
                                {
                                    examen.PrecioDolares = fila.Precio;
                                    if (fila.Costo > 0) examen.CostoDolares = fila.Costo;
                                    if (!string.IsNullOrWhiteSpace(fila.Observaciones)) examen.Observaciones = fila.Observaciones;
                                    actualizados++;
                                }
                            }
                            else
                            {
                                var nuevoExamen = new ExamenCatalogo
                                {
                                    ExamenId = Guid.NewGuid(),
                                    TenantId = tenantId,
                                    CodigoProveedor = fila.Codigo,
                                    NombreExamen = fila.Descripcion,
                                    Observaciones = fila.Observaciones,
                                    Categoria = ClasificarCategoria(fila.Descripcion),
                                    CostoDolares = fila.Costo,
                                    PrecioDolares = fila.Precio,
                                    TiempoRespuesta = "48 a 72 Horas",
                                    EsTercerizado = true,
                                    LaboratorioDestino = labDestino,
                                    Activo = true
                                };
                                _context.ExamenesCatalogo.Add(nuevoExamen);
                                dictPorNombre[nuevoExamen.NombreExamen] = nuevoExamen;
                                dictPorCodigo[nuevoExamen.CodigoProveedor] = nuevoExamen;
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