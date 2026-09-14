namespace ChronoSaludWeb.Services;

/// <summary>
/// Respuesta de POST /usuarios/login. Espeja LoginResponseDto de la API.
/// </summary>
public record LoginRespuesta(string Token, string Rol, int IdUsuario, string Nombre);

/// <summary>
/// Respuesta de POST /usuarios/registro. Espeja RegistroResponseDto de la API:
/// ya incluye el token, no hace falta loguear aparte después de registrarse.
/// </summary>
public record RegistroRespuesta(int IdUsuario, string Email, string Rol, string Token);

/// <summary>
/// Login / logout contra la API. El token se guarda en la sesión del servidor,
/// así que el navegador solo se lleva la cookie de sesión.
/// </summary>
public class AuthService
{
    private readonly ApiClient _api;
    private readonly IHttpContextAccessor _contexto;

    public AuthService(ApiClient api, IHttpContextAccessor contexto)
    {
        _api = api;
        _contexto = contexto;
    }

    public SesionUsuario? SesionActual => _contexto.HttpContext?.Session.ObtenerSesion();

    public bool HaySesion => SesionActual is not null;

    /// <summary>
    /// Doctor y administrador eligen el paciente de una lista (GET /pacientes,
    /// reservado a esos roles). Paciente no necesita esa lista: el propio
    /// IdPaciente sale de su perfil (PerfilService), nunca de un selector.
    /// </summary>
    public bool PuedeCargarTurnos => SesionActual?.Rol is "doctor" or "administrador" or "paciente";

    /// <summary>
    /// GET /pacientes está reservado a doctor y administrador. El detalle usa el
    /// mismo criterio: trae datos clínicos (alergias, condiciones) que no
    /// corresponde mostrarle a cualquier usuario autenticado.
    /// </summary>
    public bool PuedeVerPacientes => SesionActual?.Rol is "doctor" or "administrador";

    /// <summary>
    /// GET /doctores (listado, detalle y /me) no le exige ningún rol a la API más
    /// allá de estar autenticado — a diferencia de GET /pacientes, que sí reserva
    /// a doctor y administrador. Tiene sentido: nombre, especialidad, matrícula y
    /// consultorio son datos de un padrón profesional, no datos clínicos de una
    /// persona, y el paciente los necesita para elegir con quién agendar un turno.
    /// Se deja como propiedad (en vez de un chequeo implícito) para que la
    /// decisión quede documentada y trazable si el día de mañana la API cambia.
    /// </summary>
    public bool PuedeVerDoctores => HaySesion;

    /// <summary>
    /// POST /recetas y PUT /recetas/{id} los reserva la API al rol doctor.
    /// </summary>
    public bool PuedeEmitirRecetas => SesionActual?.Rol is "doctor";

    /// <summary>
    /// POST y PUT de historiales clínicos los reserva la API al rol doctor.
    /// </summary>
    public bool PuedeEscribirHistorial => SesionActual?.Rol is "doctor";

    /// <summary>
    /// Doctor y administrador pueden mirar las recetas de cualquier paciente;
    /// el paciente solo las suyas.
    /// </summary>
    public bool PuedeElegirPaciente => SesionActual?.Rol is "doctor" or "administrador";

    /// <summary>
    /// Ojo: DELETE /turnos/{id} no pide ningún rol, solo estar autenticado, así
    /// que esto es una restricción nuestra de interfaz y no la aplica la API.
    /// Se eligieron los roles del personal, los mismos que ya maneja el PUT
    /// (administrador y secretario) más doctor.
    /// </summary>
    public bool PuedeCancelarTurnos =>
        SesionActual?.Rol is "doctor" or "administrador" or "secretario";

    /// <summary>
    /// PUT /turnos/{id} es el único camino para mover el estado de un turno, y la
    /// API lo reserva a administrador y secretario (TurnoEndpoints, MapPut).
    /// Espejamos esa regla tal cual, pero conviene saber dos cosas: "secretario"
    /// hoy es inalcanzable, porque el registro solo acepta paciente, doctor y
    /// administrador, así que en la práctica esto es solo-administrador; y un
    /// doctor NO puede confirmar ni completar sus propios turnos, solo cancelarlos.
    /// </summary>
    public bool PuedeCambiarEstadoTurno =>
        SesionActual?.Rol is "administrador" or "secretario";

    /// <summary>
    /// POST /doctores solo lo acepta con este rol.
    /// </summary>
    public bool EsAdministrador => SesionActual?.Rol is "administrador";

