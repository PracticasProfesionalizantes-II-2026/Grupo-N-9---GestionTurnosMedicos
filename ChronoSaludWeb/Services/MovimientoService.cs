namespace ChronoSaludWeb.Services;

/// <summary>
/// Una fila del historial. Espeja MovimientoDto de la API. FechaUtc viene en
/// UTC. IdUsuario y Usuario (nombre y apellido de quien actuó) solo llegan
/// cuando pregunta un administrador: al doctor le vienen en null.
/// </summary>
public record MovimientoLista(
    int IdMovimiento,
    DateTime FechaUtc,
    string Accion,
    string Entidad,
    int IdEntidad,
    int? IdDoctor,
    string Resumen,
    string RolUsuario,
    int? IdUsuario,
    string? Usuario);

/// <summary>
/// Respuesta de GET /movimientos: { total, movimientos }.
/// </summary>
public record MovimientosPagina(int Total, IReadOnlyList<MovimientoLista> Movimientos);

/// <summary>
/// Lectura del historial de movimientos. No hay escritura: las filas las deja
/// la API cuando se cambia un horario o un turno.
/// </summary>
public class MovimientoService
{
    private readonly ApiClient _api;

    public MovimientoService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /movimientos, del más nuevo al más viejo. El alcance lo decide la
    /// API con el token: el administrador ve todo y el doctor solo lo de su
    /// agenda y su horario (a él le ignora <paramref name="doctorId"/>).
    /// <paramref name="desde"/> y <paramref name="hasta"/> son días de
    /// Argentina, los dos incluidos. Deja pasar la ApiException: 403 para un
    /// paciente, 404 para un doctor sin perfil.
    /// </summary>
    public async Task<MovimientosPagina> BuscarAsync(
        int? doctorId = null,
        string? accion = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        int pagina = 1,
        int limite = 20)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["doctor_id"]   = doctorId,
            ["accion"]      = accion,
            ["fecha_desde"] = desde,
            ["fecha_hasta"] = hasta,
            ["pagina"]      = pagina,
            ["limite"]      = limite,
        };

        var resultado = await _api.GetAsync<MovimientosPagina>("/movimientos", parametros);
        return resultado ?? new MovimientosPagina(0, Array.Empty<MovimientoLista>());
    }
}
