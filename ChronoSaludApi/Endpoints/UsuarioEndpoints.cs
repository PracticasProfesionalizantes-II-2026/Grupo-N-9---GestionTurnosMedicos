using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/usuarios").WithTags("Usuarios");

        // POST /usuarios/registro
        grupo.MapPost("/registro", async (UsuarioRegistroDto dto, IUsuarioLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Nombre) || string.IsNullOrEmpty(dto.Apellido) ||
                string.IsNullOrEmpty(dto.Email)  || string.IsNullOrEmpty(dto.Contrasena) ||
                string.IsNullOrEmpty(dto.Rol))
                return Results.BadRequest(new { error = "Datos inválidos o faltantes." });

            if (dto.Contrasena.Length < 8)
                return Results.BadRequest(new { error = "La contraseña debe tener al menos 8 caracteres." });

            var (resultado, error) = await logica.Registrar(dto);
            if (error != null)
                return Results.Conflict(new { error });

            return Results.Created($"/usuarios/{resultado!.IdUsuario}", resultado);
        })
        .WithSummary("Registrar nuevo usuario")
        .AllowAnonymous();

        // POST /usuarios/login
        grupo.MapPost("/login", async (UsuarioLoginDto dto, IUsuarioLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Contrasena))
                return Results.BadRequest(new { error = "Email y contraseña son requeridos." });

            var (resultado, error) = await logica.Login(dto);
            if (error != null)
                return Results.Unauthorized();

            return Results.Ok(resultado);
        })
        .WithSummary("Autenticar usuario")
        .AllowAnonymous();

        // GET /usuarios/{id}
        grupo.MapGet("/{id:int}", async (int id, IUsuarioLogica logica) =>
        {
            var usuario = await logica.ObtenerPorId(id);
            return usuario == null
                ? Results.NotFound(new { error = "Usuario no encontrado." })
                : Results.Ok(usuario);
        })
        .WithSummary("Obtener usuario por ID")
        .RequireAuthorization();

        // PUT /usuarios/{id}
        grupo.MapPut("/{id:int}", async (int id, UsuarioUpdateDto dto, IUsuarioLogica logica) =>
        {
            var (ok, error) = await logica.Actualizar(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Usuario actualizado correctamente." });
        })
        .WithSummary("Actualizar usuario")
        .RequireAuthorization();

        // DELETE /usuarios/{id}
        grupo.MapDelete("/{id:int}", async (int id, IUsuarioLogica logica) =>
        {
            var (ok, error) = await logica.EliminarLogico(id);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.Forbid();

            return Results.NoContent();
        })
        .WithSummary("Baja lógica de usuario")
        .RequireAuthorization(policy => policy.RequireRole("administrador"));
    }
}
