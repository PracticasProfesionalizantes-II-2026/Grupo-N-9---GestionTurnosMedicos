namespace ChronoSaludWeb.Services;

/// <summary>
/// "Hoy" en hora de Argentina. DateTime.Today usa el reloj del servidor, y con
/// el servidor en UTC desde las 21:00 de acá ya es mañana: los turnos de hoy
/// pasarían a contarse como pasados.
/// </summary>
public static class FechaArgentina
{
    public static readonly TimeZoneInfo Zona = BuscarZona();

    public static DateTime Hoy() => Hoy(DateTime.UtcNow);

    /// <summary>La fecha y la hora de ahora en Argentina.</summary>
    public static DateTime Ahora() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    /// <summary>
    /// El día que es en Argentina en ese instante UTC, a las 00:00. Recibe la
    /// hora por parámetro para poder probar un instante puntual (por ejemplo
    /// las 23:30 de acá, que en UTC ya es el día siguiente).
    /// </summary>
    public static DateTime Hoy(DateTime utcAhora) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcAhora, DateTimeKind.Utc), Zona).Date;

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
