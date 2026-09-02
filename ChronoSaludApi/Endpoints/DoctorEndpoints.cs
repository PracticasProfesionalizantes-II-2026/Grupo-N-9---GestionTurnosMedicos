using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class DoctorEndpoints
{
    public static void MapDoctorEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/doctores").WithTags("Doctores").RequireAuthorization();

        // GET /doctores
        grupo.MapGet("/", async (
            IDoctorLogica logica,
            string? especialidad,
            int? cobertura_id,
            int pagina = 1,
            int limite = 20) =>
        {
            var (total, doctores) = await logica.ObtenerTodos(especialidad, cobertura_id, pagina, limite);
            return Results.Ok(new { total, doctores });
        })
        .WithSummary("Listar doctores");

        // GET /doctores/me
        grupo.MapGet("/me", async (HttpContext ctx, IDoctorLogica logica) =>
        {
            var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var idUsuario))
                return Results.Unauthorized();

            var doctor = await logica.ObtenerPorIdUsuario(idUsuario);
            return doctor == null
                ? Results.NotFound(new { error = "El usuario autenticado no tiene perfil de doctor." })
                : Results.Ok(doctor);
        })
        .WithSummary("Obtener el perfil de doctor del usuario autenticado");

        // GET /doctores/{id}
        grupo.MapGet("/{id:int}", async (int id, IDoctorLogica logica) =>
        {
            var doctor = await logica.ObtenerPorId(id);
            return doctor == null
                ? Results.NotFound(new { error = "Doctor no encontrado." })
                : Results.Ok(doctor);
        })
        .WithSummary("Obtener perfil de doctor");

        // POST /doctores
        grupo.MapPost("/", async (DoctorCreateDto dto, IDoctorLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Especialidad) || string.IsNullOrEmpty(dto.Matricula))
                return Results.BadRequest(new { error = "Especialidad y matrícula son requeridas." });

            var (id, error) = await logica.Crear(dto);
            if (error != null)
                return error.Contains("matrícula")
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/doctores/{id}", new { id_doctor = id, dto.Especialidad, dto.Matricula });
        })
        .WithSummary("Crear doctor")
        .RequireAuthorization(p => p.RequireRole("administrador"));

        // PUT /doctores/{id}
        grupo.MapPut("/{id:int}", async (int id, DoctorUpdateDto dto, IDoctorLogica logica) =>
        {
            var (ok, error) = await logica.Actualizar(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Doctor actualizado correctamente." });
        })
        .WithSummary("Actualizar doctor")
        .RequireAuthorization(p => p.RequireRole("administrador"));

        // DELETE /doctores/{id}
        grupo.MapDelete("/{id:int}", async (int id, IDoctorLogica logica) =>
        {
            var (ok, error) = await logica.EliminarLogico(id);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.NoContent();
        })
        .WithSummary("Baja lógica de doctor")
        .RequireAuthorization(p => p.RequireRole("administrador"));
    }
}
