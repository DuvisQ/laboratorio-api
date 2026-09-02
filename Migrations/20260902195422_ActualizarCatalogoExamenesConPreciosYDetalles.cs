using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarCatalogoExamenesConPreciosYDetalles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreParametro",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "RangoReferenciaDefecto",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "TecnicaDefecto",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "Examenes_Catalogo");

            migrationBuilder.AddColumn<string>(
                name: "CodigoProveedor",
                table: "Examenes_Catalogo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CostoDolares",
                table: "Examenes_Catalogo",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NombreExamen",
                table: "Examenes_Catalogo",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Examenes_Catalogo",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioDolares",
                table: "Examenes_Catalogo",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TiempoRespuesta",
                table: "Examenes_Catalogo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Examenes_Parametros",
                columns: table => new
                {
                    ExamenParametroId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamenId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreParametro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Unidades = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RangoReferenciaDefecto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TecnicaDefecto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examenes_Parametros", x => x.ExamenParametroId);
                    table.ForeignKey(
                        name: "FK_Examenes_Parametros_Examenes_Catalogo_ExamenId",
                        column: x => x.ExamenId,
                        principalTable: "Examenes_Catalogo",
                        principalColumn: "ExamenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Examenes_Parametros_ExamenId",
                table: "Examenes_Parametros",
                column: "ExamenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Examenes_Parametros");

            migrationBuilder.DropColumn(
                name: "CodigoProveedor",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "CostoDolares",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "NombreExamen",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "PrecioDolares",
                table: "Examenes_Catalogo");

            migrationBuilder.DropColumn(
                name: "TiempoRespuesta",
                table: "Examenes_Catalogo");

            migrationBuilder.AddColumn<string>(
                name: "NombreParametro",
                table: "Examenes_Catalogo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RangoReferenciaDefecto",
                table: "Examenes_Catalogo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TecnicaDefecto",
                table: "Examenes_Catalogo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unidades",
                table: "Examenes_Catalogo",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
