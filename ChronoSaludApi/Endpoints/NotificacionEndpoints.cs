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
            var esStaff  = ctx.User.IsInRole("administrador");
            if (!esPropio && !esStaff)
                return Results.Forbid();

            var (total, notificaciones) = await logica.ObtenerDeUsuario(id, leida, tipo, pagina);
            return Results.Ok(new { total, notificaciones });
        })
        .WithTags("Notificaciones")
        .WithSummary("Listar notificaciones de un usuario")
        .RequireAuthorization();

        // GET /usuarios/{id}/notificaciones/no-leidas
        // Cuántas tiene sin leer, para la campana de la Web. Mismo permiso
        // que el listado: el propio usuario o un administrador.
        app.MapGet("/usuarios/{id:int}/notificaciones/no-leidas", async (
            int id,
            HttpContext ctx,
            INotificacionLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            if (idUsuario != id && !ctx.User.IsInRole("administrador"))
                return Results.Forbid();

            var noLeidas = await logica.ContarNoLeidas(id);
            return Results.Ok(new { noLeidas });
        })
        .WithTags("Notificaciones")
        .WithSummary("Contar las notificaciones sin leer de un usuario")
        .RequireAuthorization();

        // PATCH /notificaciones/leer-todas
        // Marca como leídas todas las del usuario del token.
        app.MapMethods("/notificaciones/leer-todas", new[] { "PATCH" }, async (
            HttpContext ctx,
            INotificacionLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var marcadas = await logica.MarcarTodasLeidas(idUsuario);
            return Results.Ok(new { marcadas });
        })
        .WithTags("Notificaciones")
        .WithSummary("Marcar todas las notificaciones propias como leídas")
        .RequireAuthorization();

        // PATCH /notificaciones/{id}/leer
        app.MapMethods("/notificaciones/{id:int}/leer", new[] { "PATCH" }, async (
            int id,
            HttpContext ctx,
            INotificacionLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var (ok, error, prohibido) = await logica.MarcarLeida(id, idUsuario);
            if (prohibido) return Results.Forbid();
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
