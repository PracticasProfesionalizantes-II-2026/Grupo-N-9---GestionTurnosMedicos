using System.Globalization;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

public class TurnoFilaViewModel
{
    public int IdTurno { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Paciente { get; init; } = string.Empty;
    public string Doctor { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;

    /// <summary>
    /// Null cuando la API no mandó la hora. Viene en HoraInicio ("HH:mm"),
    /// aparte de FechaInicio, que el listado devuelve a las 00:00.
    /// </summary>
    public string? Hora { get; init; }

    /// <summary>Foto del paciente del turno. Null si no tiene: el avatar muestra las iniciales.</summary>
    public string? FotoUrl { get; init; }

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(Paciente);

    public string PacienteMostrado =>
        string.IsNullOrWhiteSpace(Paciente) ? "Paciente sin datos" : Paciente.Trim();

    public string DoctorMostrado =>
        string.IsNullOrWhiteSpace(Doctor) ? "Doctor sin asignar" : Doctor.Trim();

    public string? EspecialidadMostrada =>
        string.IsNullOrWhiteSpace(Especialidad) ? null : Especialidad.Trim();

    public string FechaCorta => FechaArgentina.Corta(FechaInicio);

    /// <summary>
    /// Para el paciente, el día como se dice: "hoy", "mañana" o "viernes 16 de
    /// octubre". La hora va aparte, en su propia columna.
    /// </summary>
    public string DiaParaPaciente => FechaArgentina.DiaNatural(FechaInicio, Ahora);

    /// <summary>
    /// Arma la fila a partir de lo que devuelve la API.
    /// </summary>
    public static TurnoFilaViewModel Desde(TurnoLista turno) => DesdeConFoto(turno, null);

    /// <summary>
    /// Igual que <see cref="Desde"/>, con la URL de la foto del paciente si la tiene.
    /// </summary>
    public static TurnoFilaViewModel DesdeConFoto(TurnoLista turno, string? fotoUrl) => new()
    {
        FotoUrl      = fotoUrl,
        IdTurno      = turno.IdTurno,
        FechaInicio  = turno.FechaInicio,
        Estado       = turno.Estado,
        Paciente     = turno.Paciente,
        Doctor       = turno.Doctor,
        Especialidad = turno.Especialidad,
        // La API manda la hora aparte de FechaInicio (que viene a las 00:00).
        // Si llega vacía no inventamos una: la vista muestra un guion.
        Hora = string.IsNullOrWhiteSpace(turno.HoraInicio) ? null : turno.HoraInicio
    };

    /// <summary>Ya está cancelado: no se ofrece la acción de cancelar.</summary>
    public bool EstaCancelado =>
        string.Equals(Estado, "cancelado", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Solo se ofrece cancelar un turno pendiente o confirmado, que son los
    /// únicos que la API acepta cancelar.
    /// </summary>
    public bool PuedeCancelarse =>
        string.Equals(Estado, "pendiente", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Estado, "confirmado", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Solo un turno pendiente se puede confirmar desde el listado. El resto de
    /// las transiciones se hacen desde el detalle, que tiene todo el contexto.
    /// </summary>
    public bool PuedeConfirmarse =>
        string.Equals(Estado, "pendiente", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Se puede marcar como completado o ausente: sigue en pie y ya llegó su
    /// hora. Es la misma regla que el detalle (TurnoDetalleViewModel).
    /// </summary>
    public bool PuedeCerrarse => PuedeCancelarse && YaEmpezo;

    /// <summary>La hora de ahora en Argentina; las pruebas pueden poner otra.</summary>
    public DateTime Ahora { get; init; } = FechaArgentina.Ahora();

    /// <summary>
    /// Ya llegó la hora de inicio del turno. El paciente no cancela un turno
    /// que ya empezó (la API tampoco lo deja), así que no se le ofrece.
    /// </summary>
    public bool YaEmpezo
    {
        get
        {
            var inicio = FechaInicio.Date;
            if (TimeSpan.TryParseExact(Hora, @"hh\:mm", CultureInfo.InvariantCulture, out var hora))
                inicio = inicio + hora;

            return Ahora >= inicio;
        }
    }
}

/// <summary>
/// Filtros del listado. Viajan por query string y vuelven a la vista para que
/// el formulario quede con lo que el usuario eligió.
/// </summary>
public class TurnosFiltroViewModel
{
    /// <summary>
    /// Los estados que maneja la API, en el orden en que se muestran las
    /// tarjetas.
    /// </summary>
    public static readonly string[] EstadosTurno =
        { "confirmado", "pendiente", "completado", "ausente", "cancelado" };

    /// <summary>Las columnas por las que se puede ordenar la tabla.</summary>
    public static readonly string[] Columnas = { "hora", "paciente", "fecha", "estado" };

    /// <summary>Lo que se aplica cuando la URL no trae una columna.</summary>
    public const string ColumnaPorDefecto = "fecha";

    public string? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    /// <summary>Columna de orden que vino en la URL. Null si no vino ninguna válida.</summary>
    public string? Orden { get; set; }

    /// <summary>La tabla va de mayor a menor (dir=desc en la URL).</summary>
    public bool Descendente { get; set; }

    /// <summary>
    /// La URL trae ver=todos: se muestran también los turnos pasados. Sin
    /// eso, y sin fechas elegidas, el listado arranca en hoy.
    /// </summary>
    public bool VerTodos { get; set; }

    /// <summary>La página que se está mirando. Empieza en 1.</summary>
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// La URL trae ver=sin-cerrar (solo el personal): los turnos de días
    /// anteriores que siguen pendientes o confirmados. En este modo no
    /// cuentan ni las tarjetas de estado ni las fechas.
    /// </summary>
    public bool SinCerrar { get; set; }

    /// <summary>Se muestran solo los turnos de hoy en adelante: es lo que se ve al entrar.</summary>
    public bool SoloProximos => !VerTodos && !HayFechas && !SinCerrar;

    /// <summary>
    /// La columna con la que se le pide el orden a la API. "Hora" ordena igual
    /// que "Fecha": por día y, dentro del día, por hora.
    /// </summary>
    public string OrdenParaApi
    {
        get
        {
            if (Orden == "paciente" || Orden == "estado")
                return Orden;

            return "fecha";
        }
    }

    /// <summary>La tabla está ordenada por esta columna (la flecha va en ella).</summary>
    public bool OrdenaPor(string columna) =>
        string.Equals(columna, Orden ?? ColumnaPorDefecto, StringComparison.OrdinalIgnoreCase);

    public bool HayAlguno => !string.IsNullOrWhiteSpace(Estado) || HayFechas || VerTodos;

    /// <summary>Hay un rango de fechas elegido: manda sobre "próximos" y "todos".</summary>
    public bool HayFechas => Desde is not null || Hasta is not null;

    /// <summary>El rango está al revés y por eso no va a traer nada.</summary>
    public bool RangoInvertido => Desde is not null && Hasta is not null && Desde > Hasta;

    /// <summary>La tarjeta de este estado es la que está filtrando la tabla.</summary>
    public bool EstaActivo(string estado) =>
        string.Equals(estado, Estado, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Enlace al listado con el estado que se le pasa (null para no filtrar
    /// por estado). Conserva las fechas, "ver todos" y el orden, y vuelve a la
    /// primera página.
    /// </summary>
    public IDictionary<string, string> Ruta(string? estado) =>
        Ruta(estado, Orden, Descendente, VerTodos, 1);

    /// <summary>
    /// Enlace de un encabezado: conserva el resto de los filtros. Tocar la
    /// columna que ya ordena la invierte; tocar otra arranca de menor a mayor.
    /// </summary>
    public IDictionary<string, string> RutaDeOrden(string columna) =>
        Ruta(Estado, columna, OrdenaPor(columna) && !Descendente, VerTodos, 1);

    /// <summary>El listado tal como está, en otra página (paginador y "volver").</summary>
    public IDictionary<string, string> RutaDePagina(int pagina) =>
        Ruta(Estado, Orden, Descendente, VerTodos, pagina);

    /// <summary>
    /// Pasa de "próximos" a "todos" o al revés. El orden vuelve al de cada
    /// modo: los próximos, del más cercano al más lejano; todos, del más
    /// nuevo al más viejo.
    /// </summary>
    public IDictionary<string, string> RutaVerTodos(bool verTodos) =>
        Ruta(Estado, null, false, verTodos, 1);

    private IDictionary<string, string> Ruta(string? estado, string? orden, bool descendente, bool verTodos, int pagina)
    {
        var ruta = new Dictionary<string, string>();

        // "Sin cerrar" solo conserva el orden y la página.
        if (SinCerrar)
        {
            ruta["ver"] = TurnosSinCerrar.Ver;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(estado)) ruta["estado"] = estado;
            if (Desde is { } desde) ruta["desde"] = desde.ToString("yyyy-MM-dd");
            if (Hasta is { } hasta) ruta["hasta"] = hasta.ToString("yyyy-MM-dd");
            if (verTodos) ruta["ver"] = "todos";
        }
        if (orden is not null) ruta["orden"] = orden;
        if (descendente) ruta["dir"] = "desc";
        if (pagina > 1) ruta["pagina"] = $"{pagina}";

        return ruta;
    }
}

public class TurnosIndexViewModel
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>Turnos por página.</summary>
    public const int PorPagina = 20;

    /// <summary>Las filas de la página actual, ya filtradas y ordenadas por la API.</summary>
    public IReadOnlyList<TurnoFilaViewModel> Turnos { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Cuántos turnos cumplen los filtros, sumando todas las páginas.</summary>
    public int Total { get; init; }

    /// <summary>
    /// Cuántos turnos hay de cada estado, sin el filtro de estado: son los
    /// números de las tarjetas. Null si la API no los mandó.
    /// </summary>
    public IReadOnlyDictionary<string, int>? Conteos { get; init; }

    /// <summary>Mensaje de error de la API, si la carga falló.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    public TurnosFiltroViewModel Filtros { get; init; } = new();

    /// <summary>Rol del usuario: define el título y el alcance de lo que se ve.</summary>
    public string? Rol { get; init; }

    /// <summary>Aviso no fatal, por ejemplo un usuario sin perfil asociado.</summary>
    public string? Aviso { get; init; }

    public string Titulo
    {
        get
        {
            if (Filtros.SinCerrar)
                return "Turnos sin cerrar";

            return Rol switch
            {
                "paciente" => "Mis turnos",
                "doctor"   => "Mi agenda",
                _          => "Turnos"
            };
        }
    }

    public string? Subtitulo
    {
        get
        {
            if (Filtros.SinCerrar)
                return "Turnos de días anteriores que quedaron pendientes o confirmados: marcá cada uno como completado o ausente.";

            return Rol switch
            {
                "paciente" => "Solo se muestran los turnos en los que figurás como paciente.",
                "doctor"   => "Solo se muestran los turnos que tenés asignados.",
                _          => null
            };
        }
    }

    public string TextoVacio => Rol switch
    {
        "paciente" => "Todavía no tenés ningún turno.",
        "doctor"   => "Todavía no tenés turnos asignados.",
        _          => "Cuando se registren turnos en el sistema van a aparecer en esta lista."
    };

    // Los números de las tarjetas, ya como texto: "—" si la API no los mandó.
    public string Confirmados => Conteo("confirmado");
    public string Pendientes  => Conteo("pendiente");
    public string Completados => Conteo("completado");
    public string Ausentes    => Conteo("ausente");
    public string Cancelados  => Conteo("cancelado");

    /// <summary>No hay turnos, y la tarjeta de un estado está filtrando.</summary>
    public bool SinTurnosDelEstado => Turnos.Count == 0 && Filtros.Estado is not null;

    /// <summary>No hay turnos en el rango de fechas elegido.</summary>
    public bool SinResultados => Turnos.Count == 0 && Filtros.HayFechas;

    /// <summary>No hay turnos de hoy en adelante (puede haber pasados).</summary>
    public bool SinProximos => Turnos.Count == 0 && Filtros.SoloProximos;

    /// <summary>Alguna fila vino sin hora desde la API.</summary>
    public bool FaltaHora => Turnos.Any(t => t.Hora is null);

    /// <summary>El día de hoy en hora de Argentina.</summary>
    public DateTime Hoy { get; init; } = FechaArgentina.Hoy();

    public string FechaDeHoy =>
        Hoy.ToString("D", Cultura);

    /// <summary>Lo que muestra el paginador compartido (Shared/_Paginador).</summary>
    public PaginadorViewModel Paginador => new()
    {
        Pagina = Filtros.Pagina,
        TotalPaginas = PaginadorViewModel.ContarPaginas(Total, PorPagina),
        RutaAnterior = Filtros.RutaDePagina(Filtros.Pagina - 1),
        RutaSiguiente = Filtros.RutaDePagina(Filtros.Pagina + 1)
    };

    private string Conteo(string estado)
    {
        if (Conteos is null)
            return "—";

        // La API manda todos los estados; si faltara alguno, es que no hay.
        return Conteos.TryGetValue(estado, out var cantidad) ? $"{cantidad}" : "0";
    }

    /// <summary>
    /// "Pedro Paciente" -> "PP". Devuelve "?" si el nombre vino vacío, que pasa
    /// cuando la API no pudo resolver el usuario de la relación.
    /// </summary>
    public static string CalcularIniciales(string nombre)
    {
        var partes = (nombre ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return "?";

        var iniciales = partes.Length == 1
            ? partes[0][..1]
            : string.Concat(partes[0][..1], partes[^1][..1]);

        return iniciales.ToUpperInvariant();
    }
}

/// <summary>
/// Los turnos "sin cerrar": de días anteriores y todavía pendientes o
/// confirmados, porque nadie los marcó como completados o ausentes. Los usan
/// el listado (?ver=sin-cerrar) y el aviso del inicio del personal.
/// </summary>
public static class TurnosSinCerrar
{
    /// <summary>El valor de ?ver= en la URL del listado.</summary>
    public const string Ver = "sin-cerrar";

    /// <summary>Los estados que cuentan: los que siguen en pie.</summary>
    public const string Estados = "pendiente,confirmado";

    /// <summary>Hasta ayer: los de hoy están en la agenda del día.</summary>
    public static DateTime Hasta(DateTime hoy) => hoy.Date.AddDays(-1);

    /// <summary>
    /// El texto del aviso del inicio. Null si no hay ninguno (no sale el
    /// aviso). El doctor lee "Tenés" y la administración "Hay".
    /// </summary>
    public static string? Aviso(int? cantidad, bool esDoctor)
    {
        if (cantidad is null || cantidad <= 0)
            return null;

        var inicio = esDoctor ? "Tenés" : "Hay";
        return cantidad == 1
            ? $"{inicio} 1 turno de días anteriores sin cerrar."
            : $"{inicio} {cantidad} turnos de días anteriores sin cerrar.";
    }

    /// <summary>El enlace del aviso: el listado en modo "sin cerrar".</summary>
    public static EnlaceViewModel Enlace => new()
    {
        Controlador = "Turnos",
        Accion = "Index",
        Ruta = new Dictionary<string, string> { ["ver"] = Ver },
        Descripcion = "Revisarlos"
    };
}
