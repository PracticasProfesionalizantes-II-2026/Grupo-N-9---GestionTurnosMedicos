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
        grupo.MapPut("/{id:int}/horarios", async (int id, HorarioSemanalDto dto, IHorarioLaboralLogica logica) =>
        {
            var (ok, error) = await logica.Reemplazar(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Horario actualizado correctamente." });
        })
        .WithSummary("Reemplazar horario semanal del doctor")
        .RequireAuthorization(p => p.RequireRole("administrador", "secretario"));

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
