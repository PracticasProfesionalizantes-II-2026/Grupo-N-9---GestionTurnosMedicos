namespace ChronoSaludWeb.Services;

/// <summary>
/// La cantidad de notificaciones sin leer que muestra la campana, guardada en
/// la sesión por un minuto: así no se le pregunta a la API en cada página.
/// Se borra cuando el usuario marca alguna como leída, para que la campana se
/// actualice enseguida.
/// </summary>
public static class ContadorDeNoLeidas
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(60);

    private const string Clave = "chronosalud.noLeidas";

    /// <summary>Se guarda como "cantidad|momento", con el momento en ticks UTC.</summary>
    public static void Guardar(ISession sesion, int cantidad, DateTime ahoraUtc)
        => sesion.SetString(Clave, $"{cantidad}|{ahoraUtc.Ticks}");

    /// <summary>La cantidad guardada, o null si no hay o si ya pasó el minuto.</summary>
    public static int? Leer(ISession sesion, DateTime ahoraUtc)
    {
        var texto = sesion.GetString(Clave);
        if (string.IsNullOrEmpty(texto))
            return null;

        var partes = texto.Split('|');
        if (partes.Length != 2 || !int.TryParse(partes[0], out var cantidad) || !long.TryParse(partes[1], out var ticks))
            return null;

        var guardado = new DateTime(ticks, DateTimeKind.Utc);
        if (ahoraUtc - guardado >= Vigencia)
            return null;

        return cantidad;
    }

    public static void Borrar(ISession sesion) => sesion.Remove(Clave);
}
