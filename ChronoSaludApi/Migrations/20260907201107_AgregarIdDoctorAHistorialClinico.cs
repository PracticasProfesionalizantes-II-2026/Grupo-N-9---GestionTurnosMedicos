using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoSaludApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIdDoctorAHistorialClinico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdDoctor",
                table: "HistorialesClinicos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Relleno de las entradas que ya existian. Sin esto la FK de mas abajo
            // falla, porque EF deja todas las filas viejas en IdDoctor = 0 y no hay
            // ningun doctor con Id 0.
            //
            // OJO: solo el paso 1 recupera el autor real. Los pasos 2 y 3 son una
            // aproximacion para no perder las filas de prueba que ya estaban. Sobre
            // datos reales habria que resolver la autoria a mano ANTES de migrar.

            // 1) Autor conocido: el doctor del turno al que esta asociada la entrada.
            migrationBuilder.Sql(@"
                UPDATE h
                SET    h.IdDoctor = t.IdDoctor
                FROM   HistorialesClinicos h
                       INNER JOIN Turnos t ON t.Id = h.IdTurno
                WHERE  h.IdTurno IS NOT NULL;");

            // 2) Sin turno asociado: el doctor del turno mas reciente de ese paciente.
            migrationBuilder.Sql(@"
                UPDATE h
                SET    h.IdDoctor = (
                           SELECT TOP 1 t.IdDoctor
                           FROM   Turnos t
                           WHERE  t.IdPaciente = h.IdPaciente
                           ORDER  BY t.FechaInicio DESC, t.Id DESC)
                FROM   HistorialesClinicos h
                WHERE  h.IdDoctor = 0
                       AND EXISTS (SELECT 1 FROM Turnos t WHERE t.IdPaciente = h.IdPaciente);");

            // 3) Ultimo recurso: el doctor activo de menor Id.
            migrationBuilder.Sql(@"
                UPDATE HistorialesClinicos
                SET    IdDoctor = (SELECT MIN(Id) FROM Doctores WHERE Activo = 1)
                WHERE  IdDoctor = 0;");

            // Si quedo alguna sin resolver (base sin doctores), cortamos con un
            // mensaje claro en vez de dejar que reviente la FK.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM HistorialesClinicos WHERE IdDoctor = 0)
                    THROW 50000, 'No se pudo completar IdDoctor en HistorialesClinicos: no hay doctores cargados. Carga al menos un doctor y volve a correr la migracion.', 1;");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_IdDoctor",
                table: "HistorialesClinicos",
                column: "IdDoctor");

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialesClinicos_Doctores_IdDoctor",
                table: "HistorialesClinicos",
                column: "IdDoctor",
                principalTable: "Doctores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialesClinicos_Doctores_IdDoctor",
                table: "HistorialesClinicos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialesClinicos_IdDoctor",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "IdDoctor",
                table: "HistorialesClinicos");
        }
    }
}
