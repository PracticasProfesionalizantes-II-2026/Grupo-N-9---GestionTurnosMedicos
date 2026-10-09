using System.Text.Json.Serialization;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Un medicamento de la receta que se manda al emitirla. Espeja
/// RecetaMedicamentoDto: va solo el id, los datos del medicamento los copia la API.
/// </summary>
public record RecetaMedicamento(
    int IdMedicamento,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones);

/// <summary>
/// Un medicamento de una receta ya emitida. Espeja MedicamentoRecetadoDto:
/// nombre, genérico, concentración y forma son una copia guardada al emitir,
/// así que no cambian si después se edita el medicamento.
/// </summary>
public record MedicamentoRecetado(
    int IdMedicamento,
    string? Nombre,
    string? NombreGenerico,
    string? Concentracion,
    string? FormaFarmaceutica,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones);

/// <summary>
/// Espeja RecetaDto. No trae IdPaciente: el paciente se conoce por el
/// endpoint que se consultó. Doctor es quien la firmó.
/// </summary>
public record Receta(
    int IdReceta,
    DateTime Fecha,
    DateTime Vigencia,
    string? Detalles,
    IReadOnlyList<MedicamentoRecetado> Medicamentos,
    Profesional? Doctor = null);

/// <summary>
/// Respuesta de GET /pacientes/{id}/recetas: { recetas }.
/// </summary>
public record RecetasRespuesta(IReadOnlyList<Receta> Recetas);

/// <summary>
/// Cuerpo de POST /recetas. Espeja RecetaCreateDto.
/// </summary>
public record RecetaNueva(
    int IdPaciente,
    int IdDoctor,
    int? IdTurno,
    DateTime Fecha,
    DateTime Vigencia,
    string? Detalles,
    IReadOnlyList<RecetaMedicamento> Medicamentos);

/// <summary>
/// Respuesta del alta. Como en el alta de turnos, este endpoint contesta en
/// snake_case a diferencia del resto de la API.
/// </summary>
public record RecetaCreada(
    [property: JsonPropertyName("id_receta")] int IdReceta);

public class RecetaService
{
    private readonly ApiClient _api;

    public RecetaService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /pacientes/{id}/recetas. Es el único listado que expone la API:
    /// no hay un GET /recetas general ni un GET /recetas/{id}.
    /// </summary>
    public async Task<IReadOnlyList<Receta>> ObtenerDePacienteAsync(int idPaciente)
    {
        var respuesta = await _api.GetAsync<RecetasRespuesta>($"/pacientes/{idPaciente}/recetas");
        return respuesta?.Recetas ?? Array.Empty<Receta>();
    }

    /// <summary>
    /// POST /recetas. La API lo reserva al rol doctor.
    /// </summary>
    public Task<RecetaCreada?> CrearAsync(RecetaNueva receta)
        => _api.PostAsync<RecetaCreada>("/recetas", receta);
}
