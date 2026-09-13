using System.Security.Claims;
using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Endpoints;

public static class NotificacionEndpoints
{
    public static void MapNotificacionEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /usuarios/{id}/notificaciones
        app.MapGet("/usuarios/{id:int}/notificaciones", async (
            int id,
            HttpContext ctx,
            INotificacionLogica logica,
            bool? leida,
            string? tipo,
            int pagina = 1) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var esPropio = idUsuario == id;
            var esStaff  = ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");
            if (!esPropio && !esStaff)
                return Results.Forbid();

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
