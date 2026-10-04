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

public class UsuarioService
{
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
}
