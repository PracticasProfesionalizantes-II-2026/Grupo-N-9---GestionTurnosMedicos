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
    /// GET /doctores. El límite alto es para llenar el select de una sola vez.
    /// </summary>
    public async Task<IReadOnlyList<DoctorLista>> ObtenerTodosAsync(int limite = 200)
    {
        var parametros = new Dictionary<string, object?> { ["pagina"] = 1, ["limite"] = limite };
        var pagina = await _api.GetAsync<DoctoresPagina>("/doctores", parametros);
        return pagina?.Doctores ?? Array.Empty<DoctorLista>();
    }

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
}
