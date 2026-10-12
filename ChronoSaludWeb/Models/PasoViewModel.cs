namespace ChronoSaludWeb.Models;

/// <summary>En qué punto está un paso de un recorrido (pedir un turno).</summary>
public enum EstadoPaso
{
    Hecho,
    Actual,
    Pendiente
}

/// <summary>
/// Modelo del partial _Pasos: el título de un paso con su número en un
/// círculo, que se llena con un tilde cuando el paso ya está hecho.
/// </summary>
public class PasoViewModel
{
    public required int Numero { get; init; }
    public required string Titulo { get; init; }
    public required EstadoPaso Estado { get; init; }

    public static EstadoPaso Para(int numero, int pasoActual) =>
        numero < pasoActual ? EstadoPaso.Hecho
        : numero == pasoActual ? EstadoPaso.Actual
        : EstadoPaso.Pendiente;
}
