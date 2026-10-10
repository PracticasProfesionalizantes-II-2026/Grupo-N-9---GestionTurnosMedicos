namespace ChronoSaludApi.Logica;

public interface IRecuperarContrasenaLogica
{
    /// <summary>
    /// Si hay un usuario activo con ese email, le manda el enlace para crear
    /// una contraseña nueva. Si no, no hace nada: quien llama no se entera,
    /// para no revelar qué emails están registrados.
    /// </summary>
    Task Pedir(string email);

    /// <summary>Cambia la contraseña con el token del enlace. Devuelve el error, o null si salió bien.</summary>
    Task<string?> Restablecer(string token, string contrasenaNueva);
}
