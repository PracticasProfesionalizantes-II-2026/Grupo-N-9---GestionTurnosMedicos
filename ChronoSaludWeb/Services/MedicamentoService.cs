namespace ChronoSaludWeb.Services;

/// <summary>
/// Espeja MedicamentoDto de la API. Nombre es el nombre comercial; el resto de
/// los datos del vademécum puede faltar en medicamentos cargados a mano.
/// </summary>
public record Medicamento(
    int IdMedicamento,
    string Nombre,
    string? Descripcion,
    string? NombreGenerico = null,
    string? Concentracion = null,
    string? FormaFarmaceutica = null,
    string? Laboratorio = null);

/// <summary>
/// Respuesta de GET /medicamentos: { medicamentos }.
/// </summary>
public record MedicamentosRespuesta(IReadOnlyList<Medicamento> Medicamentos);

public class MedicamentoService
{
    /// <summary>
    /// Nombre de la fila de catálogo a la que apuntan los medicamentos escritos
    /// a mano ("Otro..."): la API exige un IdMedicamento, y el nombre real va en
    /// las indicaciones. La carga el seeder con este mismo texto.
    /// </summary>
    public const string NombreMarcadorOtro = "Otro (ver indicaciones)";

    private readonly ApiClient _api;

    public MedicamentoService(ApiClient api) => _api = api;

    public static bool EsMarcadorOtro(Medicamento medicamento) =>
        EsMarcadorOtro(medicamento.Nombre, medicamento.NombreGenerico);

    /// <summary>
    /// Lo mismo, a partir del nombre y el genérico: sirve para la copia que
    /// guarda cada receta.
    /// </summary>
    public static bool EsMarcadorOtro(string? nombre, string? nombreGenerico) =>
        string.Equals(nombre?.Trim(), NombreMarcadorOtro, StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrWhiteSpace(nombreGenerico);

    /// <summary>
    /// GET /medicamentos. No pagina: devuelve el vademécum completo.
    /// </summary>
    public async Task<IReadOnlyList<Medicamento>> ObtenerTodosAsync()
    {
        var respuesta = await _api.GetAsync<MedicamentosRespuesta>("/medicamentos");
        return respuesta?.Medicamentos ?? Array.Empty<Medicamento>();
    }

    /// <summary>
    /// Id del marcador de "Otro...", o null si todavía no se cargó en la API.
    /// </summary>
    public async Task<int?> ObtenerIdMarcadorOtroAsync()
        => (await ObtenerTodosAsync()).FirstOrDefault(EsMarcadorOtro)?.IdMedicamento;
}
