using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPacienteYDatosUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Pacientes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoCivil",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nacionalidad",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_Dni",
                table: "Pacientes",
                column: "Dni",
                unique: true,
                filter: "[Dni] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pacientes_Dni",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "EstadoCivil",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Nacionalidad",
                table: "Pacientes");
        }
    }
}
