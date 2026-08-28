using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreClinica = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Rif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "Examenes_Catalogo",
                columns: table => new
                {
                    ExamenId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreParametro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unidades = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RangoReferenciaDefecto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TecnicaDefecto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EsTercerizado = table.Column<bool>(type: "boolean", nullable: false),
                    LaboratorioDestino = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examenes_Catalogo", x => x.ExamenId);
                    table.ForeignKey(
                        name: "FK_Examenes_Catalogo_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Sexo = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "date", nullable: false),
                    TelefonoPrincipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TelefonoRepresentante = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NumeroHistoriaFisica = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.PacienteId);
                    table.ForeignKey(
                        name: "FK_Pacientes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ordenes_Laboratorio",
                columns: table => new
                {
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelativoDiario = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MotivoExamen = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ObservacionBioanalista = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PermitirEnvioParcial = table.Column<bool>(type: "boolean", nullable: false),
                    RutaArchivoExterno = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaOrden = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaValidacionFinal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordenes_Laboratorio", x => x.OrdenId);
                    table.ForeignKey(
                        name: "FK_Ordenes_Laboratorio_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "PacienteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resultados_Detalle",
                columns: table => new
                {
                    ResultadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValorResultado = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RangoReferenciaAplicado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TecnicaAplicada = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EstadoTercerizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FueraDeRango = table.Column<bool>(type: "boolean", nullable: false),
                    UuidBioanalista = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resultados_Detalle", x => x.ResultadoId);
                    table.ForeignKey(
                        name: "FK_Resultados_Detalle_Examenes_Catalogo_ExamenId",
                        column: x => x.ExamenId,
                        principalTable: "Examenes_Catalogo",
                        principalColumn: "ExamenId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resultados_Detalle_Ordenes_Laboratorio_OrdenId",
                        column: x => x.OrdenId,
                        principalTable: "Ordenes_Laboratorio",
                        principalColumn: "OrdenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Examenes_Catalogo_TenantId",
                table: "Examenes_Catalogo",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_Laboratorio_PacienteId",
                table: "Ordenes_Laboratorio",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TenantId_Cedula",
                table: "Pacientes",
                columns: new[] { "TenantId", "Cedula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resultados_Detalle_ExamenId",
                table: "Resultados_Detalle",
                column: "ExamenId");

            migrationBuilder.CreateIndex(
                name: "IX_Resultados_Detalle_OrdenId",
                table: "Resultados_Detalle",
                column: "OrdenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Resultados_Detalle");

            migrationBuilder.DropTable(
                name: "Examenes_Catalogo");

            migrationBuilder.DropTable(
                name: "Ordenes_Laboratorio");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
