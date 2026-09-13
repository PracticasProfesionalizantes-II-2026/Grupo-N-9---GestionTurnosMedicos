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
            HttpContext ctx,
            IHistorialClinicoLogica logica,
            DateTime? fecha_desde,
            DateTime? fecha_hasta) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (idPaciente, historiales, error, prohibido) = await logica.ObtenerDePaciente(id, fecha_desde, fecha_hasta, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (error != null) return Results.NotFound(new { error });

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
        grupo.MapPut("/{idHistorial:int}", async (int id, int idHistorial, HistorialClinicoCreateDto dto, HttpContext ctx, IHistorialClinicoLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Descripcion) || string.IsNullOrEmpty(dto.Diagnostico))
                return Results.BadRequest(new { error = "Descripcion y diagnostico son requeridos." });

            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            // Solo modifica la entrada indicada, y solo si el caller es el doctor
            // autor. Si no existe, es de otro paciente o es de otro doctor, 404:
            // un PUT nunca crea una entrada nueva en la historia clinica.
            var (ok, error, sinPerfilDoctor) = await logica.Actualizar(id, idHistorial, dto, idUsuario);
            if (!ok)
            {
                if (sinPerfilDoctor) return Results.NotFound(new { error });
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });
            }

            return Results.Ok(new { mensaje = "Historial modificado correctamente." });
        })
        .WithSummary("Modificar una entrada del historial clínico")
        .RequireAuthorization(p => p.RequireRole("doctor"));
    }
}
