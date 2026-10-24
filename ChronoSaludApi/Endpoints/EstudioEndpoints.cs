using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class EstudioEndpoints
{
    // Tope del pedido completo: algo más que los 5 MB del PDF, para que un
    // archivo apenas pasado llegue a la validación y reciba su mensaje.
    private const long TamanoMaximoPedido = 8 * 1024 * 1024;

    public static void MapEstudioEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /pacientes/{id}/estudios
        app.MapGet("/pacientes/{id:int}/estudios", async (
            int id,
            HttpContext ctx,
            IEstudioLogica logica,
            string? tipo,
            string? estado,
            DateTime? fecha_desde) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador");

            var (estudios, error, prohibido) = await logica.ObtenerDePaciente(id, tipo, estado, fecha_desde, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (error != null) return Results.NotFound(new { error });

            return Results.Ok(new { estudios });
        })
        .WithTags("Estudios")
        .WithSummary("Listar estudios del paciente")
        .RequireAuthorization();

        var grupo = app.MapGroup("/estudios").WithTags("Estudios").RequireAuthorization();

        // GET /estudios/{id}
        // Un estudio: el staff ve cualquiera y el paciente solo los suyos.
        grupo.MapGet("/{id:int}", async (int id, HttpContext ctx, IEstudioLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador");

            var (estudio, error, prohibido) = await logica.ObtenerPorId(id, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (estudio == null) return Results.NotFound(new { error });

            return Results.Ok(estudio);
        })
        .WithSummary("Ver un estudio");

        // POST /estudios
        grupo.MapPost("/", async (EstudioCreateDto dto, HttpContext ctx, IEstudioLogica logica) =>
        {
            if (dto.IdPaciente == 0 || string.IsNullOrEmpty(dto.Tipo) || string.IsNullOrWhiteSpace(dto.Descripcion))
                return Results.BadRequest(new { error = "Datos inválidos o incompletos." });

            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var (id, error) = await logica.Crear(dto, idUsuario);
            if (error != null)
                return error.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/estudios/{id}", new { id_estudio = id, dto.Tipo, estado = "pendiente" });
        })
        .WithSummary("Solicitar estudio médico")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // PATCH /estudios/vistos
        // El paciente vio sus resultados: los "validado" pasan a "entregado".
        grupo.MapMethods("/vistos", new[] { "PATCH" }, async (HttpContext ctx, IEstudioLogica logica) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            var (marcados, error) = await logica.MarcarVistos(solicitante.IdUsuario);
            if (error != null) return Results.NotFound(new { error });

            return Results.Ok(new { marcados });
        })
        .WithSummary("Marcar como vistos los resultados propios (paciente)")
        .RequireAuthorization(p => p.RequireRole("paciente"));

        // PUT /estudios/{id}/archivo
        // El PDF del resultado, hasta 5 MB. Reemplaza al que hubiera.
        grupo.MapPut("/{id:int}/archivo", async (int id, IFormFile? archivo, IEstudioLogica logica) =>
        {
            if (archivo == null || archivo.Length == 0)
                return Results.BadRequest(new { error = "Falta el PDF (campo \"archivo\")." });

            if (archivo.Length > EstudioLogica.TamanoMaximoPdf)
                return Results.BadRequest(new { error = "El PDF no puede superar los 5 MB." });

            // Solo se usan los bytes y el nombre: el tipo que manda el cliente se descarta.
            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);

            var (ok, error) = await logica.GuardarArchivo(id, memoria.ToArray(), archivo.FileName);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "PDF guardado correctamente." });
        })
        .WithSummary("Subir o reemplazar el PDF del resultado (hasta 5 MB)")
        .WithMetadata(new RequestSizeLimitAttribute(TamanoMaximoPedido))
        .DisableAntiforgery() // la autenticación es por JWT, no por cookie
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // DELETE /estudios/{id}/archivo
        grupo.MapDelete("/{id:int}/archivo", async (int id, IEstudioLogica logica) =>
        {
            var (ok, error) = await logica.QuitarArchivo(id);
            if (!ok) return Results.NotFound(new { error });

            return Results.NoContent();
        })
        .WithSummary("Quitar el PDF del resultado")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // PUT /estudios/{id}/resultados
        grupo.MapPut("/{id:int}/resultados", async (int id, EstudioResultadoDto dto, HttpContext ctx, IEstudioLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Resultado) || string.IsNullOrEmpty(dto.Estado))
                return Results.BadRequest(new { error = "Resultado, estado y fecha_resultado son requeridos." });

            var (ok, error) = await logica.CargarResultado(id, dto, Solicitante.Desde(ctx.User));
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Resultado cargado correctamente." });
        })
        .WithSummary("Cargar resultado de estudio")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // GET /estudios/{id}/descargar  (placeholder)
        grupo.MapGet("/{id:int}/descargar", async (int id, HttpContext ctx, IEstudioLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador");

            var (estudio, error, prohibido) = await logica.ObtenerPorId(id, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (estudio == null)
                return Results.NotFound(new { error });

            if (string.IsNullOrEmpty(estudio.ArchivoUrl))
                return Results.NotFound(new { error = "El estudio aún no tiene resultado disponible." });

            // Placeholder: En producción redirigir a ArchivoUrl o servir el binario
            return Results.Ok(new
            {
                mensaje    = "Endpoint listo. Servir el archivo desde ArchivoUrl en producción.",
                archivo_url = estudio.ArchivoUrl
            });
        })
        .WithSummary("Descargar resultado de estudio");

        // GET /estudios/{id}/archivo
        // El PDF guardado o, si no hay, el enlace del laboratorio. Si lo baja
        // el paciente, el estudio pasa a "entregado".
        grupo.MapGet("/{id:int}/archivo", async (int id, HttpContext ctx, IEstudioLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador");

            var (archivo, enlace, error, prohibido) = await logica.ObtenerArchivo(id, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();

            if (archivo != null)
                return Results.File(archivo.Contenido, "application/pdf", archivo.Nombre);

            if (enlace != null)
                return Results.Redirect(enlace);

            return Results.NotFound(new { error = error ?? "Estudio no encontrado." });
        })
        .WithSummary("Descargar el PDF del resultado, o ir al enlace del laboratorio");
    }
}
