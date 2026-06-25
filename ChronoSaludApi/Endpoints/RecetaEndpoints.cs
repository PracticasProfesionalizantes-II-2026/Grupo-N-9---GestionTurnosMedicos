using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class RecetaEndpoints
{
    public static void MapRecetaEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /pacientes/{id}/recetas
        app.MapGet("/pacientes/{id:int}/recetas", async (int id, IRecetaLogica logica) =>
        {
            var recetas = await logica.ObtenerDePaciente(id);
            return Results.Ok(new { recetas });
        })
        .WithTags("Recetas")
        .WithSummary("Listar recetas del paciente")
        .RequireAuthorization();

        var grupo = app.MapGroup("/recetas").WithTags("Recetas").RequireAuthorization();

        // POST /recetas
        grupo.MapPost("/", async (RecetaCreateDto dto, IRecetaLogica logica) =>
        {
            if (dto.IdPaciente == 0 || dto.IdDoctor == 0)
                return Results.BadRequest(new { error = "id_paciente e id_doctor son requeridos." });

            if (dto.Medicamentos == null || !dto.Medicamentos.Any())
                return Results.BadRequest(new { error = "Debe incluir al menos un medicamento." });

            var (id, error) = await logica.Crear(dto);
            if (error != null)
                return error.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/recetas/{id}", new
            {
                id_receta    = id,
                fecha        = dto.Fecha,
                vigencia     = dto.Vigencia,
                medicamentos = dto.Medicamentos
            });
        })
        .WithSummary("Emitir receta médica")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // PUT /recetas/{id}
        grupo.MapPut("/{id:int}", async (int id, RecetaCreateDto dto, IRecetaLogica logica) =>
        {
            if (dto.Medicamentos == null || !dto.Medicamentos.Any())
                return Results.BadRequest(new { error = "Debe incluir al menos un medicamento." });

            var (ok, error) = await logica.Actualizar(id, dto);
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Receta modificada correctamente." });
        })
        .WithSummary("Modificar receta médica")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // GET /recetas/{id}/descargar  (placeholder PDF)
        grupo.MapGet("/{id:int}/descargar", async (int id, IRecetaLogica logica) =>
        {
            var receta = await logica.ObtenerPorId(id);
            if (receta == null)
                return Results.NotFound(new { error = "Receta no encontrada." });

            // Placeholder: devuelve metadata. En producción se generaría un PDF real.
            return Results.Ok(new
            {
                mensaje  = "Endpoint listo. Integrar generador de PDF (ej: QuestPDF) para producción.",
                id_receta = id,
                fecha    = receta.Fecha,
                vigencia = receta.Vigencia
            });
        })
        .WithSummary("Descargar receta en PDF");
    }
}
