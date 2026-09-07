namespace ChronoSaludWeb.Services;

/// <summary>
/// Espeja MedicamentoDto de la API.
/// </summary>
public record Medicamento(int IdMedicamento, string Nombre, string? Descripcion);

/// <summary>
/// Respuesta de GET /medicamentos: { medicamentos }.
/// </summary>
public record MedicamentosRespuesta(IReadOnlyList<Medicamento> Medicamentos);

public class MedicamentoService
{
    private readonly ApiClient _api;

    public MedicamentoService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /medicamentos. No pagina: devuelve el vademécum completo.
    /// </summary>
    public async Task<IReadOnlyList<Medicamento>> ObtenerTodosAsync()
    {
        var respuesta = await _api.GetAsync<MedicamentosRespuesta>("/medicamentos");
        return respuesta?.Medicamentos ?? Array.Empty<Medicamento>();
    }

    /// <summary>
    /// Diccionario id -> nombre, para resolver los medicamentos de una receta:
    /// RecetaMedicamentoDto solo trae el IdMedicamento, no el nombre.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, Medicamento>> ObtenerPorIdAsync()
        => (await ObtenerTodosAsync()).ToDictionary(m => m.IdMedicamento);
}
