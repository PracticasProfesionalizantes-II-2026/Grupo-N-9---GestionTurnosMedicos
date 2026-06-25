using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Endpoints;

public static class NotificacionEndpoints
{
    public static void MapNotificacionEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /usuarios/{id}/notificaciones
        app.MapGet("/usuarios/{id:int}/notificaciones", async (
            int id,
            INotificacionLogica logica,
            bool? leida,
            string? tipo,
            int pagina = 1) =>
        {
            var (total, notificaciones) = await logica.ObtenerDeUsuario(id, leida, tipo, pagina);
            return Results.Ok(new { total, notificaciones });
        })
        .WithTags("Notificaciones")
        .WithSummary("Listar notificaciones de un usuario")
        .RequireAuthorization();

        // PATCH /notificaciones/{id}/leer
        app.MapMethods("/notificaciones/{id:int}/leer", new[] { "PATCH" }, async (
            int id,
            INotificacionLogica logica) =>
        {
            var (ok, error) = await logica.MarcarLeida(id);
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { id, leida = true });
        })
        .WithTags("Notificaciones")
        .WithSummary("Marcar notificación como leída")
        .RequireAuthorization();
    }
}
