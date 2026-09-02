using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;
using Laboratorio.Api.Models;
using System.Security.Claims;
using BCrypt.Net;

namespace Laboratorio.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FacturacionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FacturacionController(AppDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // 0. CREAR FACTURA A PARTIR DE UNA ORDEN
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CrearFactura([FromBody] CrearFacturaDto dto)
        {
            var tenantIdStr = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdStr))
                return Unauthorized(new { mensaje = "Token inválido o sin credenciales de inquilino." });

            Guid tenantId = Guid.Parse(tenantIdStr);

            // Verificar que la orden exista y pertenezca al tenant
            var orden = await _context.OrdenesLaboratorio
                .FirstOrDefaultAsync(o => o.OrdenId == dto.OrdenLaboratorioId && o.TenantId == tenantId);

            if (orden == null)
                return NotFound(new { mensaje = "La orden de laboratorio no existe o no pertenece a este tenant." });

            // Verificar si ya existe una factura para esta orden
            var facturaExistente = await _context.Facturas
                .FirstOrDefaultAsync(f => f.OrdenLaboratorioId == dto.OrdenLaboratorioId && f.TenantId == tenantId);

            if (facturaExistente != null)
                return BadRequest(new { mensaje = "Ya existe una factura asociada a esta orden de laboratorio.", facturaId = facturaExistente.FacturaId });

            // Calcular impuestos y totales automáticos
            var montoIva = dto.SubTotal * (dto.PorcentajeIva / 100);
            var totalNeto = dto.SubTotal + montoIva;

            var nuevaFactura = new Factura
            {
                FacturaId = Guid.NewGuid(),
                TenantId = tenantId,
                OrdenLaboratorioId = dto.OrdenLaboratorioId,
                SubTotal = dto.SubTotal,
                PorcentajeIva = dto.PorcentajeIva,
                MontoIva = montoIva,
                TotalNeto = totalNeto,
                TasaCambio = dto.TasaCambio,
                Estado = "Pendiente",
                FechaEmision = DateTime.UtcNow
            };

            _context.Facturas.Add(nuevaFactura);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Factura creada con éxito", facturaId = nuevaFactura.FacturaId, totalNeto = nuevaFactura.TotalNeto });
        }

        // ==========================================
        // 1. REGISTRAR UN PAGO (ABONO O PAGO TOTAL)
        // ==========================================
        [HttpPost("pagar")]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoDto dto)
        {
            var tenantIdStr = User.FindFirst("TenantId")?.Value;
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(tenantIdStr) || string.IsNullOrEmpty(usuarioIdStr))
                return Unauthorized(new { mensaje = "Token inválido o sin credenciales de inquilino/usuario." });

            Guid tenantId = Guid.Parse(tenantIdStr);
            Guid usuarioId = Guid.Parse(usuarioIdStr);

            // Buscar la factura
            var factura = await _context.Facturas
                .Include(f => f.Pagos)
                .FirstOrDefaultAsync(f => f.FacturaId == dto.FacturaId && f.TenantId == tenantId);

            if (factura == null)
                return NotFound(new { mensaje = "Factura no encontrada." });

            // Registrar el pago
            var nuevoPago = new Pago
            {
                PagoId = Guid.NewGuid(),
                TenantId = tenantId,
                FacturaId = factura.FacturaId,
                UsuarioId = usuarioId,
                Monto = dto.Monto,
                Moneda = dto.Moneda,
                MetodoPago = dto.MetodoPago,
                Referencia = dto.Referencia,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Pagos.Add(nuevoPago);

            // Calcular si la factura queda pagada o abonada
            // (Nota: Si pagas en otra moneda, aquí aplicarías la conversión usando factura.TasaCambio)
            var totalPagadoHistorico = factura.Pagos.Sum(p => p.Monto) + dto.Monto;

            if (totalPagadoHistorico >= factura.TotalNeto)
            {
                factura.Estado = "Pagada";
            }
            else
            {
                factura.Estado = "Abonada";
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pago registrado con éxito", estadoFactura = factura.Estado, totalPagado = totalPagadoHistorico });
        }

        // ==========================================
        // 2. APLICAR DESCUENTO CON PIN DE SUPERVISOR
        // ==========================================
        [HttpPost("descuento")]
        public async Task<IActionResult> AplicarDescuento([FromBody] AplicarDescuentoDto dto)
        {
            var tenantIdStr = User.FindFirst("TenantId")?.Value;
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(tenantIdStr) || string.IsNullOrEmpty(usuarioIdStr))
                return Unauthorized(new { mensaje = "Token inválido." });

            Guid tenantId = Guid.Parse(tenantIdStr);
            Guid cajeroId = Guid.Parse(usuarioIdStr);

            // Buscar la factura
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.FacturaId == dto.FacturaId && f.TenantId == tenantId);

            if (factura == null)
                return NotFound(new { mensaje = "Factura no encontrada." });

            // Buscar al administrador o usuario autorizado por medio del PIN
            // Buscamos usuarios de este Tenant que tengan rol Administrador y un PIN configurado
            var administradores = await _context.Usuarios
                .Where(u => u.TenantId == tenantId && u.Rol == "Administrador" && u.PinAutorizacion != null)
                .ToListAsync();

            Usuario? adminAutorizador = null;
            foreach (var admin in administradores)
            {
                // Verificamos el PIN ingresado contra el hash guardado usando BCrypt
                if (!string.IsNullOrEmpty(admin.PinAutorizacion) && BCrypt.Net.BCrypt.Verify(dto.PinAdministrador, admin.PinAutorizacion))
                {
                    adminAutorizador = admin;
                    break;
                }
            }

            if (adminAutorizador == null)
                return BadRequest(new { mensaje = "PIN de autorización inválido o usuario sin privilegios de administrador." });

            // Aplicar el descuento a la factura
            if (dto.MontoDescuento > factura.SubTotal)
                return BadRequest(new { mensaje = "El descuento no puede ser mayor al subtotal." });

            factura.MontoDescuento = dto.MontoDescuento;
            factura.MotivoDescuento = dto.MotivoDescuento;
            factura.DescuentoAplicadoPor = cajeroId;
            factura.DescuentoAutorizadoPor = adminAutorizador.UsuarioId;
            factura.FechaDescuento = DateTime.UtcNow;

            // Recalcular el Total Neto (SubTotal - Descuento + IVA)
            var baseImponible = factura.SubTotal - factura.MontoDescuento;
            factura.MontoIva = baseImponible * (factura.PorcentajeIva / 100);
            factura.TotalNeto = baseImponible + factura.MontoIva;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Descuento aplicado y auditado con éxito", nuevoTotal = factura.TotalNeto });
        }
    }

    // ==========================================
    // DTOs (Data Transfer Objects) para las peticiones
    // ==========================================
    public class RegistrarPagoDto
    {
        public Guid FacturaId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "USD";
        public string MetodoPago { get; set; } = "Efectivo";
        public string? Referencia { get; set; }
    }

    public class AplicarDescuentoDto
    {
        public Guid FacturaId { get; set; }
        public decimal MontoDescuento { get; set; }
        public string MotivoDescuento { get; set; } = string.Empty;
        public string PinAdministrador { get; set; } = string.Empty;
    }

    public class CrearFacturaDto
    {
        public Guid OrdenLaboratorioId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal PorcentajeIva { get; set; } = 0; // 0 para exentos de salud
        public decimal TasaCambio { get; set; } // Tasa oficial del BCV del día
    }
}