using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class EvitarTurnosDuplicados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Doctor_Dia_Hora",
                table: "Turnos",
                columns: new[] { "IdDoctor", "FechaInicio", "HoraInicio" },
                unique: true,
                filter: "[Estado] <> 'cancelado'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turnos_Doctor_Dia_Hora",
                table: "Turnos");
        }
    }
}
