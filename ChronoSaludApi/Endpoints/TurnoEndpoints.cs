using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class TurnoEndpoints
{
    public static void MapTurnoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/turnos").WithTags("Turnos").RequireAuthorization();

        // GET /turnos
        grupo.MapGet("/", async (
            HttpContext ctx,
            ITurnoLogica logica,
            int? paciente_id,
            int? doctor_id,
            string? estado,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int pagina = 1,
            int limite = 20) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsDoctor   = ctx.User.IsInRole("doctor");

            var (total, turnos, error) = await logica.ObtenerTodos(
                paciente_id, doctor_id, estado,
                fecha_desde, fecha_hasta, pagina, limite,
                idUsuario, callerEsPaciente, callerEsDoctor);

            if (error != null) return Results.NotFound(new { error });
            return Results.Ok(new { total, turnos });
        })
        .WithSummary("Listar turnos con filtros");

        // GET /turnos/{id}
        grupo.MapGet("/{id:int}", async (int id, HttpContext ctx, ITurnoLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsDoctor   = ctx.User.IsInRole("doctor");
            var callerEsStaff    = ctx.User.IsInRole("administrador") || ctx.User.IsInRole("secretario");

            var (turno, error) = await logica.ObtenerPorId(id, idUsuario, callerEsPaciente, callerEsDoctor, callerEsStaff);
            return turno == null
                ? Results.NotFound(new { error })
                : Results.Ok(turno);
        })
        .WithSummary("Obtener detalle de turno");

        // POST /turnos
        grupo.MapPost("/", async (TurnoCreateDto dto, HttpContext ctx, ITurnoLogica logica) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            // Al paciente no se le exige el IdPaciente: el suyo sale del token.
            if ((!solicitante.EsPaciente && dto.IdPaciente == 0) || dto.IdDoctor == 0 ||
                string.IsNullOrEmpty(dto.HoraInicio) || string.IsNullOrEmpty(dto.HoraFin))
                return Results.BadRequest(new { error = "Datos inválidos o incompletos." });

            var (id, error) = await logica.Crear(dto, solicitante);
            if (error != null)
                return error.Contains("Conflicto")
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/turnos/{id}", new
            {
                id_turno     = id,
                estado       = "pendiente",
                fecha_inicio = dto.FechaInicio,
                hora_inicio  = dto.HoraInicio
            });
        })
        .WithSummary("Crear turno");

        // PUT /turnos/{id}
        grupo.MapPut("/{id:int}", async (int id, TurnoUpdateDto dto, HttpContext ctx, ITurnoLogica logica) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            var (ok, error, sinPerfilDoctor) = await logica.Actualizar(id, dto, solicitante);
            if (!ok)
            {
                if (sinPerfilDoctor || error!.Contains("no encontrado")) return Results.NotFound(new { error });
                if (error.Contains("Conflicto"))                          return Results.Conflict(new { error });
                return Results.BadRequest(new { error });
            }
            return Results.Ok(new { mensaje = "Turno actualizado correctamente." });
        })
        .WithSummary("Modificar turno")
        .RequireAuthorization(p => p.RequireRole("doctor", "administrador", "secretario"));

        // DELETE /turnos/{id}
        grupo.MapDelete("/{id:int}", async (int id, HttpContext ctx, ITurnoLogica logica) =>
        {
            var solicitante = Solicitante.Desde(ctx.User);
            if (solicitante == null)
                return Results.Unauthorized();

            var (ok, error) = await logica.Cancelar(id, solicitante);
            if (!ok)
            {
                if (error!.Contains("no encontrado")) return Results.NotFound(new { error });
                if (error.Contains("Conflicto"))      return Results.Conflict(new { error });
                return Results.BadRequest(new { error });
            }

            return Results.NoContent();
        })
        .WithSummary("Cancelar turno");
    }
}
