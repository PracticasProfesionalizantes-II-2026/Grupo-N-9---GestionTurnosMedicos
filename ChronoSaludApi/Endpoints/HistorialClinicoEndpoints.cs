using System.Security.Claims;
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
        grupo.MapPost("/", async (int id, HistorialClinicoCreateDto dto, HttpContext ctx, IHistorialClinicoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Descripcion) || string.IsNullOrEmpty(dto.Diagnostico))
                return Results.BadRequest(new { error = "Descripcion y diagnostico son requeridos." });

            var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var idUsuario))
                return Results.Unauthorized();

            var (ok, error) = await logica.Crear(id, idUsuario, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/pacientes/{id}/historiales-clinicos",
                new { mensaje = "Entrada en historial creada correctamente." });
        })
        .WithSummary("Registrar nueva entrada en historial")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // PUT /pacientes/{id}/historiales-clinicos/{idHistorial}
        grupo.MapPut("/{idHistorial:int}", async (int id, int idHistorial, HistorialClinicoCreateDto dto, IHistorialClinicoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Descripcion) || string.IsNullOrEmpty(dto.Diagnostico))
                return Results.BadRequest(new { error = "Descripcion y diagnostico son requeridos." });

            // Solo modifica la entrada indicada. Si no existe es 404: un PUT
            // nunca crea una entrada nueva en la historia clinica.
            var (ok, error) = await logica.Actualizar(id, idHistorial, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Historial modificado correctamente." });
        })
        .WithSummary("Modificar una entrada del historial clínico")
        .RequireAuthorization(p => p.RequireRole("doctor"));
    }
}
