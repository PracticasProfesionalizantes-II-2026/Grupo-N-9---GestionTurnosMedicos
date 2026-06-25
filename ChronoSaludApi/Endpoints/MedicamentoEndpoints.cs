using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class MedicamentoEndpoints
{
    public static void MapMedicamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/medicamentos").WithTags("Medicamentos").RequireAuthorization();

        // GET /medicamentos
        grupo.MapGet("/", async (IMedicamentoLogica logica) =>
        {
            var medicamentos = await logica.ObtenerTodos();
            return Results.Ok(new { medicamentos });
        })
        .WithSummary("Listar medicamentos");

        // GET /medicamentos/{id}
        grupo.MapGet("/{id:int}", async (int id, IMedicamentoLogica logica) =>
        {
            var med = await logica.ObtenerPorId(id);
            return med == null
                ? Results.NotFound(new { error = "Medicamento no encontrado." })
                : Results.Ok(med);
        })
        .WithSummary("Obtener medicamento por ID");

        // POST /medicamentos
        grupo.MapPost("/", async (MedicamentoCreateDto dto, IMedicamentoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Nombre))
                return Results.BadRequest(new { error = "El nombre del medicamento es requerido." });

            var id = await logica.Crear(dto);
            return Results.Created($"/medicamentos/{id}", new { id_medicamento = id, dto.Nombre });
        })
        .WithSummary("Crear medicamento")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // PUT /medicamentos/{id}
        grupo.MapPut("/{id:int}", async (int id, MedicamentoCreateDto dto, IMedicamentoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Nombre))
                return Results.BadRequest(new { error = "El nombre del medicamento es requerido." });

            var ok = await logica.Actualizar(id, dto);
            return ok
                ? Results.Ok(new { mensaje = "Medicamento actualizado correctamente." })
                : Results.NotFound(new { error = "Medicamento no encontrado." });
        })
        .WithSummary("Actualizar medicamento")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // DELETE /medicamentos/{id}
        grupo.MapDelete("/{id:int}", async (int id, IMedicamentoLogica logica) =>
        {
            var ok = await logica.Eliminar(id);
            return ok
                ? Results.NoContent()
                : Results.NotFound(new { error = "Medicamento no encontrado." });
        })
        .WithSummary("Eliminar medicamento")
        .RequireAuthorization(p => p.RequireRole("administrador"));
    }
}
