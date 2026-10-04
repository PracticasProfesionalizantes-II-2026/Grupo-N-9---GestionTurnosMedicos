using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Endpoints;

public static class UsuarioFotoEndpoints
{
    // Tope duro del pedido completo: algo más que los 2 MB de la foto, para
    // que un archivo apenas pasado llegue a la validación y reciba su mensaje.
    private const long TamanoMaximoPedido = 5 * 1024 * 1024;

    public static void MapUsuarioFotoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/usuarios").WithTags("Usuarios").RequireAuthorization();

        // PUT /usuarios/{id}/foto
        grupo.MapPut("/{id:int}/foto", async (int id, IFormFile? archivo, IUsuarioFotoLogica logica) =>
        {
            if (archivo == null || archivo.Length == 0)
                return Results.BadRequest(new { error = "Falta el archivo de la foto (campo \"archivo\")." });

            if (archivo.Length > UsuarioFotoLogica.TamanoMaximo)
                return Results.BadRequest(new { error = "La foto no puede superar los 2 MB." });

            // Solo se usan los bytes: el nombre y el tipo que manda el cliente se descartan.
            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);

            var (ok, error) = await logica.Guardar(id, memoria.ToArray());
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Foto actualizada correctamente." });
        })
        .WithSummary("Subir o reemplazar la foto de un usuario (JPG, PNG o WebP, hasta 2 MB)")
        .WithMetadata(new RequestSizeLimitAttribute(TamanoMaximoPedido))
        .DisableAntiforgery() // la autenticación es por JWT, no por cookie
        .RequireAuthorization(policy => policy.RequireRole("administrador"));

        // GET /usuarios/{id}/foto
        grupo.MapGet("/{id:int}/foto", async (int id, HttpContext ctx, IUsuarioFotoLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            // Quién puede verla depende del rol del dueño. Sin foto y sin
            // permiso contestan lo mismo, para no revelar cuál de los dos es.
            var rol = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var fecha = await logica.ObtenerAcceso(id, idUsuario, rol);
            if (fecha == null)
                return Results.NotFound(new { error = "Foto no encontrada." });

            // La foto es privada: se puede guardar en el navegador pero hay que
            // revalidarla siempre. Si no cambió, se contesta 304 sin el contenido.
            var etag = $"\"{fecha.Value.Ticks}\"";
            ctx.Response.Headers.ETag = etag;
            ctx.Response.Headers.CacheControl = "private, no-cache";
            ctx.Response.Headers.XContentTypeOptions = "nosniff";

            if (ctx.Request.Headers.IfNoneMatch == etag)
                return Results.StatusCode(StatusCodes.Status304NotModified);

            var foto = await logica.Obtener(id);
            return foto == null
                ? Results.NotFound(new { error = "Foto no encontrada." })
                : Results.File(foto.Contenido, foto.TipoContenido);
        })
        .WithSummary("Obtener la foto de un usuario (la de un doctor o administrador, cualquiera; la de un paciente, solo administrador, doctor o el propio paciente)");

        // DELETE /usuarios/{id}/foto
        grupo.MapDelete("/{id:int}/foto", async (int id, IUsuarioFotoLogica logica) =>
        {
            var (ok, error) = await logica.Eliminar(id);
            if (!ok)
                return Results.NotFound(new { error });

            return Results.NoContent();
        })
        .WithSummary("Quitar la foto de un usuario")
        .RequireAuthorization(policy => policy.RequireRole("administrador"));

        // GET /usuarios/fotos?pacientes=1,2&doctores=3&turnos=7,8
        grupo.MapGet("/fotos", async (
            HttpContext ctx,
            IUsuarioFotoLogica logica,
            string? pacientes,
            string? doctores,
            string? turnos) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            if (!LeerIds(pacientes, out var idsPaciente) ||
                !LeerIds(doctores, out var idsDoctor) ||
                !LeerIds(turnos, out var idsTurno))
                return Results.BadRequest(new
                {
                    error = $"Cada lista acepta hasta {MaximoIdsPorLista} ids numéricos separados por coma."
                });

            var rol = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var disponibles = await logica.ConsultarDisponibles(idsPaciente, idsDoctor, idsTurno, idUsuario, rol);
            return Results.Ok(disponibles);
        })
        .WithSummary("Consultar qué pacientes, doctores o turnos (por su paciente) tienen una foto visible para quien pregunta");
    }

    private const int MaximoIdsPorLista = 100;

    // "1,2,3" -> [1, 2, 3], sin repetidos. Vacío o ausente es una lista vacía.
    // False si algún valor no es un número positivo o si hay más de 100.
    private static bool LeerIds(string? texto, out List<int> ids)
    {
        ids = new List<int>();
        if (string.IsNullOrWhiteSpace(texto)) return true;

        foreach (var parte in texto.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(parte, out var id) || id <= 0) return false;
            if (!ids.Contains(id)) ids.Add(id);
        }

        return ids.Count <= MaximoIdsPorLista;
    }
}
