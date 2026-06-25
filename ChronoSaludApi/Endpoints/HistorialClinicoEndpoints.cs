using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class HistorialClinicoEndpoints
{
    public static void MapHistorialClinicoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pacientes/{id:int}/historiales-clinicos")
            .WithTags("Historiales Clínicos")
            .RequireAuthorization();

        // GET /pacientes/{id}/historiales-clinicos
        grupo.MapGet("/", async (
            int id,
            IHistorialClinicoLogica logica,
            DateTime? fecha_desde,
            DateTime? fecha_hasta) =>
        {
            var resultado = await logica.ObtenerDePaciente(id, fecha_desde, fecha_hasta);
            if (resultado == null)
                return Results.NotFound(new { error = "Paciente no encontrado." });

            var (idPaciente, historiales) = resultado.Value;
            return Results.Ok(new { id_paciente = idPaciente, historiales });
        })
        .WithSummary("Obtener historial clínico del paciente");

        // POST /pacientes/{id}/historiales-clinicos
        grupo.MapPost("/", async (int id, HistorialClinicoCreateDto dto, IHistorialClinicoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Descripcion) || string.IsNullOrEmpty(dto.Diagnostico))
                return Results.BadRequest(new { error = "Descripcion y diagnostico son requeridos." });

            var (ok, error) = await logica.Crear(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/pacientes/{id}/historiales-clinicos",
                new { mensaje = "Entrada en historial creada correctamente." });
        })
        .WithSummary("Registrar nueva entrada en historial")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // PUT /pacientes/{id}/historiales-clinicos
        grupo.MapPut("/", async (int id, HistorialClinicoCreateDto dto, IHistorialClinicoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Descripcion) || string.IsNullOrEmpty(dto.Diagnostico))
                return Results.BadRequest(new { error = "Descripcion y diagnostico son requeridos." });

            var (ok, error) = await logica.Actualizar(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Historial modificado correctamente." });
        })
        .WithSummary("Modificar historial clínico")
        .RequireAuthorization(p => p.RequireRole("doctor"));
    }
}
