using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Endpoints;

public static class MovimientoEndpoints
{
    public static void MapMovimientoEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /movimientos
        // Solo lectura: el historial no tiene alta, edición ni borrado por HTTP.
        app.MapGet("/movimientos", async (
            HttpContext ctx,
            IMovimientoLogica logica,
            int? doctor_id,
            string? accion,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int pagina = 1,
            int limite = 20) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            var (total, movimientos, error, prohibido) = await logica.Buscar(
                doctor_id, accion, fecha_desde, fecha_hasta, pagina, limite, solicitante);

            if (prohibido) return Results.Forbid();
            if (error != null) return Results.NotFound(new { error });

            return Results.Ok(new { total, movimientos });
        })
        .WithTags("Movimientos")
        .WithSummary("Historial de movimientos (administrador: todo; doctor: su agenda y su horario)")
        .RequireAuthorization();
    }
}
