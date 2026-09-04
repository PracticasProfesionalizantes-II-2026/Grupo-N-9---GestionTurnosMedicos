using System.Text.Json.Serialization;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Una fila del listado. Espeja TurnoListaDto de la API.
/// FechaInicio viene siempre a las 00:00: la hora se guarda aparte
/// (Turno.HoraInicio) y llega en HoraInicio como "HH:mm".
/// </summary>
public record TurnoLista(
    int IdTurno,
    DateTime FechaInicio,
    string HoraInicio,
    string Estado,
    string Doctor,
    string Especialidad,
    string Paciente);

/// <summary>
/// Detalle de un turno. Espeja TurnoDto de la API.
/// Ojo: solo trae los IDs del paciente y del doctor, no los nombres.
/// </summary>
public record TurnoDetalle(
    int IdTurno,
    DateTime FechaInicio,
    string HoraInicio,
    string HoraFin,
    string Estado,
    int IdPaciente,
    int IdDoctor,
    string? Observaciones);

/// <summary>
/// Cuerpo de POST /turnos. Espeja TurnoCreateDto de la API.
/// </summary>
public record TurnoNuevo(
    int IdPaciente,
    int IdDoctor,
    DateTime FechaInicio,
    string HoraInicio,
    string HoraFin,
    string? Observaciones);

/// <summary>
/// Respuesta del alta. Ojo: este endpoint contesta en snake_case
/// (id_turno), a diferencia del resto de la API, que usa camelCase.
/// </summary>
public record TurnoCreado(
    [property: JsonPropertyName("id_turno")] int IdTurno,
    string Estado);

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

    /// <summary>
    /// GET /turnos/{id}. Devuelve null si la API contesta 404, para que el
    /// controlador muestre la vista de "no encontrado" en vez de un error.
    /// </summary>
    public async Task<TurnoDetalle?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _api.GetAsync<TurnoDetalle>($"/turnos/{id}");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// POST /turnos. Deja pasar la ApiException para que el controlador muestre
    /// el mensaje dentro del formulario (409 por conflicto de horario, 400 por
    /// datos inválidos) sin perder lo que el usuario ya cargó.
    /// </summary>
    public Task<TurnoCreado?> CrearAsync(TurnoNuevo turno)
        => _api.PostAsync<TurnoCreado>("/turnos", turno);

    /// <summary>
    /// DELETE /turnos/{id}. Es una baja lógica: la API deja el turno en estado
    /// "cancelado", no lo borra. Contesta 204, así que no devuelve nada.
    /// </summary>
    public Task CancelarAsync(int id) => _api.DeleteAsync($"/turnos/{id}");
}
