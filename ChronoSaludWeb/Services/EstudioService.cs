using System.Text.Json.Serialization;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Un estudio médico. Espeja EstudioDto de la API. Doctor es quien lo pidió
/// (el del turno vinculado); null si el estudio no tiene turno.
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
    Profesional? Doctor);

/// <summary>Respuesta de GET /pacientes/{id}/estudios: { estudios }.</summary>
public record EstudiosDePaciente(IReadOnlyList<Estudio> Estudios);

/// <summary>Lo que se manda en POST /estudios. La fecha la pone la API (hoy).</summary>
public record EstudioNuevo(int IdPaciente, int? IdTurno, string Tipo, string Descripcion);

/// <summary>Respuesta de POST /estudios.</summary>
public record EstudioCreado(
    [property: JsonPropertyName("id_estudio")] int IdEstudio);

/// <summary>
/// Lo que se manda en PUT /estudios/{id}/resultados. La fecha la pone la API
/// (ahora); el estado siempre es "entregado": el paciente ya lo puede ver.
/// </summary>
public record EstudioResultado(string Resultado, string? ArchivoUrl, string Estado = "entregado");

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
}
