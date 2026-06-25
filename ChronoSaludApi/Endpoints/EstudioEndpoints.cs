using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Endpoints;

public static class EstudioEndpoints
{
    public static void MapEstudioEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /pacientes/{id}/estudios
        app.MapGet("/pacientes/{id:int}/estudios", async (
            int id,
            IEstudioLogica logica,
            string? tipo,
            string? estado,
            DateTime? fecha_desde) =>
        {
            var estudios = await logica.ObtenerDePaciente(id, tipo, estado, fecha_desde);
            return Results.Ok(new { estudios });
        })
        .WithTags("Estudios")
        .WithSummary("Listar estudios del paciente")
        .RequireAuthorization();

        var grupo = app.MapGroup("/estudios").WithTags("Estudios").RequireAuthorization();

        // POST /estudios
        grupo.MapPost("/", async (EstudioCreateDto dto, IEstudioLogica logica) =>
        {
            if (dto.IdPaciente == 0 || string.IsNullOrEmpty(dto.Tipo) || string.IsNullOrEmpty(dto.Descripcion))
                return Results.BadRequest(new { error = "Datos inválidos o incompletos." });

            var (id, error) = await logica.Crear(dto);
            if (error != null)
                return error.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/estudios/{id}", new { id_estudio = id, dto.Tipo, estado = "pendiente" });
        })
        .WithSummary("Solicitar estudio médico")
        .RequireAuthorization(p => p.RequireRole("doctor"));

        // PUT /estudios/{id}/resultados
        grupo.MapPut("/{id:int}/resultados", async (int id, EstudioResultadoDto dto, IEstudioLogica logica) =>
        {
            if (string.IsNullOrEmpty(dto.Resultado) || string.IsNullOrEmpty(dto.Estado))
                return Results.BadRequest(new { error = "Resultado, estado y fecha_resultado son requeridos." });

            var (ok, error) = await logica.CargarResultado(id, dto);
            if (!ok)
                return error!.Contains("no encontrado")
                    ? Results.NotFound(new { error })
                    : Results.BadRequest(new { error });

            return Results.Ok(new { mensaje = "Resultado cargado correctamente." });
        })
        .WithSummary("Cargar resultado de estudio")
        .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // GET /estudios/{id}/descargar  (placeholder)
        grupo.MapGet("/{id:int}/descargar", async (int id, IEstudioLogica logica) =>
        {
            var estudio = await logica.ObtenerPorId(id);
            if (estudio == null)
                return Results.NotFound(new { error = "Estudio no encontrado." });

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
    }
}
