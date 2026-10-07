using System.Net;
using System.Threading.RateLimiting;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Tope de intentos de login por IP, para que no se puedan probar contraseñas
/// sin límite desde un mismo origen. El contador vive en memoria, igual que la
/// sesión: se reinicia con la aplicación y no se comparte entre instancias.
/// </summary>
public sealed class LimiteLogin : IDisposable
{
    // Cupo holgado a propósito: detrás de una misma IP puede haber varias
    // personas (un aula, una oficina) y comparten el contador.
    private const int IntentosPorVentana = 20;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(5);

    private const string IpDesconocida = "desconocida";

    // Una partición por IP. El limitador descarta solo las que quedan
    // inactivas, así que la memoria no crece sin tope.
    private readonly PartitionedRateLimiter<string> _limitador =
        PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = IntentosPorVentana,
                Window = Ventana,
                // Sin cola: el intento que no entra se rechaza en el momento.
                QueueLimit = 0
            }));

    /// <summary>
    /// Cuenta un intento para esa IP y dice si entra en el cupo. Cuentan todos
    /// los intentos, no solo los fallidos.
    /// </summary>
    public bool PermiteIntento(IPAddress? ip)
    {
        using var permiso = _limitador.AttemptAcquire(ip?.ToString() ?? IpDesconocida);
        return permiso.IsAcquired;
    }

    public void Dispose() => _limitador.Dispose();
}
