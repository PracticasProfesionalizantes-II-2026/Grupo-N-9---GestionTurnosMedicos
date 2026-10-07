namespace ChronoSaludApi.Logica;

/// <summary>
/// "Hoy" en hora de Argentina. DateTime.Today usa el reloj del servidor, y con
/// el servidor en UTC desde las 21:00 de acá ya es mañana: un turno de hoy
/// pasaría a contarse como de ayer.
/// </summary>
public static class FechaArgentina
{
    public static readonly TimeZoneInfo Zona = BuscarZona();

    /// <summary>El día que es ahora en Argentina, a las 00:00.</summary>
    public static DateTime Hoy() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona).Date;

    /// <summary>
    /// El id de la zona cambia según el sistema: IANA en Linux, el nombre de
    /// Windows en Windows. Si no está ninguno de los dos se arma una fija en
    /// UTC-3, que es correcta porque Argentina no cambia la hora desde 2009.
    /// </summary>
    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Argentina", TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }
}
