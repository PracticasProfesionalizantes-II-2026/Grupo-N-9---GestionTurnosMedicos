using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHorarioLaboral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HorariosLaborales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDoctor = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosLaborales", x => x.Id);
                    table.CheckConstraint("CK_HorarioLaboral_DiaSemana", "[DiaSemana] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_HorarioLaboral_Horas", "[HoraFin] > [HoraInicio]");
                    table.ForeignKey(
                        name: "FK_HorariosLaborales_Doctores_IdDoctor",
                        column: x => x.IdDoctor,
                        principalTable: "Doctores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosLaborales_IdDoctor_DiaSemana",
                table: "HorariosLaborales",
                columns: new[] { "IdDoctor", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosLaborales");
        }
    }
}
