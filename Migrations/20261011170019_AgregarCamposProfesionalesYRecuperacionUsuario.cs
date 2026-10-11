using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposProfesionalesYRecuperacionUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "Usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacion",
                table: "Usuarios",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColegioBioanalistas",
                table: "Usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiracionCodigo",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mpps",
                table: "Usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoRecuperacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ColegioBioanalistas",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaExpiracionCodigo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Mpps",
                table: "Usuarios");
        }
    }
}
