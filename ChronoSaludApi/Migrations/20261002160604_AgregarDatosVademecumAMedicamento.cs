using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosVademecumAMedicamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Concentracion",
                table: "Medicamentos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormaFarmaceutica",
                table: "Medicamentos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Laboratorio",
                table: "Medicamentos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreGenerico",
                table: "Medicamentos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Concentracion",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "FormaFarmaceutica",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "Laboratorio",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "NombreGenerico",
                table: "Medicamentos");
        }
    }
}
