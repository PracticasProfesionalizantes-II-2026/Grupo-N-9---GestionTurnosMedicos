using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

/// <summary>
/// "Olvidé mi contraseña": pedir el enlace por email y crear la contraseña
/// nueva con ese enlace. El enlace lleva un token firmado que vence en una
/// hora y se usa una sola vez (ver <see cref="TokenDeRecuperacion"/>), así que
/// no hace falta guardar nada en la base.
/// </summary>
public class RecuperarContrasenaLogica : IRecuperarContrasenaLogica
{
    public const string TokenInvalido = "El enlace venció o ya se usó. Pedí uno nuevo.";

    private readonly IUsuarioRepository _repo;
    private readonly IReloj _reloj;
    private readonly IConfiguration _config;
    private readonly IEnviadorDeCorreo _correo;

    public RecuperarContrasenaLogica(IUsuarioRepository repo, IReloj reloj, IConfiguration config, IEnviadorDeCorreo correo)
    {
        _repo = repo;
        _reloj = reloj;
        _config = config;
        _correo = correo;
    }

    public async Task Pedir(string email)
    {
        var usuario = await _repo.ObtenerPorEmail(email.Trim());

        // Sin usuario (o dado de baja) no se manda nada, y quien llama recibe
        // la misma respuesta que si existiera.
        if (usuario is null || !usuario.Activo)
            return;

        var token = TokenDeRecuperacion.Crear(usuario.Id, usuario.Contrasena, _reloj.Ahora(), Clave());
        var urlWeb = (_config["Recuperacion:UrlWeb"] ?? "http://localhost:5044").TrimEnd('/');
        var enlace = $"{urlWeb}/Cuenta/Restablecer?token={Uri.EscapeDataString(token)}";

        await _correo.Enviar(
            usuario.Email,
            "Creá tu contraseña nueva de ChronoSalud",
            $"Hola, {usuario.Nombre}:\n\n" +
            "Pediste crear una contraseña nueva. Entrá a este enlace (vale una hora y se usa una sola vez):\n\n" +
            $"{enlace}\n\n" +
            "Si no fuiste vos, ignorá este mensaje: tu contraseña sigue siendo la misma.");
    }

    public async Task<string?> Restablecer(string token, string contrasenaNueva)
    {
        // La misma regla que el cambio de contraseña desde Mi perfil.
        if (string.IsNullOrEmpty(contrasenaNueva) || contrasenaNueva.Length < 8)
            return "La contraseña nueva debe tener al menos 8 caracteres.";

        var idUsuario = TokenDeRecuperacion.LeerIdUsuario(token);
        if (idUsuario is null)
            return TokenInvalido;

        var usuario = await _repo.ObtenerPorId(idUsuario.Value);
        if (usuario is null || !usuario.Activo)
            return TokenInvalido;

        if (!TokenDeRecuperacion.EsValido(token, usuario.Id, usuario.Contrasena, _reloj.Ahora(), Clave()))
            return TokenInvalido;

        // Al cambiar el hash, este mismo enlace deja de servir.
        usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasenaNueva);
        await _repo.Actualizar(usuario);
        return null;
    }

    /// <summary>
    /// La clave con la que se firman los enlaces. Conviene una propia
    /// (Recuperacion:Clave; en Azure, Recuperacion__Clave). Si no está, se usa
    /// la del JWT, que también es secreta y ya existe.
    /// </summary>
    private string Clave()
    {
        var propia = _config["Recuperacion:Clave"];
        return string.IsNullOrWhiteSpace(propia) ? _config["Jwt:Key"]! : propia;
    }
}
