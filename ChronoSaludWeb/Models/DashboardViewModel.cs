namespace ChronoSaludWeb.Models;

/// <summary>Qué datos muestra cada fila del panel de turnos.</summary>
public enum VistaPanel
{
    /// <summary>El paciente ve con qué doctor y qué día.</summary>
    Paciente,
    /// <summary>El doctor ve a quién atiende.</summary>
    Doctor,
    /// <summary>El administrativo ve paciente y doctor.</summary>
    Administrador
}

/// <summary>
/// Una tarjeta de la grilla de accesos rápidos. Sin Controlador queda
/// deshabilitada: es para las secciones que todavía no existen.
/// </summary>
public class AccesoRapidoViewModel
{
    public required string Titulo { get; init; }

    /// <summary>Clave del ícono; el partial la traduce a un SVG.</summary>
    public required string Icono { get; init; }

    public string? Controlador { get; init; }
    public string? Accion { get; init; }
    public IDictionary<string, string>? Ruta { get; init; }

    /// <summary>Por qué está deshabilitada. Se muestra como tooltip.</summary>
    public string? Motivo { get; init; }

    public bool Habilitado => Controlador is not null && Accion is not null;
}

public class PanelTurnosViewModel
{
    public required string Titulo { get; init; }
    public required VistaPanel Vista { get; init; }
    public IReadOnlyList<TurnoFilaViewModel> Turnos { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Texto del estado vacío, propio de cada rol.</summary>
    public required string TextoVacio { get; init; }
}

public class DashboardViewModel
{
    public string? Rol { get; init; }
    public string? Nombre { get; init; }

    public bool HaySesion => Rol is not null;

    /// <summary>La API falló al armar el dashboard.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    /// <summary>
    /// Aviso no fatal: por ejemplo un doctor cuyo usuario todavía no tiene
    /// perfil de doctor cargado.
    /// </summary>
    public string? Aviso { get; init; }

    public IReadOnlyList<AccesoRapidoViewModel> Accesos { get; init; } = Array.Empty<AccesoRapidoViewModel>();

    public PanelTurnosViewModel? Panel { get; init; }

    // Métricas del dashboard administrativo. Null cuando no aplican.
    public int? TurnosDeHoy { get; init; }
    public int? TotalPacientes { get; init; }

    public bool HayMetricas => TurnosDeHoy is not null || TotalPacientes is not null;

    public string FechaDeHoy =>
        DateTime.Today.ToString("D", TurnosIndexViewModel.Cultura);

    public string Saludo =>
        string.IsNullOrWhiteSpace(Nombre) ? "Hola" : $"Hola, {Nombre.Trim()}";
}
