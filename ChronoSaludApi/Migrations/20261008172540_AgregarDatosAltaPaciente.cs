using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosAltaPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoPostal",
                table: "Pacientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaNombre",
                table: "Pacientes",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaTelefono",
                table: "Pacientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localidad",
                table: "Pacientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                table: "Pacientes",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Pacientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoPostal",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaNombre",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaTelefono",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Localidad",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Provincia",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Pacientes");
        }
    }
}
