using System.Threading.RateLimiting;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Tope de intentos para el login y para el cambio de contraseña, así no se
/// pueden probar contraseñas sin límite contra una cuenta.
///
/// Cuenta por email (login) y por usuario (cambio de contraseña), no por IP:
/// a la API los pedidos le llegan desde el servidor de la Web, así que la IP
/// sería la misma para todos y un tope por IP trabaría a todo el mundo. El
/// tope por IP de cada persona ya lo pone la Web (LimiteLogin).
///
/// Es la misma idea que LimiteLogin de la Web: el contador vive en memoria,
/// se reinicia con la aplicación y cuentan todos los intentos, no solo los
/// fallidos.
/// </summary>
public sealed class LimiteIntentos : IDisposable
{
    /// <summary>Lo que responde la API cuando se pasa el tope (429).</summary>
    public const string Mensaje = "Demasiados intentos. Esperá unos minutos y volvé a probar.";

    // Login: 10 intentos cada 15 minutos por email. Cambio de contraseña:
    // 5 cada 15 minutos por usuario.
    private readonly PartitionedRateLimiter<string> _login = Crear(10, TimeSpan.FromMinutes(15));
    private readonly PartitionedRateLimiter<string> _contrasena = Crear(5, TimeSpan.FromMinutes(15));

    /// <summary>Cuenta un intento de login con ese email y dice si entra en el cupo.</summary>
    public bool PermiteLogin(string email) =>
        Permite(_login, email.Trim().ToLowerInvariant());

    /// <summary>Cuenta un intento de cambio de contraseña de ese usuario y dice si entra en el cupo.</summary>
    public bool PermiteCambioDeContrasena(int idUsuario) =>
        Permite(_contrasena, $"{idUsuario}");

    private static bool Permite(PartitionedRateLimiter<string> limitador, string clave)
    {
        using var permiso = limitador.AttemptAcquire(clave);
        return permiso.IsAcquired;
    }

    /// <summary>
    /// Un limitador con una "partición" por clave (un contador por email o por
    /// usuario), de ventana fija: cada tanto tiempo el contador vuelve a cero.
    /// Descarta solo las particiones que quedan sin uso, así que la memoria no
    /// crece sin tope.
    /// </summary>
    private static PartitionedRateLimiter<string> Crear(int intentos, TimeSpan ventana) =>
        PartitionedRateLimiter.Create<string, string>(clave =>
            RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = intentos,
                Window = ventana,
                // Sin cola: el intento que no entra se rechaza en el momento.
                QueueLimit = 0
            }));

    public void Dispose()
    {
        _login.Dispose();
        _contrasena.Dispose();
    }
}
