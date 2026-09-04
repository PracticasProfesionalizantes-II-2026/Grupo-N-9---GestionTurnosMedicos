namespace ChronoSaludWeb.Services;

/// <summary>
/// Una fila del listado. Espeja TurnoListaDto de la API.
/// Ojo: la API NO manda la hora ni la especialidad en el listado.
/// FechaInicio viene siempre a las 00:00 porque la hora se guarda aparte
/// (Turno.HoraInicio) y solo la expone GET /turnos/{id}.
/// </summary>
public record TurnoLista(
    int IdTurno,
    DateTime FechaInicio,
    string Estado,
    string Doctor,
    string Paciente);

/// <summary>
/// Respuesta de GET /turnos: { total, turnos }.
/// </summary>
public record TurnosPagina(int Total, IReadOnlyList<TurnoLista> Turnos);

public class TurnoService
{
    private readonly ApiClient _api;

    public TurnoService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /turnos con los filtros que acepta la API. Los parámetros en null
    /// no se mandan (de eso se encarga el ApiClient).
    /// </summary>
    public async Task<TurnosPagina> ObtenerAsync(
        int? pacienteId = null,
        int? doctorId = null,
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        int pagina = 1,
        int limite = 20)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["paciente_id"] = pacienteId,
            ["doctor_id"]   = doctorId,
            ["estado"]      = estado,
            ["fecha_desde"] = desde,
            ["fecha_hasta"] = hasta,
            ["pagina"]      = pagina,
            ["limite"]      = limite,
        };

        var pagina_ = await _api.GetAsync<TurnosPagina>("/turnos", parametros);
        return pagina_ ?? new TurnosPagina(0, Array.Empty<TurnoLista>());
    }
}
