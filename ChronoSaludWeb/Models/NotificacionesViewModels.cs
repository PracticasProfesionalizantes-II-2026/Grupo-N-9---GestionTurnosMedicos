using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// La campana del encabezado (partial _Campana). Solo la ven los pacientes.
/// </summary>
public class CampanaViewModel
{
    /// <summary>Cuántas hay sin leer. Null si la API no contestó: la campana sale sin número.</summary>
    public int? NoLeidas { get; init; }

    /// <summary>Se está en la pantalla de notificaciones: el enlace va marcado.</summary>
    public bool Activa { get; init; }

    /// <summary>
    /// La campana se balancea una vez para llamar la atención sobre lo que no
    /// se leyó. Solo en el inicio: en cada pantalla sería ruido.
    /// </summary>
    public bool EnElInicio { get; init; }

    /// <summary>Hay que mover la campana: estamos en el inicio y hay sin leer.</summary>
    public bool Llamar => EnElInicio && Globito is not null;

    /// <summary>
    /// El número del globito rojo: null si no hay ninguna (no sale), "9+" si
    /// son más de 9, para que no se agrande.
    /// </summary>
    public string? Globito
    {
        get
        {
            if (NoLeidas is null || NoLeidas <= 0)
                return null;

            return NoLeidas > 9 ? "9+" : NoLeidas.Value.ToString();
        }
    }

    /// <summary>
    /// Lo que lee el lector de pantalla. El globito es solo visual, así que
    /// el número completo va acá.
    /// </summary>
    public string TextoAccesible
    {
        get
        {
            if (NoLeidas is null || NoLeidas <= 0)
                return "Notificaciones";

            return NoLeidas == 1
                ? "Notificaciones: 1 sin leer"
                : $"Notificaciones: {NoLeidas} sin leer";
        }
    }
}

/// <summary>Una notificación de la lista.</summary>
public class NotificacionFilaViewModel
{
    public int Id { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public bool Leida { get; init; }

    /// <summary>
    /// Los tipos de la API son "turno", "recordatorio", "estudio", "receta" y
    /// "general"; hoy se crean los de turno, recordatorio y estudio.
    /// </summary>
    public string Icono => Tipo switch
    {
        "turno" => "calendario",
        "recordatorio" => "reloj",
        "estudio" => "estudio",
        "receta" => "pastilla",
        _ => "info"
    };

    /// <summary>"9 oct 2026, 10:30".</summary>
    public string FechaTexto => FechaArgentina.CortaConHora(Fecha);

    /// <summary>Las de turnos y los recordatorios del día llevan el enlace "Ver mis turnos".</summary>
    public bool EsDeTurno => Tipo is "turno" or "recordatorio";

    /// <summary>Las de estudios llevan el enlace "Ver mis estudios".</summary>
    public bool EsDeEstudio => Tipo == "estudio";

    /// <summary>
    /// La pantalla a la que lleva, si la API mandó una y es una ruta de este
    /// mismo sitio (paso 23). Null si no hay: queda el enlace por el tipo.
    /// </summary>
    public string? Enlace { get; init; }

    /// <summary>"Ver el turno" o "Ver los estudios", según adónde lleva el enlace.</summary>
    public string TextoDelEnlace =>
        Enlace is not null && Enlace.StartsWith("/Estudios", StringComparison.OrdinalIgnoreCase) ? "Ver los estudios"
        : Enlace is not null && Enlace.StartsWith("/Turnos", StringComparison.OrdinalIgnoreCase) ? "Ver el turno"
        : "Ver el detalle";

    /// <summary>
    /// True solo para una ruta relativa de este sitio ("/Turnos/Detalle/12"):
    /// empieza con una sola barra, sin "//" ni "/\" (que el navegador toma
    /// como otro sitio) y sin espacios ni caracteres de control.
    /// </summary>
    public static bool EsEnlaceSeguro(string? enlace) =>
        !string.IsNullOrEmpty(enlace)
        && enlace.Length <= 200
        && enlace[0] == '/'
        && (enlace.Length == 1 || (enlace[1] != '/' && enlace[1] != '\\'))
        && enlace.All(c => !char.IsWhiteSpace(c) && !char.IsControl(c));

    public static NotificacionFilaViewModel Desde(NotificacionLista notificacion) => new()
    {
        Id = notificacion.Id,
        Tipo = notificacion.Tipo,
        Mensaje = notificacion.Mensaje,
        Fecha = notificacion.Fecha,
        Leida = notificacion.Leida,
        Enlace = EsEnlaceSeguro(notificacion.Enlace) ? notificacion.Enlace : null
    };
}

/// <summary>La pantalla Notificaciones/Index.</summary>
public class NotificacionesIndexViewModel
{
    public IReadOnlyList<NotificacionFilaViewModel> Notificaciones { get; init; } = Array.Empty<NotificacionFilaViewModel>();

    /// <summary>Cuántas hay en la pestaña elegida.</summary>
    public int Total { get; init; }

    /// <summary>Cuántas hay sin leer en total, para el botón "Marcar todas".</summary>
    public int NoLeidas { get; init; }

    public int Pagina { get; init; } = 1;

    /// <summary>La pestaña "Sin leer".</summary>
    public bool SoloNoLeidas { get; init; }

    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    /// <summary>El rol de quien mira: cambia qué avisos le llegan.</summary>
    public string? Rol { get; init; }

    public string Subtitulo => Rol switch
    {
        "doctor" => "Las cancelaciones de tu agenda y los resultados de los estudios que pediste, de los más nuevos a los más viejos.",
        "administrador" => "Los turnos que pidieron los pacientes y falta confirmar, de los más nuevos a los más viejos.",
        _ => "Los avisos de tus turnos y de tus estudios, de los más nuevos a los más viejos."
    };

    public string AyudaVacio => Rol switch
    {
        "doctor" => "Cuando un paciente o la administración cancelen un turno tuyo, o llegue el resultado de un estudio que pediste, te avisamos acá.",
        "administrador" => "Cuando un paciente pida un turno, te avisamos acá para que lo confirmes.",
        _ => "Cuando se confirme, se cambie o se cancele un turno tuyo, o cuando esté el resultado de un estudio, te avisamos acá."
    };

    /// <summary>
    /// La ruta de una pestaña. "Todas" no lleva nada, así la URL queda limpia.
    /// </summary>
    public static IDictionary<string, string> RutaPestana(bool soloNoLeidas)
    {
        var ruta = new Dictionary<string, string>();
        if (soloNoLeidas) ruta["ver"] = "sin-leer";
        return ruta;
    }

    /// <summary>La ruta de una página, en la pestaña elegida.</summary>
    public IDictionary<string, string> RutaPagina(int pagina)
    {
        var ruta = RutaPestana(SoloNoLeidas);
        if (pagina > 1) ruta["pagina"] = pagina.ToString();
        return ruta;
    }

    public PaginadorViewModel Paginador => new()
    {
        Pagina = Pagina,
        TotalPaginas = PaginadorViewModel.ContarPaginas(Total, NotificacionService.PorPagina),
        RutaAnterior = RutaPagina(Pagina - 1),
        RutaSiguiente = RutaPagina(Pagina + 1)
    };
}
