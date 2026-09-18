namespace ChronoSaludWeb.Services;

/// <summary>
/// Una cobertura asociada a un paciente. Espeja PacienteCoberturaDto de la API.
/// </summary>
public record PacienteCobertura(
    int Id,
    int IdCobertura,
    string NombreCobertura,
    string IdAfiliado,
    string? Plan);

/// <summary>
/// Respuesta de GET /pacientes/{id}/coberturas: { coberturas }.
/// </summary>
public record PacienteCoberturasRespuesta(IReadOnlyList<PacienteCobertura> Coberturas);

public class CoberturaService
{
    private readonly ApiClient _api;

    public CoberturaService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /pacientes/{id}/coberturas. Solo pide estar autenticado (sin rol
    /// específico), así que sirve tanto para la ficha de administrador/doctor
    /// como para un futuro "mi perfil" del propio paciente.
    /// </summary>
    public async Task<IReadOnlyList<PacienteCobertura>> ObtenerDePacienteAsync(int idPaciente)
    {
        var respuesta = await _api.GetAsync<PacienteCoberturasRespuesta>($"/pacientes/{idPaciente}/coberturas");
        return respuesta?.Coberturas ?? Array.Empty<PacienteCobertura>();
    }
}
