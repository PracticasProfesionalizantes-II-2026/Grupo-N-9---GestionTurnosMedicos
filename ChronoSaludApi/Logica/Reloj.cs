namespace ChronoSaludApi.Logica;

/// <summary>
/// La hora actual. Es una interfaz para que las pruebas puedan usar una hora
/// fija (RelojFijo) en vez de la de verdad.
/// </summary>
public interface IReloj
{
    /// <summary>La fecha y la hora de ahora, en Argentina.</summary>
    DateTime Ahora();
}

/// <summary>
/// El reloj de verdad: la hora de Argentina, corra donde corra el servidor
/// (Azure está en UTC, tres horas adelante).
/// </summary>
public class RelojArgentina : IReloj
{
    public DateTime Ahora() => FechaArgentina.Ahora();
}
