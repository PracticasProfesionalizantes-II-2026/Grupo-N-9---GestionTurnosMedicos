using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class HorarioLaboralEndpoints
{
    public static void MapHorarioLaboralEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/doctores").WithTags("Horarios").RequireAuthorization();

        // GET /doctores/especialidades
        grupo.MapGet("/especialidades", async (IHorarioLaboralLogica logica) =>
        {
            var especialidades = await logica.ObtenerEspecialidades();
            return Results.Ok(especialidades);
        })
        .WithSummary("Listar especialidades de doctores activos");

        // GET /doctores/{id}/horarios
        grupo.MapGet("/{id:int}/horarios", async (int id, IHorarioLaboralLogica logica) =>
        {
            var (horarios, error) = await logica.ObtenerPorDoctor(id);
            return horarios == null
                ? Results.NotFound(new { error })
                : Results.Ok(horarios);
        })
        .WithSummary("Obtener horario semanal del doctor");

        // PUT /doctores/{id}/horarios
        grupo.MapPut("/{id:int}/horarios", async (int id, HorarioSemanalDto dto, HttpContext ctx, IHorarioLaboralLogica logica) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            var (ok, error, conflictos, reintentar, prohibido) = await logica.Reemplazar(id, dto, solicitante);
            if (!ok)
            {
                // Un doctor sobre un horario que no es el suyo: 403 con el motivo.
                if (prohibido)
                    return Results.Json(new { error }, statusCode: StatusCodes.Status403Forbidden);

                // Turnos reservados que quedarían fuera: no se guardó nada.
                if (conflictos.Count > 0)
                    return Results.Conflict(new { error, conflictos });

                // La base estaba ocupada: 503 con Retry-After, para que quien
                // llama sepa que repetir el mismo pedido es seguro.
                if (reintentar)
                {
                    ctx.Response.Headers.RetryAfter = "1";
                    return Results.Json(new { error }, statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                return error!.Contains("no encontrado") || error == HorarioLaboralLogica.SinPerfilDeDoctor
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });
            }

            return Results.Ok(new { mensaje = "Horario actualizado correctamente." });
        })
        .WithSummary("Reemplazar horario semanal del doctor (administrador: cualquiera; doctor: solo el suyo)")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // GET /doctores/{id}/disponibilidad?fecha=YYYY-MM-DD
        grupo.MapGet("/{id:int}/disponibilidad", async (int id, DateTime? fecha, IHorarioLaboralLogica logica) =>
        {
            if (!fecha.HasValue)
                return Results.BadRequest(new { error = "fecha es requerida." });

            var (franjas, error) = await logica.ObtenerDisponibilidad(id, fecha.Value);
            if (franjas == null)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(franjas);
        })
        .WithSummary("Franjas libres del doctor en una fecha");
    }
}
