using System.Text.Json.Serialization;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Un medicamento dentro de una receta. Espeja RecetaMedicamentoDto.
/// Ojo: solo trae el id, no el nombre; hay que cruzarlo con GET /medicamentos.
/// </summary>
public record RecetaMedicamento(
    int IdMedicamento,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones);

/// <summary>
/// Espeja RecetaDto. No trae IdPaciente ni IdDoctor: el paciente se conoce
/// por el endpoint que se consultó, y quién la emitió no se puede saber.
/// </summary>
public record Receta(
    int IdReceta,
    DateTime Fecha,
    DateTime Vigencia,
    string? Detalles,
    IReadOnlyList<RecetaMedicamento> Medicamentos);

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
