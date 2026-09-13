using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class PacienteEndpoints
{
    public static void MapPacienteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pacientes").WithTags("Pacientes").RequireAuthorization();

        // GET /pacientes
        grupo.MapGet("/", async (
            IPacienteLogica logica,
            string? nombre,
            string? dni,
            int? cobertura_id,
            int pagina = 1,
            int limite = 20) =>
        {
            var (total, pacientes) = await logica.ObtenerTodos(nombre, dni, cobertura_id, pagina, limite);
            return Results.Ok(new { total, pagina, pacientes });
        })
        .WithSummary("Listar pacientes")
        .RequireAuthorization(p => p.RequireRole("doctor", "administrador"));

        // GET /pacientes/me
        grupo.MapGet("/me", async (HttpContext ctx, IPacienteLogica logica) =>
        {
            var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var idUsuario))
                return Results.Unauthorized();

            var paciente = await logica.ObtenerPorIdUsuario(idUsuario);
            return paciente == null
                ? Results.NotFound(new { error = "El usuario autenticado no tiene perfil de paciente." })
                : Results.Ok(paciente);
        })
        .WithSummary("Obtener el perfil de paciente del usuario autenticado");

        // GET /pacientes/{id}
        grupo.MapGet("/{id:int}", async (int id, HttpContext ctx, IPacienteLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (paciente, error, prohibido) = await logica.ObtenerPorId(id, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();

            return paciente == null
                ? Results.NotFound(new { error })
                : Results.Ok(paciente);
        })
        .WithSummary("Obtener perfil de paciente");

        // PUT /pacientes/{id}
        grupo.MapPut("/{id:int}", async (int id, PacienteUpdateDto dto, HttpContext ctx, IPacienteLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsStaff = ctx.User.IsInRole("doctor") || ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (ok, error, prohibido, conflictoDni) = await logica.Actualizar(id, dto, idUsuario, callerEsStaff);
            if (prohibido) return Results.Forbid();
            if (conflictoDni) return Results.Conflict(new { error });
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Datos del paciente actualizados correctamente." });
        })
        .WithSummary("Actualizar datos clínicos del paciente");
    }
}
