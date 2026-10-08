namespace ChronoSaludApi.Logica;

/// <summary>
/// Los estados de un turno y los cambios permitidos entre ellos:
///   pendiente  → confirmado
///   pendiente o confirmado → completado o ausente (solo si el turno ya empezó)
///   pendiente o confirmado → cancelado
///   completado, ausente y cancelado son finales: no cambian más.
/// </summary>
public static class EstadosTurno
{
    public const string Pendiente  = "pendiente";
    public const string Confirmado = "confirmado";
    public const string Completado = "completado";
    public const string Ausente    = "ausente";
    public const string Cancelado  = "cancelado";

    public static readonly string[] Todos = { Pendiente, Confirmado, Completado, Ausente, Cancelado };

    /// <summary>Pendiente o confirmado: el turno todavía puede pasar.</summary>
    public static bool EstaEnPie(string estado) => estado == Pendiente || estado == Confirmado;

    /// <summary>
    /// Si un turno puede pasar del estado <paramref name="actual"/> al
    /// <paramref name="nuevo"/>. <paramref name="yaEmpezo"/> dice si ya llegó
    /// la hora de inicio del turno. Si no puede, devuelve el motivo.
    /// </summary>
    public static (bool ok, string? error) PuedeCambiar(string actual, string nuevo, bool yaEmpezo)
    {
        if (!Todos.Contains(nuevo))
            return (false, "Ese estado no existe. Los estados son: pendiente, confirmado, completado, ausente y cancelado.");

        // Pedir el mismo estado que ya tiene no cambia nada.
        if (nuevo == actual)
            return (true, null);

        if (!EstaEnPie(actual))
            return (false, $"Conflicto de estado: el turno está {actual} y ya no cambia de estado.");

        if (nuevo == Pendiente)
            return (false, "Conflicto de estado: un turno confirmado no vuelve a pendiente.");

        if ((nuevo == Completado || nuevo == Ausente) && !yaEmpezo)
            return (false, "Conflicto de estado: todavía no es la hora del turno. Se puede marcar como " +
                           "completado o ausente recién cuando empieza.");

        // Lo que queda está permitido: pendiente → confirmado, o pasar a
        // completado, ausente o cancelado.
        return (true, null);
    }
}
