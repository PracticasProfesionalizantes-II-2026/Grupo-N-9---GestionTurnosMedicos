using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Manda por correo el mismo aviso que el paciente ve en la campana, con un
/// saludo y el enlace a la pantalla de la Web que corresponde. Solo a cuentas
/// activas y con email. Lo usan los avisos de turnos y de estudios, y el
/// recordatorio del día.
///
/// La dirección de la Web sale de Recuperacion:UrlWeb, la misma que usa
/// "Olvidé mi contraseña".
/// </summary>
public class AvisosPorCorreo
{
    private readonly IEnviadorDeCorreo _correo;
    private readonly IConfiguration _config;

    public AvisosPorCorreo(IEnviadorDeCorreo correo, IConfiguration config)
    {
        _correo = correo;
        _config = config;
    }

    public string UrlDeLaWeb => (_config["Recuperacion:UrlWeb"] ?? "http://localhost:5044").TrimEnd('/');

    /// <summary>
    /// <paramref name="ruta"/> es la pantalla de la Web a la que lleva el
    /// enlace, por ejemplo "/Turnos" o "/Estudios".
    /// </summary>
    public Task Avisar(Usuario usuario, string asunto, string mensaje, string ruta) =>
        Enviar(usuario, asunto, CuerpoDeAviso(usuario, mensaje, $"{UrlDeLaWeb}{ruta}"));

    /// <summary>Un correo con el cuerpo armado por quien llama (el recordatorio).</summary>
    public Task Enviar(Usuario usuario, string asunto, string cuerpo)
    {
        if (!usuario.Activo || string.IsNullOrWhiteSpace(usuario.Email))
            return Task.CompletedTask;

        return _correo.Enviar(usuario.Email, asunto, cuerpo);
    }

    public static string CuerpoDeAviso(Usuario usuario, string mensaje, string enlace) =>
        $"Hola, {usuario.Nombre}:\n\n" +
        $"{mensaje}\n\n" +
        $"Podés ver el detalle en ChronoSalud: {enlace}";
}
