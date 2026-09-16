using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosRepresentantePaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CedulaRepresentante",
                table: "Pacientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreRepresentante",
                table: "Pacientes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentescoRepresentante",
                table: "Pacientes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CedulaRepresentante",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NombreRepresentante",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ParentescoRepresentante",
                table: "Pacientes");
        }
    }
}
