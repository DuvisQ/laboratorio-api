using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Models;

namespace Laboratorio.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<ExamenCatalogo> ExamenesCatalogo { get; set; }
        public DbSet<OrdenLaboratorio> OrdenesLaboratorio { get; set; }
        public DbSet<ResultadoDetalle> ResultadosDetalle { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("uuid-ossp");

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
    }
}