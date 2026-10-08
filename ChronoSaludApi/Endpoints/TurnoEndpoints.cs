using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

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
            string? estados,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            string? orden,
            string? dir,
            int pagina = 1,
            int limite = 20) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsDoctor   = ctx.User.IsInRole("doctor");

            // Una página nunca trae más de 100 turnos.
            pagina = Math.Max(pagina, 1);
            limite = Math.Clamp(limite, 1, 100);

            var filtro = new FiltroTurnos
            {
                PacienteId = paciente_id,
                DoctorId   = doctor_id,
                Estados    = LeerEstados(estado, estados),
                Desde      = fecha_desde,
                Hasta      = fecha_hasta
            };
            var descendente = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

            var (total, turnos, conteos, error) = await logica.ObtenerTodos(
                filtro, LeerOrden(orden), descendente, pagina, limite,
                idUsuario, callerEsPaciente, callerEsDoctor);

            if (error != null) return Results.NotFound(new { error });
            return Results.Ok(new { total, turnos, conteos });
        })
        .WithSummary("Listar turnos con filtros, orden y página");

        // GET /turnos/{id}
        grupo.MapGet("/{id:int}", async (int id, HttpContext ctx, ITurnoLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            var callerEsPaciente = ctx.User.IsInRole("paciente");
            var callerEsDoctor   = ctx.User.IsInRole("doctor");
            var callerEsStaff    = ctx.User.IsInRole("administrador");

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
        .RequireAuthorization(p => p.RequireRole("doctor", "administrador"));

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

    /// <summary>
    /// Junta "estado" (uno solo, como siempre) y "estados" (varios separados
    /// por coma, por ejemplo "pendiente,confirmado") en una sola lista.
    /// </summary>
    public static List<string> LeerEstados(string? estado, string? estados)
    {
        var lista = new List<string>();

        if (!string.IsNullOrWhiteSpace(estado))
            lista.Add(estado.Trim().ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(estados))
        {
            foreach (var parte in estados.Split(','))
            {
                var limpio = parte.Trim().ToLowerInvariant();
                if (limpio != "" && !lista.Contains(limpio))
                    lista.Add(limpio);
            }
        }

        return lista;
    }

    /// <summary>
    /// Las columnas por las que se puede ordenar. Cualquier otro valor, o
    /// ninguno, ordena por fecha y hora.
    /// </summary>
    public static string LeerOrden(string? orden)
    {
        var limpio = (orden ?? "").Trim().ToLowerInvariant();

        if (limpio == "paciente" || limpio == "estado")
            return limpio;

        return "fecha";
    }
}
