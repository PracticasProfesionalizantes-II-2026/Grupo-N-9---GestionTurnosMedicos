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
            int? cobertura_id,
            int pagina = 1,
            int limite = 20) =>
        {
            var (total, pacientes) = await logica.ObtenerTodos(nombre, cobertura_id, pagina, limite);
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
        grupo.MapGet("/{id:int}", async (int id, IPacienteLogica logica) =>
        {
            var paciente = await logica.ObtenerPorId(id);
            return paciente == null
                ? Results.NotFound(new { error = "Paciente no encontrado." })
                : Results.Ok(paciente);
        })
        .WithSummary("Obtener perfil de paciente");

        // PUT /pacientes/{id}
        grupo.MapPut("/{id:int}", async (int id, PacienteUpdateDto dto, IPacienteLogica logica) =>
        {
            var (ok, error) = await logica.Actualizar(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Datos del paciente actualizados correctamente." });
        })
        .WithSummary("Actualizar datos clínicos del paciente");
    }
}
