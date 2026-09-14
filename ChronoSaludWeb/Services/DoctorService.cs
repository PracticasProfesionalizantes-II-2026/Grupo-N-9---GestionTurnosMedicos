namespace ChronoSaludWeb.Services;

/// <summary>
/// Perfil de doctor. Espeja DoctorDto de la API.
/// </summary>
public record DoctorDetalle(
    int IdDoctor,
    string Nombre,
    string Apellido,
    string Especialidad,
    string Matricula,
    string? Consultorio);

/// <summary>
/// Una fila del listado. Espeja DoctorListaDto: Nombre ya viene armado
/// como "Nombre Apellido".
/// </summary>
public record DoctorLista(int IdDoctor, string Nombre, string Especialidad, string Matricula);

/// <summary>
/// Respuesta de GET /doctores: { total, doctores }.
/// </summary>
public record DoctoresPagina(int Total, IReadOnlyList<DoctorLista> Doctores);

public class DoctorService
{
    private readonly ApiClient _api;

    public DoctorService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /doctores/{id}. Null si no existe, así una inconsistencia de datos
    /// no rompe la pantalla que lo estaba mostrando.
    /// </summary>
    public async Task<DoctorDetalle?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _api.GetAsync<DoctorDetalle>($"/doctores/{id}");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// GET /doctores con el filtro por especialidad, que la API resuelve como
    /// "contiene". Solo devuelve doctores activos.
    /// </summary>
    public async Task<DoctoresPagina> BuscarAsync(string? especialidad = null, int limite = 100)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["especialidad"] = especialidad,
            ["pagina"] = 1,
            ["limite"] = limite
        };

        var pagina = await _api.GetAsync<DoctoresPagina>("/doctores", parametros);
        return pagina ?? new DoctoresPagina(0, Array.Empty<DoctorLista>());
    }

    /// <summary>
    /// Todos los doctores, para llenar un select de una sola vez.
    /// </summary>
    public async Task<IReadOnlyList<DoctorLista>> ObtenerTodosAsync(int limite = 200)
        => (await BuscarAsync(limite: limite)).Doctores;

    /// <summary>
    /// GET /doctores/me: el perfil de doctor del usuario logueado.
    /// Null si el usuario no tiene uno (la API contesta 404).
    /// Hace falta porque IdUsuario e IdDoctor son de tablas distintas.
    /// </summary>
    public async Task<DoctorDetalle?> ObtenerMiPerfilAsync()
    {
        try
        {
            return await _api.GetAsync<DoctorDetalle>("/doctores/me");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// POST /doctores: completa la fila de Doctores de un Usuario que ya existe
    /// (con rol "doctor" o no — la API no lo valida). Reservado a administrador.
    /// Devuelve el IdDoctor creado. Tira ApiException 409 con "matrícula" si ya
    /// está tomada, 400 si faltan datos o el IdUsuario no existe.
    /// </summary>
    public Task<DoctorAlta?> CrearAsync(int idUsuario, string especialidad, string matricula, string? consultorio)
        => _api.PostAsync<DoctorAlta>(
            "/doctores",
            new { idUsuario, especialidad, matricula, consultorio });
}

/// <summary>
/// Respuesta de POST /doctores: { id_doctor, especialidad, matricula }. A
/// diferencia del resto de la API, acá el id viene en snake_case (el endpoint
/// arma un objeto anónimo a mano en vez de un DTO), así que hace falta el
/// JsonPropertyName explícito: la comparación de ApiClient solo ignora
/// mayúsculas/minúsculas, no el guión bajo.
/// </summary>
public record DoctorAlta(
    [property: System.Text.Json.Serialization.JsonPropertyName("id_doctor")] int IdDoctor,
    string Especialidad,
    string Matricula);
