using System.Security.Claims;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/usuarios").WithTags("Usuarios");

        // POST /usuarios/registro
        // Es anónimo para que un paciente se registre solo. Un rol distinto de
        // paciente pide el token de un administrador (ver UsuarioLogica.Registrar).
        grupo.MapPost("/registro", async (UsuarioRegistroDto dto, HttpContext ctx, IUsuarioLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Nombre) || string.IsNullOrEmpty(dto.Apellido) ||
                string.IsNullOrEmpty(dto.Email)  || string.IsNullOrEmpty(dto.Contrasena) ||
                string.IsNullOrEmpty(dto.Rol))
                return Results.BadRequest(new { error = "Datos inválidos o faltantes." });

            if (dto.Contrasena.Length < 8)
                return Results.BadRequest(new { error = "La contraseña debe tener al menos 8 caracteres." });

            var (resultado, error, sinPermiso) = await logica.Registrar(dto, ctx.User.IsInRole("administrador"));
            if (sinPermiso)
                return ctx.User.Identity?.IsAuthenticated == true
                    ? Results.Forbid()
                    : Results.Unauthorized();

            if (error != null)
                return Results.Conflict(new { error });

            return Results.Created($"/usuarios/{resultado!.IdUsuario}", resultado);
        })
        .WithSummary("Registrar nuevo usuario")
        .AllowAnonymous();

        // POST /usuarios/login
        grupo.MapPost("/login", async (UsuarioLoginDto dto, IUsuarioLogica logica, LimiteIntentos limite) =>
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Contrasena))
                return Results.BadRequest(new { error = "Email y contraseña son requeridos." });

            // Tope de intentos por email: frena a quien prueba contraseñas
            // contra una misma cuenta.
            if (!limite.PermiteLogin(dto.Email))
                return Results.Json(new { error = LimiteIntentos.Mensaje }, statusCode: StatusCodes.Status429TooManyRequests);

            var (resultado, error) = await logica.Login(dto);
            if (error != null)
                return Results.Unauthorized();

            return Results.Ok(resultado);
        })
        .WithSummary("Autenticar usuario")
        .AllowAnonymous();

        // POST /usuarios/me/contrasena
        // La contraseña de quien está logueado: el usuario sale del token,
        // nunca de la URL. Pide la actual.
        grupo.MapPost("/me/contrasena", async (CambioContrasenaDto dto, HttpContext ctx, IUsuarioLogica logica, LimiteIntentos limite) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            if (!limite.PermiteCambioDeContrasena(idUsuario))
                return Results.Json(new { error = LimiteIntentos.Mensaje }, statusCode: StatusCodes.Status429TooManyRequests);

            var (ok, error) = await logica.CambiarContrasena(idUsuario, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Contraseña actualizada." });
        })
        .WithSummary("Cambiar la contraseña propia (pide la actual)")
        .RequireAuthorization();

        // GET /usuarios/{id}
        grupo.MapGet("/{id:int}", async (int id, HttpContext ctx, IUsuarioLogica logica) =>
        {
            // Solo el propio usuario o un administrador pueden leer la cuenta.
            // Va antes de buscarla: un 403 no revela si el id existe.
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            if (idUsuario != id && !ctx.User.IsInRole("administrador"))
                return Results.Forbid();

            var usuario = await logica.ObtenerPorId(id);
            return usuario == null
                ? Results.NotFound(new { error = "Usuario no encontrado." })
                : Results.Ok(usuario);
        })
        .WithSummary("Obtener usuario por ID")
        .RequireAuthorization();

        // PUT /usuarios/{id}
        grupo.MapPut("/{id:int}", async (int id, UsuarioUpdateDto dto, HttpContext ctx, IUsuarioLogica logica) =>
        {
            // Solo el propio usuario o un administrador pueden modificar la cuenta.
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idUsuario))
                return Results.Unauthorized();

            if (idUsuario != id && !ctx.User.IsInRole("administrador"))
                return Results.Forbid();

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
        // Baja lógica. Quién la pide sale del token: nadie se da de baja a sí
        // mismo. Los frenos (último administrador, turnos en pie) van con 409.
        grupo.MapDelete("/{id:int}", async (int id, HttpContext ctx, IUsuarioLogica logica) =>
        {
            var idClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var idSolicitante))
                return Results.Unauthorized();

            var (ok, error) = await logica.DarDeBaja(id, idSolicitante);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.Conflict(new { error });

            return Results.NoContent();
        })
        .WithSummary("Baja lógica de usuario (y de su perfil de doctor)")
        .RequireAuthorization(policy => policy.RequireRole("administrador"));

        // POST /usuarios/{id}/reactivar
        grupo.MapPost("/{id:int}/reactivar", async (int id, IUsuarioLogica logica) =>
        {
            var (ok, error) = await logica.Reactivar(id);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.Conflict(new { error });

            return Results.Ok(new { mensaje = "Cuenta reactivada." });
        })
        .WithSummary("Reactivar una cuenta dada de baja (y su perfil de doctor)")
        .RequireAuthorization(policy => policy.RequireRole("administrador"));

        // GET /usuarios
        grupo.MapGet("/", async (
            IUsuarioLogica logica,
            string? buscar,
            string? rol,
            int pagina = 1,
            int limite = 20,
            bool bajas = false) =>
        {
            pagina = Math.Max(pagina, 1);
            limite = Math.Clamp(limite, 1, 100);

            var (total, usuarios, error) = await logica.Buscar(buscar, rol, pagina, limite, bajas);
            if (error != null)
                return Results.BadRequest(new { error });

            return Results.Ok(new { total, pagina, usuarios });
        })
        .WithSummary("Buscar usuarios activos (o, con bajas=true, los dados de baja) por nombre, apellido o email")
        .RequireAuthorization(policy => policy.RequireRole("administrador"));
    }
}
