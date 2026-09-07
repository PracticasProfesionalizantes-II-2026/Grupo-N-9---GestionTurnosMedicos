namespace ChronoSaludWeb.Services;

/// <summary>
/// Resuelve el IdPaciente o IdDoctor del usuario logueado, que no son el
/// IdUsuario: son tablas distintas y hay que preguntárselo a la API.
/// </summary>
public class PerfilService
{
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly IHttpContextAccessor _contexto;

    public PerfilService(PacienteService pacientes, DoctorService doctores, IHttpContextAccessor contexto)
    {
        _pacientes = pacientes;
        _doctores = doctores;
        _contexto = contexto;
    }

    /// <summary>
    /// Se cachea en la sesión para no pedirlo en cada pantalla, pero solo
    /// cuando existe: si todavía no tiene perfil se vuelve a preguntar, así
    /// aparece apenas se lo crean sin necesidad de volver a loguearse.
    /// </summary>
    public async Task<int?> IdPerfilAsync(bool esDoctor)
    {
        var sesion = _contexto.HttpContext?.Session;

        var cacheado = sesion?.ObtenerIdPerfil();
        if (cacheado is not null) return cacheado;

        var id = esDoctor
            ? (await _doctores.ObtenerMiPerfilAsync())?.IdDoctor
            : (await _pacientes.ObtenerMiPerfilAsync())?.IdPaciente;

        if (id is not null) sesion?.GuardarIdPerfil(id.Value);
        return id;
    }
}
