using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using Laboratorio.Api.Dtos.Ordenes;
using Laboratorio.Api.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BCrypt.Net;

namespace Laboratorio.Api.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Administrador},{Roles.Bioanalista},{Roles.Secretaria},{Roles.Cajero}")]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenesController : BaseController
    {
        private readonly AppDbContext _context;

        public OrdenesController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. LISTADO DE ÓRDENES CON FILTROS Y RESUMEN FINANCIERO
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrdenResumenDto>>> GetOrdenes(
            [FromQuery] string? busqueda = null,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? fecha = null,
            [FromQuery] int limite = 100)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var query = _context.OrdenesLaboratorio
                .Where(o => o.TenantId == tenantId)
                .Include(o => o.Paciente)
                .Include(o => o.Factura!)
                    .ThenInclude(f => f.Pagos)
                .Include(o => o.Resultados!)
                    .ThenInclude(r => r.Examen)
                .AsQueryable();

            // Filtro por Estado
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                query = query.Where(o => o.Estado.ToLower() == estado.ToLower());
            }

            // Filtro por Fecha (opcional)
            if (fecha.HasValue)
            {
                var fechaFiltro = DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc);
                query = query.Where(o => o.FechaOrden.Date == fechaFiltro.Date);
            }

            // Filtro por Búsqueda (Paciente, Cédula, Correlativo o Motivo)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                query = query.Where(o =>
                    (o.Paciente != null && o.Paciente.NombreCompleto.ToLower().Contains(term)) ||
                    (o.Paciente != null && o.Paciente.Cedula.ToLower().Contains(term)) ||
                    o.CorrelativoDiario.ToString().Contains(term) ||
                    (o.MotivoExamen != null && o.MotivoExamen.ToLower().Contains(term)));
            }

            var ordenes = await query
                .OrderByDescending(o => o.FechaOrden)
                .Take(Math.Min(limite, 500))
                .ToListAsync();

            var hoy = DateTime.UtcNow;

            var resultadoDtos = ordenes.Select(o =>
            {
                var factura = o.Factura;
                decimal totalNetoUsd = factura?.TotalNeto ?? 0;
                decimal tasaCambio = factura?.TasaCambio ?? 0;
                decimal totalNetoVes = totalNetoUsd * tasaCambio;

                decimal totalPagadoUsd = 0;
                if (factura != null && factura.Pagos != null)
                {
                    totalPagadoUsd = factura.Pagos.Sum(p =>
                        p.Moneda.ToUpper() == "VES"
                            ? (tasaCambio > 0 ? (p.Monto / tasaCambio) : 0)
                            : p.Monto);
                }

                decimal saldoPendienteUsd = Math.Max(0, totalNetoUsd - totalPagadoUsd);

                // Calcular edad del paciente
                int edad = 0;
                if (o.Paciente != null)
                {
                    edad = hoy.Year - o.Paciente.FechaNacimiento.Year;
                    if (o.Paciente.FechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
                }

                var examenesList = o.Resultados?
                    .Where(r => r.Examen != null)
                    .Select(r => r.Examen!.NombreExamen)
                    .ToList() ?? new List<string>();

                return new OrdenResumenDto
                {
                    OrdenId = o.OrdenId,
                    CorrelativoDiario = o.CorrelativoDiario,
                    FechaOrden = o.FechaOrden,
                    FechaValidacionFinal = o.FechaValidacionFinal,
                    Estado = o.Estado,
                    MotivoExamen = o.MotivoExamen,
                    ObservacionBioanalista = o.ObservacionBioanalista,

                    PacienteId = o.PacienteId,
                    PacienteNombre = o.Paciente?.NombreCompleto ?? "Desconocido",
                    PacienteCedula = o.Paciente?.Cedula ?? "S/C",
                    PacienteSexo = o.Paciente?.Sexo ?? "N/A",
                    PacienteEdad = edad,
                    PacienteTelefono = o.Paciente?.TelefonoPrincipal,

                    TotalExamenes = examenesList.Count,
                    ExamenesNombres = examenesList,

                    FacturaId = factura?.FacturaId,
                    TotalNetoUSD = totalNetoUsd,
                    TotalNetoVES = totalNetoVes,
                    TotalPagadoUSD = totalPagadoUsd,
                    SaldoPendienteUSD = saldoPendienteUsd,
                    TasaCambio = tasaCambio,
                    EstadoFactura = factura?.Estado ?? "Sin Factura"
                };
            }).ToList();

            return Ok(resultadoDtos);
        }

        // ==========================================
        // 2. MÉTRICAS DIARIAS DEL TABLERO CLÍNICO Y CAJA
        // ==========================================
        [HttpGet("metricas-hoy")]
        public async Task<ActionResult<MetricasOrdenesDto>> GetMetricasHoy()
        {
            var tenantId = ObtenerTenantIdDelToken();
            var hoy = DateTime.UtcNow.Date;

            var ordenesHoy = await _context.OrdenesLaboratorio
                .Where(o => o.TenantId == tenantId && o.FechaOrden.Date == hoy)
                .ToListAsync();

            var facturasHoy = await _context.Facturas
                .Where(f => f.TenantId == tenantId && f.FechaEmision.Date == hoy)
                .ToListAsync();

            var pagosHoy = await _context.Pagos
                .Where(p => p.TenantId == tenantId && p.FechaRegistro.Date == hoy)
                .ToListAsync();

            var totalFacturadoUsd = facturasHoy.Sum(f => f.TotalNeto);
            var totalCobradoUsd = pagosHoy.Where(p => p.Moneda.ToUpper() == "USD").Sum(p => p.Monto);
            var totalCobradoVes = pagosHoy.Where(p => p.Moneda.ToUpper() == "VES").Sum(p => p.Monto);

            var metricas = new MetricasOrdenesDto
            {
                TotalOrdenesHoy = ordenesHoy.Count,
                RegistradasHoy = ordenesHoy.Count(o => o.Estado == "Registrada"),
                ProcesadasHoy = ordenesHoy.Count(o => o.Estado == "Procesada"),
                ValidadasHoy = ordenesHoy.Count(o => o.Estado == "Validada"),
                TotalFacturadoUSDHoy = totalFacturadoUsd,
                TotalCobradoUSDHoy = totalCobradoUsd,
                TotalCobradoVESHoy = totalCobradoVes
            };

            return Ok(metricas);
        }

        // ==========================================
        // 3. CREAR ORDEN INTEGRADA CON FACTURACIÓN Y PAGOS (PASO 5)
        // ==========================================
        [HttpPost("crear-con-factura")]
        public async Task<IActionResult> CrearOrdenConFactura([FromBody] CrearOrdenConFacturaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = ObtenerTenantIdDelToken();
            var usuarioId = ObtenerUsuarioGuidDelToken();

            // 1. Validar que el paciente exista en el tenant
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.PacienteId == dto.PacienteId && p.TenantId == tenantId);

            if (paciente == null)
                return NotFound(new { mensaje = "El paciente seleccionado no existe o no pertenece a esta clínica." });

            // 2. Validar que los exámenes existan en el catálogo y obtener precios
            var examenes = await _context.ExamenesCatalogo
                .Include(e => e.Parametros)
                .Where(e => dto.ExamenesIds.Contains(e.ExamenId) && e.TenantId == tenantId && e.Activo)
                .ToListAsync();

            if (!examenes.Any())
                return BadRequest(new { mensaje = "Debe seleccionar al menos un examen válido del catálogo." });

            // 3. Calcular montos
            decimal subTotalUSD = examenes.Sum(e => e.PrecioDolares);
            decimal montoDescuento = Math.Max(0, dto.MontoDescuento);

            Usuario? adminAutorizador = null;

            // 4. Si hay descuento solicitado, validar PIN de Supervisor / Administrador
            if (montoDescuento > 0)
            {
                if (montoDescuento > subTotalUSD)
                    return BadRequest(new { mensaje = "El monto del descuento no puede ser superior al subtotal de la orden." });

                if (string.IsNullOrWhiteSpace(dto.PinAdministrador))
                    return BadRequest(new { mensaje = "Se requiere el PIN de 4 dígitos de un Administrador para autorizar el descuento." });

                var administradores = await _context.Usuarios
                    .Where(u => u.TenantId == tenantId && (u.Rol == Roles.Administrador || u.Rol == Roles.SuperAdmin) && !string.IsNullOrEmpty(u.PinAutorizacion))
                    .ToListAsync();

                foreach (var admin in administradores)
                {
                    if (BCrypt.Net.BCrypt.Verify(dto.PinAdministrador, admin.PinAutorizacion))
                    {
                        adminAutorizador = admin;
                        break;
                    }
                }

                if (adminAutorizador == null)
                {
                    return BadRequest(new { mensaje = "PIN de autorización inválido o usuario sin privilegios de administrador para aprobar descuentos." });
                }
            }

            decimal totalNetoUSD = Math.Max(0, subTotalUSD - montoDescuento);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var hoy = DateTime.UtcNow.Date;

                // 5. Correlativo diario secuencial (#001, #002...)
                var ultimoCorrelativo = await _context.OrdenesLaboratorio
                    .Where(o => o.TenantId == tenantId && o.FechaOrden.Date == hoy)
                    .MaxAsync(o => (int?)o.CorrelativoDiario) ?? 0;

                var nuevoCorrelativo = ultimoCorrelativo + 1;

                // 6. Crear Orden de Laboratorio
                var nuevaOrden = new OrdenLaboratorio
                {
                    OrdenId = Guid.NewGuid(),
                    TenantId = tenantId,
                    PacienteId = paciente.PacienteId,
                    CorrelativoDiario = nuevoCorrelativo,
                    Estado = "Registrada",
                    MotivoExamen = dto.MotivoExamen,
                    FechaOrden = DateTime.UtcNow
                };

                _context.OrdenesLaboratorio.Add(nuevaOrden);

                // 7. Crear Resultados Detalle para cada examen seleccionado
                var resultados = new List<ResultadoDetalle>();
                foreach (var ex in examenes)
                {
                    var primerParam = ex.Parametros.OrderBy(p => p.Orden).FirstOrDefault();

                    resultados.Add(new ResultadoDetalle
                    {
                        ResultadoId = Guid.NewGuid(),
                        TenantId = tenantId,
                        OrdenId = nuevaOrden.OrdenId,
                        ExamenId = ex.ExamenId,
                        EstadoTercerizado = ex.EsTercerizado ? "Tercerizado" : "Interno",
                        RangoReferenciaAplicado = primerParam?.RangoReferenciaDefecto ?? string.Empty,
                        TecnicaAplicada = primerParam?.TecnicaDefecto ?? string.Empty,
                        MuestraEntregada = true,
                        FechaCarga = DateTime.UtcNow
                    });
                }

                _context.ResultadosDetalle.AddRange(resultados);

                // 8. Crear Factura Serie B
                var nuevaFactura = new Factura
                {
                    FacturaId = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrdenLaboratorioId = nuevaOrden.OrdenId,
                    SubTotal = subTotalUSD,
                    MontoDescuento = montoDescuento,
                    MotivoDescuento = dto.MotivoDescuento,
                    PorcentajeIva = 0, // Servicios de salud exentos según legislación venezolana
                    MontoIva = 0,
                    TotalNeto = totalNetoUSD,
                    TasaCambio = dto.TasaCambio > 0 ? dto.TasaCambio : 1,
                    Estado = "Pendiente",
                    FechaEmision = DateTime.UtcNow,
                    DescuentoAplicadoPor = montoDescuento > 0 ? usuarioId : null,
                    DescuentoAutorizadoPor = adminAutorizador?.UsuarioId,
                    FechaDescuento = montoDescuento > 0 ? DateTime.UtcNow : null
                };

                _context.Facturas.Add(nuevaFactura);

                // 9. Procesar Pagos Iniciales si se ingresaron
                decimal totalPagadoEquivUsd = 0;
                if (dto.Pagos != null && dto.Pagos.Any())
                {
                    foreach (var p in dto.Pagos.Where(p => p.Monto > 0))
                    {
                        var nuevoPago = new Pago
                        {
                            PagoId = Guid.NewGuid(),
                            TenantId = tenantId,
                            FacturaId = nuevaFactura.FacturaId,
                            UsuarioId = usuarioId,
                            Monto = p.Monto,
                            Moneda = p.Moneda.ToUpper(),
                            MetodoPago = p.MetodoPago,
                            Referencia = p.Referencia,
                            FechaRegistro = DateTime.UtcNow
                        };

                        _context.Pagos.Add(nuevoPago);

                        if (p.Moneda.ToUpper() == "VES")
                        {
                            totalPagadoEquivUsd += nuevaFactura.TasaCambio > 0 ? (p.Monto / nuevaFactura.TasaCambio) : 0;
                        }
                        else
                        {
                            totalPagadoEquivUsd += p.Monto;
                        }
                    }
                }

                // 10. Actualizar Estado de Factura según pago
                if (totalPagadoEquivUsd >= (totalNetoUSD - 0.01m))
                {
                    nuevaFactura.Estado = "Pagada";
                }
                else if (totalPagadoEquivUsd > 0.01m)
                {
                    nuevaFactura.Estado = "Abonada";
                }
                else
                {
                    nuevaFactura.Estado = "Pendiente";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Orden clínica y factura generadas exitosamente",
                    ordenId = nuevaOrden.OrdenId,
                    correlativo = nuevaOrden.CorrelativoDiario,
                    numeroOrden = $"#{nuevaOrden.CorrelativoDiario:D3}-{nuevaOrden.FechaOrden:yyyy}",
                    facturaId = nuevaFactura.FacturaId,
                    totalNetoUSD = nuevaFactura.TotalNeto,
                    totalNetoVES = nuevaFactura.TotalNeto * nuevaFactura.TasaCambio,
                    estadoFactura = nuevaFactura.Estado,
                    saldoPendienteUSD = Math.Max(0, nuevaFactura.TotalNeto - totalPagadoEquivUsd)
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = $"Error interno al emitir la orden: {ex.Message}" });
            }
        }

        // ==========================================
        // 4. DETALLE COMPLETO DE ORDEN (BIOANALISTA, REPORTE Y TICKET)
        // ==========================================
        [HttpGet("{id}/detalle-completo")]
        public async Task<ActionResult<OrdenDetalleCompletoDto>> GetOrdenDetalleCompleto(Guid id)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var orden = await _context.OrdenesLaboratorio
                .Where(o => o.OrdenId == id && o.TenantId == tenantId)
                .Include(o => o.Tenant)
                .Include(o => o.Paciente)
                .Include(o => o.Factura!)
                    .ThenInclude(f => f.Pagos)
                .Include(o => o.Resultados!)
                    .ThenInclude(r => r.Examen!)
                        .ThenInclude(e => e.Parametros)
                .FirstOrDefaultAsync();

            if (orden == null)
                return NotFound(new { mensaje = "La orden clínica solicitada no existe." });

            var hoy = DateTime.UtcNow;
            int edad = 0;
            if (orden.Paciente != null)
            {
                edad = hoy.Year - orden.Paciente.FechaNacimiento.Year;
                if (orden.Paciente.FechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            }

            // Mapeo de Factura y Pagos
            FacturaDetalleDto? facturaDto = null;
            if (orden.Factura != null)
            {
                var f = orden.Factura;
                var pagosList = f.Pagos?.Select(p => new PagoItemDto
                {
                    PagoId = p.PagoId,
                    Monto = p.Monto,
                    Moneda = p.Moneda,
                    MetodoPago = p.MetodoPago,
                    Referencia = p.Referencia,
                    FechaRegistro = p.FechaRegistro
                }).ToList() ?? new List<PagoItemDto>();

                decimal totalUsd = pagosList.Where(p => p.Moneda == "USD").Sum(p => p.Monto);
                decimal totalVes = pagosList.Where(p => p.Moneda == "VES").Sum(p => p.Monto);
                decimal totalEquivUsd = totalUsd + (f.TasaCambio > 0 ? (totalVes / f.TasaCambio) : 0);

                facturaDto = new FacturaDetalleDto
                {
                    FacturaId = f.FacturaId,
                    SubTotal = f.SubTotal,
                    MontoDescuento = f.MontoDescuento,
                    MotivoDescuento = f.MotivoDescuento,
                    PorcentajeIva = f.PorcentajeIva,
                    MontoIva = f.MontoIva,
                    TotalNeto = f.TotalNeto,
                    TasaCambio = f.TasaCambio,
                    Estado = f.Estado,
                    FechaEmision = f.FechaEmision,
                    TotalPagadoUSD = totalEquivUsd,
                    TotalPagadoVES = totalVes,
                    SaldoPendienteUSD = Math.Max(0, f.TotalNeto - totalEquivUsd),
                    Pagos = pagosList
                };
            }

            // Mapeo de Exámenes y Parámetros
            var examenesDto = new List<ExamenOrdenDetalleDto>();
            Guid? bioanalistaId = null;

            if (orden.Resultados != null)
            {
                foreach (var r in orden.Resultados)
                {
                    if (r.UuidBioanalista.HasValue)
                        bioanalistaId = r.UuidBioanalista.Value;

                    var paramsDto = r.Examen?.Parametros?
                        .OrderBy(p => p.Orden)
                        .Select(p => new ExamenParametroDetalleDto
                        {
                            ParametroId = p.ExamenParametroId,
                            NombreParametro = p.NombreParametro,
                            Unidades = p.Unidades,
                            RangoReferenciaDefecto = p.RangoReferenciaDefecto,
                            TecnicaDefecto = p.TecnicaDefecto,
                            Orden = p.Orden
                        }).ToList() ?? new List<ExamenParametroDetalleDto>();

                    examenesDto.Add(new ExamenOrdenDetalleDto
                    {
                        ResultadoId = r.ResultadoId,
                        ExamenId = r.ExamenId,
                        CodigoProveedor = r.Examen?.CodigoProveedor ?? string.Empty,
                        NombreExamen = r.Examen?.NombreExamen ?? "Examen Clínico",
                        Categoria = r.Examen?.Categoria ?? "General",
                        PrecioDolares = r.Examen?.PrecioDolares ?? 0,
                        EsTercerizado = r.Examen?.EsTercerizado ?? false,
                        Muestra = r.Examen?.Observaciones,
                        MuestraEntregada = r.MuestraEntregada,
                        ValorResultado = r.ValorResultado,
                        RangoReferenciaAplicado = r.RangoReferenciaAplicado,
                        TecnicaAplicada = r.TecnicaAplicada,
                        FueraDeRango = r.FueraDeRango,
                        FechaCarga = r.FechaCarga,
                        Parametros = paramsDto
                    });
                }
            }

            // Buscar Bioanalista Validador si aplica
            BioanalistaValidadorDto? bioDto = null;
            if (bioanalistaId.HasValue)
            {
                var bioUser = await _context.Usuarios
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.UsuarioId == bioanalistaId.Value);

                if (bioUser != null)
                {
                    bioDto = new BioanalistaValidadorDto
                    {
                        UsuarioId = bioUser.UsuarioId,
                        NombreCompleto = bioUser.NombreUsuario,
                        Cedula = bioUser.Cedula,
                        CodigoColegioBioanalistas = bioUser.ColegioBioanalistas,
                        NumeroMpps = bioUser.Mpps,
                        AvatarUrl = bioUser.AvatarUrl
                    };
                }
            }

            var detalle = new OrdenDetalleCompletoDto
            {
                OrdenId = orden.OrdenId,
                CorrelativoDiario = orden.CorrelativoDiario,
                FechaOrden = orden.FechaOrden,
                FechaValidacionFinal = orden.FechaValidacionFinal,
                Estado = orden.Estado,
                MotivoExamen = orden.MotivoExamen,
                ObservacionBioanalista = orden.ObservacionBioanalista,

                PacienteId = orden.PacienteId,
                PacienteNombre = orden.Paciente?.NombreCompleto ?? "Desconocido",
                PacienteCedula = orden.Paciente?.Cedula ?? "S/C",
                PacienteSexo = orden.Paciente?.Sexo ?? "N/A",
                PacienteFechaNacimiento = orden.Paciente?.FechaNacimiento ?? DateTime.MinValue,
                PacienteEdad = edad,
                PacienteTelefono = orden.Paciente?.TelefonoPrincipal,
                PacienteDireccion = orden.Paciente?.Direccion,
                PacienteNumeroHistoria = orden.Paciente?.NumeroHistoria ?? orden.Paciente?.NumeroHistoriaFisica,

                Factura = facturaDto,
                Examenes = examenesDto,
                Bioanalista = bioDto,

                Clinica = new ClinicaCabeceraDto
                {
                    NombreClinica = orden.Tenant?.NombreClinica ?? "Sociedad Anticancerosa de La Victoria",
                    Rif = orden.Tenant?.Rif ?? "J-30426645-6",
                    Direccion = "Calle Rivas Dávila, Centro Médico Dr. Rubén Merenfeld, La Victoria, Aragua",
                    Telefono = "(0244) 321-4567"
                }
            };

            return Ok(detalle);
        }

        // ==========================================
        // 5. CARGA DE RESULTADOS POR EL BIOANALISTA
        // ==========================================
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Administrador},{Roles.Bioanalista}")]
        [HttpPut("{id}/resultados")]
        public async Task<IActionResult> IngresarResultados(Guid id, [FromBody] IngresarResultadosDto dto)
        {
            var tenantId = ObtenerTenantIdDelToken();

            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Resultados)
                .FirstOrDefaultAsync(o => o.OrdenId == id && o.TenantId == tenantId);

            if (orden == null)
                return NotFound(new { mensaje = "La orden clínica no existe." });

            if (orden.Estado == "Validada")
                return BadRequest(new { mensaje = "Esta orden ya se encuentra validada y bloqueada contra modificaciones." });

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
                        resultadoBd.MuestraEntregada = item.MuestraEntregada;

                        if (!string.IsNullOrEmpty(item.ValorResultado))
                        {
                            resultadoBd.FechaCarga = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Resultados analíticos guardados exitosamente", ordenId = orden.OrdenId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = $"Error al guardar resultados: {ex.Message}" });
            }
        }

        // ==========================================
        // 6. VALIDACIÓN Y FIRMA DIGITAL DEL BIOANALISTA
        // ==========================================
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Administrador},{Roles.Bioanalista}")]
        [HttpPatch("{id}/validar")]
        public async Task<IActionResult> ValidarOrden(Guid id, [FromBody] ValidacionOrdenDto dto)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var bioanalistaUsuarioId = ObtenerUsuarioGuidDelToken();

            var orden = await _context.OrdenesLaboratorio
                .Include(o => o.Resultados)
                .FirstOrDefaultAsync(o => o.OrdenId == id && o.TenantId == tenantId);

            if (orden == null)
                return NotFound(new { mensaje = "La orden no existe." });

            if (orden.Estado == "Validada")
                return BadRequest(new { mensaje = "Esta orden ya se encuentra validada y bloqueada." });

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
                            if (!string.IsNullOrEmpty(itemValidado.ValorResultado))
                                resultadoBd.ValorResultado = itemValidado.ValorResultado;

                            if (!string.IsNullOrEmpty(itemValidado.RangoReferenciaAplicado))
                                resultadoBd.RangoReferenciaAplicado = itemValidado.RangoReferenciaAplicado;

                            if (!string.IsNullOrEmpty(itemValidado.TecnicaAplicada))
                                resultadoBd.TecnicaAplicada = itemValidado.TecnicaAplicada;

                            resultadoBd.FueraDeRango = itemValidado.FueraDeRango;
                            resultadoBd.UuidBioanalista = bioanalistaUsuarioId;
                        }
                    }
                }

                orden.Estado = "Validada";
                orden.FechaValidacionFinal = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Orden validada exitosamente con firma y sello digital del bioanalista.",
                    ordenId = orden.OrdenId,
                    fechaValidacion = orden.FechaValidacionFinal
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = $"Error al validar orden: {ex.Message}" });
            }
        }

        // ==========================================
        // 7. REGISTRAR ABONO O PAGO POSTERIOR A UNA ORDEN
        // ==========================================
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Administrador},{Roles.Cajero},{Roles.Secretaria}")]
        [HttpPost("{id}/pagar")]
        public async Task<IActionResult> PagarOrden(Guid id, [FromBody] CrearPagoInicialDto dto)
        {
            var tenantId = ObtenerTenantIdDelToken();
            var usuarioId = ObtenerUsuarioGuidDelToken();

            var factura = await _context.Facturas
                .Include(f => f.Pagos)
                .FirstOrDefaultAsync(f => f.OrdenLaboratorioId == id && f.TenantId == tenantId);

            if (factura == null)
                return NotFound(new { mensaje = "No se encontró la factura asociada a esta orden." });

            if (dto.Monto <= 0)
                return BadRequest(new { mensaje = "El monto a registrar debe ser superior a 0." });

            var nuevoPago = new Pago
            {
                PagoId = Guid.NewGuid(),
                TenantId = tenantId,
                FacturaId = factura.FacturaId,
                UsuarioId = usuarioId,
                Monto = dto.Monto,
                Moneda = dto.Moneda.ToUpper(),
                MetodoPago = dto.MetodoPago,
                Referencia = dto.Referencia,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Pagos.Add(nuevoPago);

            // Recalcular saldo pagado
            decimal totalPagadoUsd = 0;
            foreach (var p in factura.Pagos.Concat(new[] { nuevoPago }))
            {
                if (p.Moneda.ToUpper() == "VES")
                    totalPagadoUsd += factura.TasaCambio > 0 ? (p.Monto / factura.TasaCambio) : 0;
                else
                    totalPagadoUsd += p.Monto;
            }

            if (totalPagadoUsd >= (factura.TotalNeto - 0.01m))
                factura.Estado = "Pagada";
            else if (totalPagadoUsd > 0.01m)
                factura.Estado = "Abonada";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Pago registrado exitosamente",
                facturaId = factura.FacturaId,
                estadoFactura = factura.Estado,
                totalPagadoUSD = totalPagadoUsd,
                saldoPendienteUSD = Math.Max(0, factura.TotalNeto - totalPagadoUsd)
            });
        }
    }
}