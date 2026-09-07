namespace ChronoSaludWeb.Services;

/// <summary>
/// Una entrada del historial. Espeja HistorialClinicoDto.
/// No trae el doctor: quién la escribió no se puede saber desde el front.
/// </summary>
public record EntradaHistorial(
    int IdHistorial,
    DateTime Fecha,
    string Descripcion,
    string Diagnostico,
    int? IdTurno);

/// <summary>
/// Respuesta de GET /pacientes/{id}/historiales-clinicos: { id_paciente, historiales }.
/// Solo se usa la lista; el id del paciente ya lo conoce quien llama.
/// </summary>
public record HistorialRespuesta(IReadOnlyList<EntradaHistorial> Historiales);

/// <summary>
/// Cuerpo del alta. Espeja HistorialClinicoCreateDto.
/// </summary>
public record EntradaHistorialNueva(
    DateTime Fecha,
    string Descripcion,
    string Diagnostico,
    int? IdTurno);

public class HistorialService
{
    private readonly ApiClient _api;

    public HistorialService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /pacientes/{id}/historiales-clinicos, con el rango de fechas que
    /// acepta la API. Es el único listado: no hay endpoint de detalle.
    /// </summary>
    public async Task<IReadOnlyList<EntradaHistorial>> ObtenerDePacienteAsync(
        int idPaciente, DateTime? desde = null, DateTime? hasta = null)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["fecha_desde"] = desde,
            ["fecha_hasta"] = hasta
        };

        var respuesta = await _api.GetAsync<HistorialRespuesta>(
            $"/pacientes/{idPaciente}/historiales-clinicos", parametros);

        return respuesta?.Historiales ?? Array.Empty<EntradaHistorial>();
    }

    /// <summary>
    /// POST /pacientes/{id}/historiales-clinicos. La API lo reserva al rol doctor.
    /// Contesta 201 con un mensaje, sin el id de la entrada creada.
    /// </summary>
    public Task CrearAsync(int idPaciente, EntradaHistorialNueva entrada)
        => _api.PostAsync($"/pacientes/{idPaciente}/historiales-clinicos", entrada);
}
