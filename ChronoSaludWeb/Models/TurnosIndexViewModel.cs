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

    public string FechaCorta =>
        FechaInicio.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);

    /// <summary>
    /// Fecha y hora juntas, que es por lo que se ordena. Sin hora (o con una
    /// que no se entiende) queda a las 00:00 de su día. Null si la API no mandó
    /// la fecha: esa fila no tiene dónde ubicarse en el tiempo.
    /// </summary>
    public DateTime? Momento
    {
        get
        {
            if (FechaInicio == default) return null;

            return TimeSpan.TryParseExact(Hora, @"hh\:mm", CultureInfo.InvariantCulture, out var hora)
                ? FechaInicio.Date + hora
                : FechaInicio.Date;
        }
    }

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
    /// Solo un turno pendiente se puede confirmar desde el listado. El resto de
    /// las transiciones se hacen desde el detalle, que tiene todo el contexto.
    /// </summary>
    public bool PuedeConfirmarse =>
        string.Equals(Estado, "pendiente", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Filtros del listado. Viajan por query string y vuelven a la vista para que
/// el formulario quede con lo que el usuario eligió.
/// </summary>
public class TurnosFiltroViewModel
{
    /// <summary>
    /// Los cuatro estados que maneja la API, en el orden en que se muestran
    /// las tarjetas. Es también el orden fijo de la columna Estado.
    /// </summary>
    public static readonly string[] EstadosTurno =
        { "confirmado", "pendiente", "completado", "cancelado" };

    /// <summary>Las columnas por las que se puede ordenar la tabla.</summary>
    public static readonly string[] Columnas = { "hora", "paciente", "fecha", "estado" };

    /// <summary>Lo que se aplica cuando la URL no trae una columna.</summary>
    public const string ColumnaPorDefecto = "fecha";

    public string? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    /// <summary>Columna de orden que vino en la URL. Null si no vino ninguna válida.</summary>
    public string? Orden { get; set; }

    /// <summary>La URL trae dir=desc.</summary>
    public bool Descendente { get; set; }

    /// <summary>La tabla está ordenada por esta columna (la flecha va en ella).</summary>
    public bool OrdenaPor(string columna) =>
        string.Equals(columna, Orden ?? ColumnaPorDefecto, StringComparison.OrdinalIgnoreCase);

    public bool HayAlguno => !string.IsNullOrWhiteSpace(Estado) || HayFechas;

    /// <summary>Hay un rango de fechas: es lo único que achica lo que se le pide a la API.</summary>
    public bool HayFechas => Desde is not null || Hasta is not null;

    /// <summary>El rango está al revés y por eso no va a traer nada.</summary>
    public bool RangoInvertido => Desde is not null && Hasta is not null && Desde > Hasta;

    /// <summary>La tarjeta de este estado es la que está filtrando la tabla.</summary>
    public bool EstaActivo(string estado) =>
        string.Equals(estado, Estado, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parámetros de un enlace al listado que conserva las fechas y el orden
    /// elegidos y lleva el estado que se le pasa (null para no filtrar por
    /// estado). Los que están en null no viajan.
    /// </summary>
    public IDictionary<string, string> Ruta(string? estado) => Ruta(estado, Orden, Descendente);

    /// <summary>
    /// Parámetros del enlace de un encabezado: conserva el estado y las fechas.
    /// Tocar la columna que ya ordena la invierte; tocar otra arranca en
    /// ascendente.
    /// </summary>
    public IDictionary<string, string> RutaDeOrden(string columna) =>
        Ruta(Estado, columna, OrdenaPor(columna) && !Descendente);

    private IDictionary<string, string> Ruta(string? estado, string? orden, bool descendente)
    {
        var ruta = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(estado)) ruta["estado"] = estado;
        if (Desde is { } desde) ruta["desde"] = desde.ToString("yyyy-MM-dd");
        if (Hasta is { } hasta) ruta["hasta"] = hasta.ToString("yyyy-MM-dd");
        if (orden is not null) ruta["orden"] = orden;
        if (descendente) ruta["dir"] = "desc";

        return ruta;
    }
}

/// <summary>
/// Orden de la tabla de turnos. Es una función pura: no lee el reloj ni la
/// sesión, todo lo que necesita le llega por parámetro.
///
/// Idea común a las cuatro columnas: cada una define BLOQUES y un orden
/// DENTRO de cada bloque. Los bloques nunca cambian de lugar; "descendente"
/// invierte solamente el orden interno. El último criterio es siempre IdTurno
/// ascendente (también en descendente), para que dos filas empatadas salgan
/// igual en cada carga.
///
/// FECHA y HORA (el mismo comparador; también es lo que se aplica cuando
/// "orden" es null o no se reconoce). Se ordena por Momento, nunca por el
/// texto que se muestra.
///   Bloque 0: Momento.Date >= hoy. Asc: del más próximo al más lejano.
///             Desc: del más lejano al más próximo.
///   Bloque 1: Momento.Date &lt; hoy. Asc: del más reciente al más viejo.
///             Desc: del más viejo al más reciente.
///   Bloque 2: Momento null. Solo por IdTurno, en las dos direcciones.
///   La comparación con hoy es por DÍA: un turno de hoy a las 9:00 sigue en
///   el bloque 0 aunque ya sean las 18:00.
///
/// PACIENTE.
///   Bloque 0: con nombre. Asc: A a Z. Desc: Z a A. Se compara Paciente con
///             Trim() usando StringComparer.Create(TurnosIndexViewModel.Cultura,
///             ignoreCase: true), que ubica "Álvarez" junto a "Alvarez" y la ñ
///             después de la n.
///   Bloque 1: Paciente vacío o solo espacios. Solo por IdTurno.
///   Desempate: las reglas de Fecha en ascendente, y después IdTurno.
///
/// ESTADO.
///   Bloque 0: estado conocido. Asc: el orden de
///             TurnosFiltroViewModel.EstadosTurno (confirmado, pendiente,
///             completado, cancelado). Desc: al revés. Sin distinguir
///             mayúsculas.
///   Bloque 1: estado que no está en esa lista. Solo por IdTurno.
///   Desempate: las reglas de Fecha en ascendente, y después IdTurno.
///
/// El desempate por fecha de Paciente y Estado queda siempre ascendente,
/// aunque la columna esté en descendente.
///
/// Casos borde a cubrir:
///   - lista vacía;
///   - "orden" null, desconocido o con otra capitalización;
///   - Momento null (va último en las dos direcciones);
///   - fila sin hora: Momento es las 00:00 de su día;
///   - turno de hoy con la hora ya pasada (bloque 0, no bloque 1);
///   - "hoy" calculado a las 23:30 de Argentina, que en UTC ya es mañana;
///   - nombres con tilde, ñ y mayúsculas mezcladas;
///   - Paciente vacío o con espacios;
///   - estado con otra capitalización, y estado desconocido;
///   - dos turnos con el mismo Momento (decide IdTurno).
/// </summary>
public static class OrdenTurnos
{
    private static readonly StringComparer Nombres =
        StringComparer.Create(TurnosIndexViewModel.Cultura, ignoreCase: true);

    public static IReadOnlyList<TurnoFilaViewModel> Aplicar(
        IEnumerable<TurnoFilaViewModel> filas, string? orden, bool descendente, DateTime hoy)
    {
        var dia = hoy.Date;

        var ordenadas = orden?.Trim().ToLowerInvariant() switch
        {
            "paciente" => PorPaciente(filas, descendente, dia),
            "estado"   => PorEstado(filas, descendente, dia),
            _          => PorFecha(filas, descendente, dia)
        };

        return ordenadas.ThenBy(f => f.IdTurno).ToList();
    }

    private static IOrderedEnumerable<TurnoFilaViewModel> PorFecha(
        IEnumerable<TurnoFilaViewModel> filas, bool descendente, DateTime dia) =>
        filas
            .OrderBy(f => BloqueDeFecha(f, dia))
            .ThenBy(f => ClaveDeFecha(f, dia, descendente));

    private static IOrderedEnumerable<TurnoFilaViewModel> PorPaciente(
        IEnumerable<TurnoFilaViewModel> filas, bool descendente, DateTime dia)
    {
        var porBloque = filas.OrderBy(f => Nombre(f).Length == 0 ? 1 : 0);

        // Las filas sin nombre empatan todas en "" y siguen empatadas en el
        // desempate por fecha, así que las termina ordenando IdTurno.
        var porNombre = descendente
            ? porBloque.ThenByDescending(Nombre, Nombres)
            : porBloque.ThenBy(Nombre, Nombres);

        return DesempatarPorFecha(porNombre, f => Nombre(f).Length > 0, dia);
    }

    private static IOrderedEnumerable<TurnoFilaViewModel> PorEstado(
        IEnumerable<TurnoFilaViewModel> filas, bool descendente, DateTime dia)
    {
        var porEstado = filas
            .OrderBy(f => PosicionDeEstado(f) < 0 ? 1 : 0)
            .ThenBy(f => PosicionDeEstado(f) is var posicion && posicion < 0 ? 0
                       : descendente ? -posicion
                       : posicion);

        return DesempatarPorFecha(porEstado, f => PosicionDeEstado(f) >= 0, dia);
    }

    /// <summary>
    /// Segundo criterio de Paciente y Estado: las reglas de Fecha, siempre en
    /// ascendente. No se aplica a las filas del bloque de "dato faltante"
    /// (<paramref name="conDato"/> en false), que van solo por IdTurno.
    /// </summary>
    private static IOrderedEnumerable<TurnoFilaViewModel> DesempatarPorFecha(
        IOrderedEnumerable<TurnoFilaViewModel> filas, Func<TurnoFilaViewModel, bool> conDato, DateTime dia) =>
        filas
            .ThenBy(f => conDato(f) ? BloqueDeFecha(f, dia) : 0)
            .ThenBy(f => conDato(f) ? ClaveDeFecha(f, dia, descendente: false) : 0);

    /// <summary>0 = hoy o futuro, 1 = pasado, 2 = sin fecha. Se compara por día.</summary>
    private static int BloqueDeFecha(TurnoFilaViewModel fila, DateTime dia) =>
        fila.Momento is not { } momento ? 2
        : momento.Date >= dia ? 0
        : 1;

    /// <summary>
    /// Número que, ordenado de menor a mayor, da el orden dentro del bloque:
    /// los de hoy en adelante por Momento creciente y los pasados por Momento
    /// decreciente (el signo cambiado). En descendente se invierte el signo
    /// de nuevo. Las filas sin fecha valen todas 0.
    /// </summary>
    private static long ClaveDeFecha(TurnoFilaViewModel fila, DateTime dia, bool descendente)
    {
        if (fila.Momento is not { } momento) return 0;

        var clave = momento.Date >= dia ? momento.Ticks : -momento.Ticks;
        return descendente ? -clave : clave;
    }

    private static string Nombre(TurnoFilaViewModel fila) => fila.Paciente?.Trim() ?? string.Empty;

    /// <summary>Lugar del estado en el orden fijo, o -1 si no es uno conocido.</summary>
    private static int PosicionDeEstado(TurnoFilaViewModel fila) =>
        Array.FindIndex(TurnosFiltroViewModel.EstadosTurno,
            e => string.Equals(e, fila.Estado?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public class TurnosIndexViewModel
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>
    /// Todo lo que cargó la API para el usuario y el rango de fechas, sin
    /// filtrar por estado. Sobre esto cuenta el resumen de arriba.
    /// </summary>
    public IReadOnlyList<TurnoFilaViewModel> Turnos { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Las filas de la tabla: <see cref="Turnos"/> con el filtro de estado aplicado.</summary>
    public IReadOnlyList<TurnoFilaViewModel> Visibles { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Total que informa la API, puede ser mayor que lo listado.</summary>
    public int Total { get; init; }

    /// <summary>Mensaje de error de la API, si la carga falló.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    public TurnosFiltroViewModel Filtros { get; init; } = new();

    /// <summary>Rol del usuario: define el título y el alcance de lo que se ve.</summary>
    public string? Rol { get; init; }

    /// <summary>Aviso no fatal, por ejemplo un usuario sin perfil asociado.</summary>
    public string? Aviso { get; init; }

    public string Titulo => Rol switch
    {
        "paciente" => "Mis turnos",
        "doctor"   => "Mi agenda",
        _          => "Turnos"
    };

    public string? Subtitulo => Rol switch
    {
        "paciente" => "Solo se muestran los turnos en los que figurás como paciente.",
        "doctor"   => "Solo se muestran los turnos que tenés asignados.",
        _          => null
    };

    public string TextoVacio => Rol switch
    {
        "paciente" => "Todavía no tenés ningún turno.",
        "doctor"   => "Todavía no tenés turnos asignados.",
        _          => "Cuando se registren turnos en el sistema van a aparecer en esta lista."
    };

    public int Confirmados => Contar("confirmado");
    public int Pendientes  => Contar("pendiente");
    public int Completados => Contar("completado");
    public int Cancelados  => Contar("cancelado");

    /// <summary>No hay resultados, pero porque el rango de fechas no trae nada.</summary>
    public bool SinResultados => Turnos.Count == 0 && Filtros.HayFechas;

    /// <summary>Hay turnos cargados, pero ninguno del estado de la tarjeta activa.</summary>
    public bool SinTurnosDelEstado => Turnos.Count > 0 && Visibles.Count == 0;

    /// <summary>La API paginó y quedaron turnos afuera del listado.</summary>
    public bool HayMas => Total > Turnos.Count;

    /// <summary>Alguna fila vino sin hora desde la API.</summary>
    public bool FaltaHora => Visibles.Any(t => t.Hora is null);

    /// <summary>El día de hoy en hora de Argentina, el mismo que se usó para ordenar.</summary>
    public DateTime Hoy { get; init; } = FechaArgentina.Hoy();

    public string FechaDeHoy =>
        Hoy.ToString("D", Cultura);

    private int Contar(string estado) =>
        Turnos.Count(t => string.Equals(t.Estado, estado, StringComparison.OrdinalIgnoreCase));

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
