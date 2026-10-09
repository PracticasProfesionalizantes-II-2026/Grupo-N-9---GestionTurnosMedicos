namespace ChronoSaludWeb.Services;

/// <summary>
/// Una notificación. Espeja NotificacionDto de la API. La fecha ya viene en
/// hora de Argentina.
/// </summary>
public record NotificacionLista(int Id, string Tipo, string Mensaje, DateTime Fecha, bool Leida);

/// <summary>
/// Respuesta de GET /usuarios/{id}/notificaciones: { total, notificaciones }.
/// </summary>
public record NotificacionesPagina(int Total, IReadOnlyList<NotificacionLista> Notificaciones);

/// <summary>Respuesta de GET /usuarios/{id}/notificaciones/no-leidas.</summary>
public record NoLeidasRespuesta(int NoLeidas);

/// <summary>Respuesta de PATCH /notificaciones/leer-todas.</summary>
public record MarcadasRespuesta(int Marcadas);

/// <summary>
/// Las notificaciones del usuario logueado. Todos los pedidos son sobre las
/// propias: el id sale de la sesión.
/// </summary>
public class NotificacionService
{
    /// <summary>La API las devuelve de a 20.</summary>
    public const int PorPagina = 20;

    private readonly ApiClient _api;
    private readonly IHttpContextAccessor _contexto;

    public NotificacionService(ApiClient api, IHttpContextAccessor contexto)
    {
        _api = api;
        _contexto = contexto;
    }

    private ISession? Sesion => _contexto.HttpContext?.Session;

    private int IdUsuario => Sesion?.ObtenerSesion()?.IdUsuario ?? 0;

    /// <summary>
    /// GET /usuarios/{id}/notificaciones, de la más nueva a la más vieja.
    /// Con <paramref name="soloNoLeidas"/> trae solo las sin leer.
    /// </summary>
    public async Task<NotificacionesPagina> ObtenerAsync(int pagina, bool soloNoLeidas)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["pagina"] = pagina,
            ["leida"] = soloNoLeidas ? false : null
        };

        var respuesta = await _api.GetAsync<NotificacionesPagina>($"/usuarios/{IdUsuario}/notificaciones", parametros);
        return respuesta ?? new NotificacionesPagina(0, Array.Empty<NotificacionLista>());
    }

    /// <summary>
    /// Cuántas tiene sin leer, preguntándole a la API. De paso actualiza el
    /// número que guarda la campana.
    /// </summary>
    public async Task<int> ContarNoLeidasAsync()
    {
        var respuesta = await _api.GetAsync<NoLeidasRespuesta>($"/usuarios/{IdUsuario}/notificaciones/no-leidas");
        var cantidad = respuesta?.NoLeidas ?? 0;

        if (Sesion is not null)
            ContadorDeNoLeidas.Guardar(Sesion, cantidad, DateTime.UtcNow);

        return cantidad;
    }

    /// <summary>
    /// El número de la campana: el guardado en la sesión si tiene menos de un
    /// minuto; si no, se le pregunta a la API. Si la API falla devuelve null y
    /// la campana se muestra sin número: la página no se cae por esto.
    /// </summary>
    public async Task<int?> NoLeidasParaLaCampanaAsync()
    {
        if (Sesion is not null && ContadorDeNoLeidas.Leer(Sesion, DateTime.UtcNow) is { } guardado)
            return guardado;

        try
        {
            return await ContarNoLeidasAsync();
        }
        catch (ApiException)
        {
            return null;
        }
    }

    /// <summary>PATCH /notificaciones/{id}/leer. La API solo deja marcar las propias.</summary>
    public async Task MarcarLeidaAsync(int id)
    {
        await _api.PatchAsync($"/notificaciones/{id}/leer", null);
        if (Sesion is not null)
            ContadorDeNoLeidas.Borrar(Sesion);
    }

    /// <summary>PATCH /notificaciones/leer-todas. Devuelve cuántas marcó.</summary>
    public async Task<int> MarcarTodasAsync()
    {
        var respuesta = await _api.PatchAsync<MarcadasRespuesta>("/notificaciones/leer-todas", null);
        if (Sesion is not null)
            ContadorDeNoLeidas.Borrar(Sesion);

        return respuesta?.Marcadas ?? 0;
    }
}