    /// <summary>
    /// Registra un paciente nuevo y deja la sesión abierta...
    public Task<SesionUsuario> RegistrarPacienteAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono)
        => RegistrarYLoguearAsync(nombre, apellido, email, contrasena, telefono, "paciente");

    /// <summary>
    /// Alta de doctor por un administrador: solo crea el Usuario (rol "doctor"),
    /// sin loguear al admin como ese usuario nuevo. La API no crea la fila en
    /// Doctores acá — eso requiere un segundo paso, POST /doctores con el
    /// IdUsuario devuelto (ver DoctorService.CrearAsync).
    /// </summary>
    public async Task<int> RegistrarComoDoctorAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono)
        => (await RegistrarUsuarioAsync(nombre, apellido, email, contrasena, telefono, "doctor")).IdUsuario;

    /// <summary>
    /// Alta de otro administrador. A diferencia del doctor, acá no hay un
    /// segundo paso: la API no tiene tabla de perfil para administradores.
    /// </summary>
    public async Task RegistrarComoAdministradorAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono)
        => await RegistrarUsuarioAsync(nombre, apellido, email, contrasena, telefono, "administrador");

    /// <summary>
    /// Alta de paciente hecha por un administrador (por ejemplo, para alguien que
    /// no puede autogestionarse por la web). A diferencia de
    /// <see cref="RegistrarPacienteAsync"/>, no abre sesión como ese usuario: el
    /// admin sigue logueado con la suya. La API ya crea la fila en Pacientes sola
    /// (UsuarioLogica.Registrar), así que no hace falta un segundo paso.
    /// </summary>
    public async Task<int> RegistrarComoPacienteAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono)
        => (await RegistrarUsuarioAsync(nombre, apellido, email, contrasena, telefono, "paciente")).IdUsuario;

    private async Task<SesionUsuario> RegistrarYLoguearAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono, string rol)
    {
        var respuesta = await RegistrarUsuarioAsync(nombre, apellido, email, contrasena, telefono, rol);

        var sesion = new SesionUsuario(respuesta.Token, respuesta.Rol, respuesta.IdUsuario, nombre);
        _contexto.HttpContext!.Session.GuardarSesion(sesion);
        return sesion;
    }

    /// <summary>
    /// POST /usuarios/registro es AllowAnonymous y el campo Rol es un string
    /// libre que la API valida contra "paciente"/"doctor"/"administrador" sin
    /// mirar quién hace el pedido — quien le pegue directo a la API (no a
    /// través de este método) podría registrarse como administrador. Por eso
    /// <paramref name="rol"/> es privado a esta clase: los métodos públicos de
    /// arriba son los únicos que lo fijan, y siempre con una constante.
    /// </summary>
    private async Task<RegistroRespuesta> RegistrarUsuarioAsync(
        string nombre, string apellido, string email, string contrasena, string? telefono, string rol)
    {
        var respuesta = await _api.PostAsync<RegistroRespuesta>(
            "/usuarios/registro",
            new { nombre, apellido, email, contrasena, telefono, rol },
            anonimo: true);

        if (respuesta is null || string.IsNullOrEmpty(respuesta.Token))
            throw new ApiException("La API no devolvió una respuesta válida de registro.", 0);

        return respuesta;
    }
    /// <summary>
    /// Autentica contra la API y deja la sesión abierta.
    /// Tira <see cref="ApiException"/> si las credenciales no sirven o la API no responde.
    /// </summary>
    public async Task<SesionUsuario> LoginAsync(string email, string contrasena)
    {
        LoginRespuesta? respuesta;
        try
        {
            // anonimo: true porque todavía no hay token y porque un 401 acá
            // significa "credenciales incorrectas", no "sesión vencida".
            respuesta = await _api.PostAsync<LoginRespuesta>(
                "/usuarios/login",
                new { email, contrasena },
                anonimo: true);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status401Unauthorized)
        {
            // La API responde 401 sin cuerpo y a propósito no aclara si falló
            // el email o la contraseña.
            throw new ApiException("Email o contraseña incorrectos.", ex.Status);
        }

        if (respuesta is null || string.IsNullOrEmpty(respuesta.Token))
            throw new ApiException("La API no devolvió un token de sesión.", 0);

        var sesion = new SesionUsuario(
            respuesta.Token, respuesta.Rol, respuesta.IdUsuario, respuesta.Nombre);

        _contexto.HttpContext!.Session.GuardarSesion(sesion);
        return sesion;
    }

    public void Logout() => _contexto.HttpContext?.Session.CerrarSesion();
}
