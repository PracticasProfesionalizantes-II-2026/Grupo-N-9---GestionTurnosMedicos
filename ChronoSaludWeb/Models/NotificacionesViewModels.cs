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
    /// Los tipos de la API son "turno", "estudio", "receta" y "general"; hoy
    /// se crean solo los de turno y estudio.
    /// </summary>
    public string Icono => Tipo switch
    {
        "turno" => "calendario",
        "estudio" => "estudio",
        "receta" => "pastilla",
        _ => "info"
    };

    /// <summary>"9 oct 2026, 10:30".</summary>
    public string FechaTexto => FechaArgentina.CortaConHora(Fecha);

    /// <summary>Las de turnos llevan el enlace "Ver mis turnos".</summary>
    public bool EsDeTurno => Tipo == "turno";

    /// <summary>Las de estudios llevan el enlace "Ver mis estudios".</summary>
    public bool EsDeEstudio => Tipo == "estudio";

    public static NotificacionFilaViewModel Desde(NotificacionLista notificacion) => new()
    {
        Id = notificacion.Id,
        Tipo = notificacion.Tipo,
        Mensaje = notificacion.Mensaje,
        Fecha = notificacion.Fecha,
        Leida = notificacion.Leida
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
