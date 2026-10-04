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

/// <summary>
/// Una foto que existe y que el usuario logueado puede ver. Espeja
/// FotoDisponibleDto: Id es el id por el que se preguntó (de paciente, de
/// doctor o de turno) e IdUsuario el dueño de la foto.
/// </summary>
public record FotoDisponible(int Id, int IdUsuario);

/// <summary>
/// Respuesta de GET /usuarios/fotos: { pacientes, doctores, turnos }.
/// </summary>
public record FotosDisponiblesRespuesta(
    IReadOnlyList<FotoDisponible>? Pacientes,
    IReadOnlyList<FotoDisponible>? Doctores,
    IReadOnlyList<FotoDisponible>? Turnos);

/// <summary>
/// Quién tiene foto entre los ids consultados: cada diccionario va del id por
/// el que se preguntó al IdUsuario del dueño. Un id ausente no tiene foto, o
/// tiene una que el usuario logueado no puede ver.
/// </summary>
public class FotosDisponibles
{
    public Dictionary<int, int> Pacientes { get; } = new();
    public Dictionary<int, int> Doctores { get; } = new();
    public Dictionary<int, int> Turnos { get; } = new();
}

public class UsuarioService
{
    // Tope de "limite" que acepta GET /usuarios.
    private const int LimiteMaximo = 100;

    // Tope de ids por lista que acepta GET /usuarios/fotos (400 si se pasa).
    private const int MaximoIdsPorConsulta = 100;

    private readonly ApiClient _api;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(ApiClient api, ILogger<UsuarioService> logger)
    {
        _api = api;
        _logger = logger;
    }

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
    /// PUT /usuarios/{id}/foto: crea o reemplaza la foto. Reservado a
    /// administrador. La API vuelve a validar tamaño y firma (400 si no pasa).
    /// </summary>
    public Task SubirFotoAsync(int id, ImagenValidada imagen)
        => _api.PutArchivoAsync($"/usuarios/{id}/foto", "archivo", imagen.Contenido, imagen.TipoContenido);

    /// <summary>
    /// DELETE /usuarios/{id}/foto. Reservado a administrador. No falla si el
    /// usuario no tenía foto.
    /// </summary>
    public Task QuitarFotoAsync(int id)
        => _api.DeleteAsync($"/usuarios/{id}/foto");

    /// <summary>
    /// GET /usuarios/{id}/foto. Null si no tiene foto. La API se la da a
    /// administrador, doctor o al propio usuario (403 al resto). Con el ETag de
    /// la copia que ya tiene el navegador, contesta sin contenido si no cambió.
    /// </summary>
    public Task<ArchivoApi?> ObtenerFotoAsync(int id, string? etag = null)
        => _api.GetArchivoAsync($"/usuarios/{id}/foto", etag);

    /// <summary>
    /// GET /usuarios/fotos: de los pacientes, doctores y turnos (por su
    /// paciente) de una pantalla, cuáles tienen una foto que el usuario
    /// logueado puede ver. Sirve para pintar el &lt;img&gt; solo donde hay foto.
    /// La API acepta hasta 100 ids por lista: si llegan más se parte en varios
    /// pedidos, y sin ids no se llama. Si la consulta falla devuelve vacío (la
    /// pantalla se ve con iniciales) y deja un Warning en el log, para poder
    /// distinguir "nadie tiene foto" de "falló".
    /// </summary>
    public async Task<FotosDisponibles> ObtenerFotosAsync(
        IEnumerable<int>? pacientes = null,
        IEnumerable<int>? doctores = null,
        IEnumerable<int>? turnos = null)
    {
        var tandasPacientes = Tandas(pacientes);
        var tandasDoctores = Tandas(doctores);
        var tandasTurnos = Tandas(turnos);

        var pedidos = Math.Max(tandasPacientes.Count, Math.Max(tandasDoctores.Count, tandasTurnos.Count));
        var fotos = new FotosDisponibles();

        try
        {
            for (var i = 0; i < pedidos; i++)
            {
                var parametros = new Dictionary<string, object?>
                {
                    ["pacientes"] = Unir(tandasPacientes, i),
                    ["doctores"] = Unir(tandasDoctores, i),
                    ["turnos"] = Unir(tandasTurnos, i)
                };

                var respuesta = await _api.GetAsync<FotosDisponiblesRespuesta>("/usuarios/fotos", parametros);

                Volcar(respuesta?.Pacientes, fotos.Pacientes);
                Volcar(respuesta?.Doctores, fotos.Doctores);
                Volcar(respuesta?.Turnos, fotos.Turnos);
            }
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            _logger.LogWarning(
                "No se pudo consultar GET /usuarios/fotos (status {Status}): {Mensaje}. La pantalla se muestra con iniciales.",
                error.Status, error.Message);
            return new FotosDisponibles();
        }

        return fotos;
    }

    // Ids válidos y sin repetir, en grupos de a lo sumo 100.
    private static List<int[]> Tandas(IEnumerable<int>? ids) =>
        (ids ?? Enumerable.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .Chunk(MaximoIdsPorConsulta)
            .ToList();

    // "1,2,3", o null (parámetro que no se manda) si esa lista no tiene tanda i.
    private static string? Unir(List<int[]> tandas, int i) =>
        i < tandas.Count ? string.Join(',', tandas[i]) : null;

    private static void Volcar(IReadOnlyList<FotoDisponible>? origen, Dictionary<int, int> destino)
    {
        if (origen is null) return;

        foreach (var foto in origen)
            destino[foto.Id] = foto.IdUsuario;
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
