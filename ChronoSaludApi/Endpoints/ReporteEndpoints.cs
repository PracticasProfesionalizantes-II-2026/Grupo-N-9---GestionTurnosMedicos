using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Endpoints;

public static class ReporteEndpoints
{
    public static void MapReporteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/reportes")
            .WithTags("Reportes")
            .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // GET /reportes/turnos
        grupo.MapGet("/turnos", async (
            ITurnoLogica logica,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int? doctor_id,
            string? especialidad,
            string? formato) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            var (total, turnos) = await logica.ObtenerTodos(
                null, doctor_id, null, fecha_desde, fecha_hasta, 1, int.MaxValue);

            var completados = turnos.Count(t => t.Estado == "completado");
            var cancelados  = turnos.Count(t => t.Estado == "cancelado");
            var pendientes  = turnos.Count(t => t.Estado == "pendiente");

            return Results.Ok(new
            {
                total_turnos = total,
                completados,
                cancelados,
                pendientes,
                periodo = new { desde = fecha_desde, hasta = fecha_hasta }
            });
        })
        .WithSummary("Reporte estadístico de turnos");

        // GET /reportes/pacientes
        grupo.MapGet("/pacientes", async (
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            string? formato) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            // Placeholder: En producción consultar métricas reales del DB
            return Results.Ok(new
            {
                mensaje = "Reporte de pacientes generado.",
                periodo = new { desde = fecha_desde, hasta = fecha_hasta }
            });
        })
        .WithSummary("Reporte de actividad de pacientes")
        .RequireAuthorization(p => p.RequireRole("administrador"));

        // GET /reportes/disponibilidad
        grupo.MapGet("/disponibilidad", async (
            int? doctor_id,
            DateTime? fecha_desde,
            DateTime? fecha_hasta) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            // Placeholder: En producción calcular franjas libres
            return Results.Ok(new
            {
                mensaje    = "Reporte de disponibilidad generado.",
                doctor_id,
                periodo = new { desde = fecha_desde, hasta = fecha_hasta }
            });
        })
        .WithSummary("Reporte de disponibilidad de profesionales");
    }
}
