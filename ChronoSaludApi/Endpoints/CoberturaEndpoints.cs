using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class CoberturaEndpoints
{
    public static void MapCoberturaEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /coberturas
        app.MapGet("/coberturas", async (ICoberturaLogica logica, string? nombre) =>
        {
            var coberturas = await logica.ObtenerTodas(nombre);
            return Results.Ok(new { coberturas });
        })
        .WithTags("Coberturas")
        .WithSummary("Listar coberturas")
        .RequireAuthorization();

        // Rutas anidadas bajo /pacientes/{id}/coberturas
        var grupo = app.MapGroup("/pacientes/{id:int}/coberturas")
            .WithTags("Coberturas")
            .RequireAuthorization();

        // GET /pacientes/{id}/coberturas
        grupo.MapGet("/", async (int id, HttpContext ctx, ICoberturaLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (coberturas, error, prohibido) = await logica.ObtenerDePaciente(id, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (error != null) return Results.NotFound(new { error });

            return Results.Ok(new { coberturas });
        })
        .WithSummary("Listar coberturas de un paciente");

        // POST /pacientes/{id}/coberturas
        grupo.MapPost("/", async (int id, AsociarCoberturaDto dto, HttpContext ctx, ICoberturaLogica logica) =>
        {
            if (dto.IdCobertura == 0 || string.IsNullOrEmpty(dto.IdAfiliado))
                return Results.BadRequest(new { error = "id_cobertura e id_afiliado son requeridos." });

            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            // Cambiar la cobertura de un paciente es administrativo, no clínico:
            // acá el doctor no cuenta como staff, sólo administrador/secretario.
            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsStaff = ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (ok, error, sinPerfil, prohibido) = await logica.AsociarAPaciente(id, dto, idUsuario, callerEsPaciente, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (sinPerfil) return Results.NotFound(new { error });
            if (!ok)
            {
                if (error!.Contains("ya está asociada")) return Results.Conflict(new { error });
                if (error.Contains("no encontrada"))     return Results.NotFound(new { error });
                return Results.BadRequest(new { error });
            }
            return Results.Created($"/pacientes/{id}/coberturas", new { mensaje = "Cobertura asociada correctamente." });
        })
        .WithSummary("Asociar cobertura a paciente");

        // PUT /pacientes/{id}/coberturas
        grupo.MapPut("/", async (int id, AsociarCoberturaDto dto, HttpContext ctx, ICoberturaLogica logica) =>
        {
            if (dto.IdCobertura == 0 || string.IsNullOrEmpty(dto.IdAfiliado))
                return Results.BadRequest(new { error = "id_cobertura e id_afiliado son requeridos." });

            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsStaff = ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (ok, error, sinPerfil, prohibido) = await logica.ActualizarDePaciente(id, dto, idUsuario, callerEsPaciente, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (sinPerfil) return Results.NotFound(new { error });
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Cobertura modificada correctamente." });
        })
        .WithSummary("Modificar cobertura de paciente");

        // DELETE /pacientes/{id}/coberturas/{cobertura_id}
        grupo.MapDelete("/{cobertura_id:int}", async (int id, int cobertura_id, HttpContext ctx, ICoberturaLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsStaff = ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (ok, error, sinPerfil, prohibido) = await logica.DesvincularDePaciente(id, cobertura_id, idUsuario, callerEsPaciente, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (sinPerfil) return Results.NotFound(new { error });
            if (!ok)
                return error!.Contains("no encontrada")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.NoContent();
        })
        .WithSummary("Desvincular cobertura de paciente");
    }
}
