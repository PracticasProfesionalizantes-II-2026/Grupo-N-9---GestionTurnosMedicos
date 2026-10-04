namespace ChronoSaludWeb.Services;

/// <summary>
/// Datos de la cuenta. Espeja UsuarioDto de la API.
/// </summary>
public record UsuarioDetalle(
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string Rol);

/// <summary>
/// Una fila del buscador. Espeja UsuarioListaDto de la API: IdPaciente e
/// IdDoctor vienen cargados solo si el usuario tiene ese perfil.
/// </summary>
public record UsuarioLista(
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string Rol,
    bool Activo,
    int? IdPaciente,
    int? IdDoctor,
    bool TieneFoto);

/// <summary>
/// Respuesta de GET /usuarios: { total, pagina, usuarios }.
/// </summary>
public record UsuariosPagina(int Total, int Pagina, IReadOnlyList<UsuarioLista> Usuarios);

public class UsuarioService
{
    // Tope de "limite" que acepta GET /usuarios.
    private const int LimiteMaximo = 100;

    private readonly ApiClient _api;

    public UsuarioService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /usuarios/{id}. Null si no existe (la API contesta 404).
    /// </summary>
    public async Task<UsuarioDetalle?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _api.GetAsync<UsuarioDetalle>($"/usuarios/{id}");
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// PUT /usuarios/{id} con nombre, apellido y teléfono. La contraseña no se
    /// manda nunca desde acá y el email la API no lo deja cambiar.
    /// Ojo: la API ignora los campos vacíos, así que un teléfono en blanco no
    /// borra el que ya estaba. Tampoco revisa quién hace el pedido: que sea un
    /// administrador lo exige UsuariosController, no este endpoint.
    /// </summary>
    public Task ActualizarAsync(int id, string nombre, string apellido, string? telefono)
        => _api.PutAsync($"/usuarios/{id}", new { nombre, apellido, telefono });

    /// <summary>
    /// GET /usuarios: busca "contiene" en nombre, apellido o email entre los
    /// usuarios activos, con filtro opcional por rol. Reservado a administrador
    /// (403 con el cuerpo vacío al resto); un rol que no existe da 400.
    /// </summary>
    public async Task<UsuariosPagina> BuscarAsync(string? buscar, string? rol, int pagina, int limite)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["buscar"] = buscar,
            ["rol"] = rol,
            ["pagina"] = pagina,
            ["limite"] = limite
        };

        var resultado = await _api.GetAsync<UsuariosPagina>("/usuarios", parametros);
        return resultado ?? new UsuariosPagina(0, pagina, Array.Empty<UsuarioLista>());
    }

    /// <summary>
    /// La fila del buscador de un usuario puntual, que es de donde salen su
    /// IdPaciente y su IdDoctor: UsuarioDto no los trae y no hay endpoint que
    /// los resuelva por IdUsuario. Busca por su email (es único) y se queda con
    /// la fila de ese id. Null si está dado de baja o si la búsqueda falla, así
    /// la pantalla de edición muestra igual los datos de la cuenta.
    /// </summary>
    public async Task<UsuarioLista?> ObtenerFilaAsync(UsuarioDetalle usuario)
    {
        try
        {
            var pagina = await BuscarAsync(usuario.Email, rol: null, pagina: 1, LimiteMaximo);
            return pagina.Usuarios.FirstOrDefault(u => u.IdUsuario == usuario.IdUsuario);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return null;
        }
    }
}
