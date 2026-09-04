namespace ChronoSaludWeb.Services;

/// <summary>
/// Perfil de paciente. Espeja PacienteDto de la API.
/// </summary>
public record PacienteDetalle(
    int IdPaciente,
    string Nombre,
    string Apellido,
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones);

/// <summary>
/// Una fila del listado. Espeja PacienteListaDto de la API.
/// </summary>
public record PacienteLista(int IdPaciente, string Nombre, string Apellido);

/// <summary>
/// Respuesta de GET /pacientes: { total, pagina, pacientes }.
/// </summary>
public record PacientesPagina(int Total, int Pagina, IReadOnlyList<PacienteLista> Pacientes);

public class PacienteService
{
    private readonly ApiClient _api;

    public PacienteService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /pacientes/{id}. Null si no existe, así una inconsistencia de datos
    /// no rompe la pantalla que lo estaba mostrando.
    /// </summary>
    public async Task<PacienteDetalle?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _api.GetAsync<PacienteDetalle>($"/pacientes/{id}");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// GET /pacientes. Solo lo permite a doctor y administrador: con otro rol
    /// la API responde 403 con el cuerpo vacío.
    /// El límite alto es para llenar el select de una sola vez.
    /// </summary>
    public async Task<IReadOnlyList<PacienteLista>> ObtenerTodosAsync(int limite = 200)
    {
        var parametros = new Dictionary<string, object?> { ["pagina"] = 1, ["limite"] = limite };
        var pagina = await _api.GetAsync<PacientesPagina>("/pacientes", parametros);
        return pagina?.Pacientes ?? Array.Empty<PacienteLista>();
    }

    /// <summary>
    /// GET /pacientes/me: el perfil de paciente del usuario logueado.
    /// Null si el usuario no tiene uno (la API contesta 404).
    /// Hace falta porque IdUsuario e IdPaciente son de tablas distintas.
    /// </summary>
    public async Task<PacienteDetalle?> ObtenerMiPerfilAsync()
    {
        try
        {
            return await _api.GetAsync<PacienteDetalle>("/pacientes/me");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Cuántos pacientes hay. Pide limite=1 y se queda con el "total" que
    /// informa la API, para no traerse la tabla entera solo para contar.
    /// </summary>
    public async Task<int> ContarAsync()
    {
        var parametros = new Dictionary<string, object?> { ["pagina"] = 1, ["limite"] = 1 };
        var pagina = await _api.GetAsync<PacientesPagina>("/pacientes", parametros);
        return pagina?.Total ?? 0;
    }
}
