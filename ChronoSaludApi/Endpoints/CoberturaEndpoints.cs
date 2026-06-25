using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class CoberturaEndpoints
{
    public static void MapCoberturaEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /coberturas
        app.MapGet("/coberturas", async (ICoberturaLogica logica, string? nombre) =>
        {
            var coberturas = await logica.ObtenerTodas(nombre);
            return Results.Ok(new { coberturas });
        })
        .WithTags("Coberturas")
        .WithSummary("Listar coberturas")
        .RequireAuthorization();

        // Rutas anidadas bajo /pacientes/{id}/coberturas
        var grupo = app.MapGroup("/pacientes/{id:int}/coberturas")
            .WithTags("Coberturas")
            .RequireAuthorization();

        // POST /pacientes/{id}/coberturas
        grupo.MapPost("/", async (int id, AsociarCoberturaDto dto, ICoberturaLogica logica) =>
        {
            if (dto.IdCobertura == 0 || string.IsNullOrEmpty(dto.IdAfiliado))
                return Results.BadRequest(new { error = "id_cobertura e id_afiliado son requeridos." });

            var (ok, error) = await logica.AsociarAPaciente(id, dto);
            if (!ok)
            {
                if (error!.Contains("ya está asociada")) return Results.Conflict(new { error });
                if (error.Contains("no encontrada"))     return Results.NotFound(new { error });
                return Results.BadRequest(new { error });
            }
            return Results.Created($"/pacientes/{id}/coberturas", new { mensaje = "Cobertura asociada correctamente." });
        })
        .WithSummary("Asociar cobertura a paciente");

        // PUT /pacientes/{id}/coberturas
        grupo.MapPut("/", async (int id, AsociarCoberturaDto dto, ICoberturaLogica logica) =>
        {
            if (dto.IdCobertura == 0 || string.IsNullOrEmpty(dto.IdAfiliado))
                return Results.BadRequest(new { error = "id_cobertura e id_afiliado son requeridos." });

            var (ok, error) = await logica.ActualizarDePaciente(id, dto);
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Cobertura modificada correctamente." });
        })
        .WithSummary("Modificar cobertura de paciente");

        // DELETE /pacientes/{id}/coberturas/{cobertura_id}
        grupo.MapDelete("/{cobertura_id:int}", async (int id, int cobertura_id, ICoberturaLogica logica) =>
        {
            var (ok, error) = await logica.DesvincularDePaciente(id, cobertura_id);
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.NoContent();
        })
        .WithSummary("Desvincular cobertura de paciente");
    }
}
