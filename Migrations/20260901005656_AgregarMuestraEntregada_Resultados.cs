using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMuestraEntregada_Resultados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MuestraEntregada",
                table: "Resultados_Detalle",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuestraEntregada",
                table: "Resultados_Detalle");
        }
    }
}
