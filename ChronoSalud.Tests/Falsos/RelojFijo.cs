using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza al reloj de verdad: la hora es la que pone la prueba. Si no pone
/// ninguna, es hoy a las 10:00, así las fechas que arman las pruebas con
/// DateTime.Today siguen siendo "hoy", "mañana" o "ayer".
/// </summary>
public class RelojFijo : IReloj
{
    public DateTime Momento { get; set; } = DateTime.Today.AddHours(10);

    public DateTime Ahora() => Momento;
}
