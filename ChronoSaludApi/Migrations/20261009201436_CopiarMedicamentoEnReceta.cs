using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class CopiarMedicamentoEnReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecetaMedicamentos_Medicamentos_IdMedicamento",
                table: "RecetaMedicamentos");

            migrationBuilder.AddColumn<string>(
                name: "Concentracion",
                table: "RecetaMedicamentos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormaFarmaceutica",
                table: "RecetaMedicamentos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreGenerico",
                table: "RecetaMedicamentos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreMedicamento",
                table: "RecetaMedicamentos",
                type: "nvarchar(max)",
                nullable: true);

            // Las recetas que ya existen guardan la copia del medicamento como
            // está hoy. Va dentro de EXEC para que el script de Azure no falle:
            // SQL Server revisa las columnas antes de que se creen.
            migrationBuilder.Sql(@"EXEC(N'
            UPDATE rm
            SET rm.NombreMedicamento = m.Nombre,
                rm.NombreGenerico = m.NombreGenerico,
                rm.Concentracion = m.Concentracion,
                rm.FormaFarmaceutica = m.FormaFarmaceutica
            FROM RecetaMedicamentos rm
            INNER JOIN Medicamentos m ON m.Id = rm.IdMedicamento
            WHERE rm.NombreMedicamento IS NULL;')");

            migrationBuilder.AddForeignKey(
                name: "FK_RecetaMedicamentos_Medicamentos_IdMedicamento",
                table: "RecetaMedicamentos",
                column: "IdMedicamento",
                principalTable: "Medicamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecetaMedicamentos_Medicamentos_IdMedicamento",
                table: "RecetaMedicamentos");

            migrationBuilder.DropColumn(
                name: "Concentracion",
                table: "RecetaMedicamentos");

            migrationBuilder.DropColumn(
                name: "FormaFarmaceutica",
                table: "RecetaMedicamentos");

            migrationBuilder.DropColumn(
                name: "NombreGenerico",
                table: "RecetaMedicamentos");

            migrationBuilder.DropColumn(
                name: "NombreMedicamento",
                table: "RecetaMedicamentos");

            migrationBuilder.AddForeignKey(
                name: "FK_RecetaMedicamentos_Medicamentos_IdMedicamento",
                table: "RecetaMedicamentos",
                column: "IdMedicamento",
                principalTable: "Medicamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
