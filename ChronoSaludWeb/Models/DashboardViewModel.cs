namespace ChronoSaludWeb.Models;

/// <summary>Qué datos muestra cada fila del panel de turnos.</summary>
public enum VistaPanel
{
    /// <summary>El paciente ve con qué doctor y qué día.</summary>
    Paciente,
    /// <summary>El doctor ve a quién atiende y qué día.</summary>
    Doctor,
    /// <summary>El administrativo ve paciente y doctor.</summary>
    Administrador
}

/// <summary>
/// Cómo se acomodan las tarjetas del tablero. Se nombra por la forma y no por
/// el rol para que el que no tiene tablero propio caiga en algo razonable.
/// </summary>
public enum DisposicionDashboard
{
    /// <summary>Resumen y accesos a la izquierda, panel de turnos a la derecha.</summary>
    PanelDerecha,
    /// <summary>Accesos en una fila arriba, panel de turnos a todo el ancho abajo.</summary>
    PanelAbajo,
    /// <summary>Panel de turnos y accesos a la izquierda, actividad reciente a la derecha.</summary>
    ActividadDerecha
}

/// <summary>
/// Destino del botón "…" de una tarjeta: lleva a la pantalla que muestra todo
/// lo que la tarjeta resume.
/// </summary>
public class EnlaceViewModel
{
    public required string Controlador { get; init; }
    public required string Accion { get; init; }
    public IDictionary<string, string>? Ruta { get; init; }

    /// <summary>
    /// Qué se va a ver al seguirlo, escrito para entenderse solo ("Ver todos
    /// mis turnos"). Es el texto del enlace o del botón; donde solo hay lugar
    /// para un "…" va en un sr-only, porque tres puntos no le dicen nada a un
    /// lector de pantalla.
    /// </summary>
    public required string Descripcion { get; init; }
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

    /// <summary>
    /// Por qué está deshabilitada. A la vista va la etiqueta "Próximamente";
    /// este texto lo lee el lector de pantalla.
    /// </summary>
    public string? Motivo { get; init; }

    public bool Habilitado => Controlador is not null && Accion is not null;
}

/// <summary>Una tarjeta de número del tablero administrativo.</summary>
public class MetricaViewModel
{
    public required string Titulo { get; init; }
    public required int Valor { get; init; }

    /// <summary>Clave del ícono; el partial la traduce a un SVG.</summary>
    public required string Icono { get; init; }

    public EnlaceViewModel? VerMas { get; init; }
}

/// <summary>
/// Lo que va debajo del saludo en el banner del inicio: una frase con una
/// parte resaltada y hasta dos botones. Las tres partes de la frase se
/// escriben seguidas, así que <see cref="Cierre"/> trae su propio espacio
/// inicial cuando lo necesita (" con Ana Pérez." o solo ".").
/// </summary>
public class BannerViewModel
{
    public string? Resumen { get; init; }

    /// <summary>La parte que va en negrita, ej. "jueves 8 de octubre a las 10:30".</summary>
    public string? Destacado { get; init; }

    public string? Cierre { get; init; }

    /// <summary>La acción más probable para este rol. Va como botón primario.</summary>
    public EnlaceViewModel? Principal { get; init; }

    public EnlaceViewModel? Secundario { get; init; }
}

/// <summary>
/// Modelo del partial _EstadoVacio: ícono en círculo, título, una línea de
/// ayuda y un botón que lleva a la acción que resuelve el vacío.
/// </summary>
public class EstadoVacioViewModel
{
    /// <summary>Clave del ícono; el partial la traduce a un SVG.</summary>
    public required string Icono { get; init; }

    public required string Titulo { get; init; }
    public string? Ayuda { get; init; }
    public EnlaceViewModel? Accion { get; init; }
}

public class PanelTurnosViewModel
{
    public required string Titulo { get; init; }
    public required VistaPanel Vista { get; init; }
    public IReadOnlyList<TurnoFilaViewModel> Turnos { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Título del estado vacío, propio de cada rol.</summary>
    public required string TextoVacio { get; init; }

    /// <summary>Línea de ayuda debajo del título del estado vacío.</summary>
    public string? AyudaVacio { get; init; }

    /// <summary>Botón del estado vacío: adónde ir cuando no hay turnos.</summary>
    public EnlaceViewModel? AccionVacio { get; init; }

    public EnlaceViewModel? VerMas { get; init; }
}

/// <summary>
/// Una línea del historial reciente del paciente: una consulta de la historia
/// clínica o una receta emitida.
/// </summary>
public class ActividadRecienteViewModel
{
    /// <summary>"Consulta" o "Receta".</summary>
    public required string Tipo { get; init; }

    public required DateTime Fecha { get; init; }

    public string FechaCorta =>
        Fecha.ToString("d 'de' MMMM", TurnosIndexViewModel.Cultura);
}

/// <summary>
/// La tarjeta de historial reciente. Tiene Error propio porque se arma con dos
/// llamadas que pueden fallar sin que eso deba voltear el tablero entero.
/// </summary>
public class PanelActividadViewModel
{
    public required string Titulo { get; init; }
    public IReadOnlyList<ActividadRecienteViewModel> Entradas { get; init; } = Array.Empty<ActividadRecienteViewModel>();
    public required string TextoVacio { get; init; }

    /// <summary>Línea de ayuda debajo del título del estado vacío.</summary>
    public string? AyudaVacio { get; init; }

    /// <summary>Botón del estado vacío.</summary>
    public EnlaceViewModel? AccionVacio { get; init; }

    /// <summary>Mensaje si la API falló al traer historial o recetas.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    public EnlaceViewModel? VerMas { get; init; }
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

    /// <summary>Acción sugerida junto al aviso, ej. "Completar mi perfil".</summary>
    public EnlaceViewModel? AvisoEnlace { get; init; }

    /// <summary>
    /// Frase y botones del banner de bienvenida. Null cuando el rol no tiene
    /// nada que resumir: el banner muestra solo el saludo.
    /// </summary>
    public BannerViewModel? Banner { get; init; }

    public DisposicionDashboard Disposicion { get; init; } = DisposicionDashboard.PanelDerecha;

    public IReadOnlyList<AccesoRapidoViewModel> Accesos { get; init; } = Array.Empty<AccesoRapidoViewModel>();

    public PanelTurnosViewModel? Panel { get; init; }

    /// <summary>Historial reciente del paciente. Null en los otros roles.</summary>
    public PanelActividadViewModel? Actividad { get; init; }

    /// <summary>Tarjetas de número. Vacío cuando el rol no las tiene.</summary>
    public IReadOnlyList<MetricaViewModel> Metricas { get; init; } = Array.Empty<MetricaViewModel>();

    public bool HayMetricas => Metricas.Count > 0;

    public string Saludo =>
        string.IsNullOrWhiteSpace(Nombre) ? "Hola" : $"Hola, {Nombre.Trim()}";
}
