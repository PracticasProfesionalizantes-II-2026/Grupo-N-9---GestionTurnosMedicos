using System.Text.Json.Serialization;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Un estudio médico. Espeja EstudioDto de la API. Doctor es quien lo pidió
/// (el del turno vinculado); null si el estudio no tiene turno. TienePdf y
/// NombrePdf dicen si el resultado tiene un PDF subido a la app (paso 23).
/// </summary>
public record Estudio(
    int IdEstudio,
    string Tipo,
    string Estado,
    DateTime FechaSolicitud,
    int? IdTurno,
    string? Resultado,
    string? ArchivoUrl,
    DateTime? FechaResultado,
    string Descripcion,
    Profesional? Doctor,
    bool TienePdf = false,
    string? NombrePdf = null);

/// <summary>Respuesta de GET /pacientes/{id}/estudios: { estudios }.</summary>
public record EstudiosDePaciente(IReadOnlyList<Estudio> Estudios);

/// <summary>Lo que se manda en POST /estudios. La fecha la pone la API (hoy).</summary>
public record EstudioNuevo(int IdPaciente, int? IdTurno, string Tipo, string Descripcion);

/// <summary>Respuesta de POST /estudios.</summary>
public record EstudioCreado(
    [property: JsonPropertyName("id_estudio")] int IdEstudio);

/// <summary>
/// Lo que se manda en PUT /estudios/{id}/resultados. La fecha la pone la API
/// (ahora). El estado es "validado": el resultado está cargado y el paciente
/// todavía no lo vio. Pasa a "entregado" solo, cuando lo ve (paso 23).
/// </summary>
public record EstudioResultado(string Resultado, string? ArchivoUrl, string Estado = "validado");

/// <summary>Respuesta de PATCH /estudios/vistos.</summary>
public record EstudiosVistos(int Marcados);

public class EstudioService
{
    private readonly ApiClient _api;

    public EstudioService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /pacientes/{id}/estudios, del más nuevo al más viejo. El paciente
    /// solo puede pedir los suyos; el personal, los de cualquiera.
    /// </summary>
    public async Task<IReadOnlyList<Estudio>> ObtenerDePacienteAsync(int idPaciente)
    {
        var respuesta = await _api.GetAsync<EstudiosDePaciente>($"/pacientes/{idPaciente}/estudios");
        return respuesta?.Estudios ?? Array.Empty<Estudio>();
    }

    /// <summary>
    /// GET /estudios/{id}. Null si no existe o si no es de quien pregunta
    /// (la API contesta 404 o 403): para la Web, en los dos casos "no existe".
    /// </summary>
    public async Task<Estudio?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _api.GetAsync<Estudio>($"/estudios/{id}");
        }
        catch (ApiException error) when (error.Status is StatusCodes.Status404NotFound or StatusCodes.Status403Forbidden)
        {
            return null;
        }
    }

    /// <summary>POST /estudios. Solo un doctor; queda a su nombre por el turno.</summary>
    public Task<EstudioCreado?> CrearAsync(EstudioNuevo estudio)
        => _api.PostAsync<EstudioCreado>("/estudios", estudio);

    /// <summary>PUT /estudios/{id}/resultados. La API le avisa al paciente.</summary>
    public Task CargarResultadoAsync(int id, EstudioResultado resultado)
        => _api.PutAsync($"/estudios/{id}/resultados", resultado);

    /// <summary>PUT /estudios/{id}/archivo: sube o reemplaza el PDF del resultado.</summary>
    public Task SubirPdfAsync(int id, byte[] contenido, string nombre)
        => _api.PutArchivoAsync($"/estudios/{id}/archivo", "archivo", contenido, "application/pdf", nombre);

    /// <summary>DELETE /estudios/{id}/archivo.</summary>
    public Task QuitarPdfAsync(int id)
        => _api.DeleteAsync($"/estudios/{id}/archivo");

    /// <summary>
    /// GET /estudios/{id}/archivo: el PDF guardado. Null si no tiene, o si no
    /// es de quien pregunta. Si lo baja el paciente, la API lo marca como visto.
    /// </summary>
    public async Task<ArchivoApi?> DescargarPdfAsync(int id)
    {
        try
        {
            return await _api.GetArchivoAsync($"/estudios/{id}/archivo");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status403Forbidden)
        {
            return null;
        }
    }

    /// <summary>PATCH /estudios/vistos: el paciente ya vio sus resultados.</summary>
    public async Task<int> MarcarVistosAsync()
        => (await _api.PatchAsync<EstudiosVistos>("/estudios/vistos", null))?.Marcados ?? 0;
}
