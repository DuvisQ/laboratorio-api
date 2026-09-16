using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Models;
using Laboratorio.Api.Services;

namespace Laboratorio.Api.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantService? _tenantService;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService? tenantService = null) : base(options)
        {
            _tenantService = tenantService;
        }

        // Propiedad dinámica que evalúa el TenantId del usuario autenticado en cada petición
        public Guid? CurrentTenantId => _tenantService?.ObtenerTenantId();

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<ExamenCatalogo> ExamenesCatalogo { get; set; }
        public DbSet<OrdenLaboratorio> OrdenesLaboratorio { get; set; }
        public DbSet<ResultadoDetalle> ResultadosDetalle { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<ExamenParametro> ExamenParametros { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("uuid-ossp");

            // ==========================================
            // FILTROS GLOBALES MULTI-TENANT (Aislamiento automático por clínica)
            // ==========================================
            modelBuilder.Entity<Paciente>()
                .HasQueryFilter(p => CurrentTenantId == null || p.TenantId == CurrentTenantId);

            modelBuilder.Entity<OrdenLaboratorio>()
                .HasQueryFilter(o => CurrentTenantId == null || o.TenantId == CurrentTenantId);

            modelBuilder.Entity<ExamenCatalogo>()
                .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

            modelBuilder.Entity<Pago>()
                .HasQueryFilter(p => CurrentTenantId == null || p.TenantId == CurrentTenantId);

            modelBuilder.Entity<Factura>()
                .HasQueryFilter(f => CurrentTenantId == null || f.TenantId == CurrentTenantId);

            modelBuilder.Entity<Usuario>()
                .HasQueryFilter(u => CurrentTenantId == null || u.TenantId == CurrentTenantId);

            // ==========================================
            // REGLAS E ÍNDICES
            // ==========================================
            // Índice único para asegurar que no se repita la cédula dentro del mismo Tenant/Clínica
            modelBuilder.Entity<Paciente>()
                .HasIndex(p => new { p.TenantId, p.Cedula })
                .IsUnique();

            // Relación entre la Orden y sus Resultados (Detalles)
            modelBuilder.Entity<ResultadoDetalle>()
                .HasOne(r => r.Orden)
                .WithMany(o => o.Resultados)
                .HasForeignKey(r => r.OrdenId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre la Orden y el Paciente
            modelBuilder.Entity<OrdenLaboratorio>()
                .HasOne(o => o.Paciente)
                .WithMany()
                .HasForeignKey(o => o.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre la Orden y el Tenant (Clínica)
            modelBuilder.Entity<OrdenLaboratorio>()
                .HasOne(o => o.Tenant)
                .WithMany()
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Asignación automática de TenantId a nuevas entidades si el usuario está autenticado
            if (CurrentTenantId.HasValue)
            {
                foreach (var entry in ChangeTracker.Entries())
                {
                    if (entry.State == EntityState.Added)
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
                        if (prop != null && (prop.CurrentValue == null || (Guid)prop.CurrentValue == Guid.Empty))
                        {
                            prop.CurrentValue = CurrentTenantId.Value;
                        }
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}