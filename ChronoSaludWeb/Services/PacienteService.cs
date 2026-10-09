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
    string? Condiciones,
    int IdUsuario = 0,
    string? Email = null,
    string? Telefono = null,
    string? Dni = null,
    string? Direccion = null,
    string? Nacionalidad = null,
    string? EstadoCivil = null,
    string? TipoDocumento = null,
    string? Provincia = null,
    string? Localidad = null,
    string? CodigoPostal = null,
    string? ContactoEmergenciaNombre = null,
    string? ContactoEmergenciaTelefono = null);

/// <summary>
/// Datos de la ficha del paciente, como los espera la API: es el cuerpo de
/// PUT /pacientes/{id} y la "ficha" del alta (POST /usuarios/registro).
/// Un dato en null no cambia lo que ya estaba guardado; para vaciarlo hay que
/// nombrarlo en <see cref="Borrar"/> (por ejemplo "alergias").
/// </summary>
public class DatosFichaPaciente
{
    public DateTime? FechaNacimiento { get; set; }
    public string? Sexo { get; set; }
    public string? GrupoSanguineo { get; set; }
    public string? Alergias { get; set; }
    public string? Condiciones { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Dni { get; set; }
    public string? Nacionalidad { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Direccion { get; set; }
    public string? Provincia { get; set; }
    public string? Localidad { get; set; }
    public string? CodigoPostal { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public List<string>? Borrar { get; set; }
}

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
    /// GET /pacientes con el filtro por nombre, que la API resuelve como
    /// "contiene" sobre nombre o apellido.
    /// Solo lo permite a doctor y administrador: con otro rol responde 403
    /// con el cuerpo vacío.
    /// </summary>
    public async Task<PacientesPagina> BuscarAsync(string? nombre = null, int limite = 100)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["nombre"] = nombre,
            ["pagina"] = 1,
            ["limite"] = limite
        };

        var pagina = await _api.GetAsync<PacientesPagina>("/pacientes", parametros);
        return pagina ?? new PacientesPagina(0, 1, Array.Empty<PacienteLista>());
    }

    /// <summary>
    /// Todos los pacientes, para llenar un select de una sola vez.
    /// </summary>
    public async Task<IReadOnlyList<PacienteLista>> ObtenerTodosAsync(int limite = 200)
        => (await BuscarAsync(limite: limite)).Pacientes;

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

    /// <summary>
    /// PUT /pacientes/{id} con la ficha completa: la usan la administración
    /// (Usuarios/Editar) y el propio paciente (Mi perfil). Los datos en null no
    /// cambian; los nombrados en ficha.Borrar se vacían. Contesta 409 si el DNI
    /// ya es de otro paciente, y 400 si un paciente quiere cambiar o borrar su
    /// DNI ya cargado.
    /// </summary>
    public Task ActualizarFichaAsync(int idPaciente, DatosFichaPaciente ficha)
        => _api.PutAsync($"/pacientes/{idPaciente}", ficha);
}
