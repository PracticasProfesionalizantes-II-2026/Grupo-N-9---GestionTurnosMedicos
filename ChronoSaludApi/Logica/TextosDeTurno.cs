using System.Globalization;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Los textos que le llegan al paciente sobre sus turnos, en la campana y por
/// correo. El día va como se dice ("lunes 13 de octubre", con el año solo si
/// no es el actual) y no como "13/10/2026", para que no haya que interpretar
/// números; los estados, con palabras y no con el código de la base.
/// </summary>
public static class TextosDeTurno
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>"lunes 13 de octubre", o "viernes 15 de enero de 2027" si es de otro año.</summary>
    public static string Dia(DateTime fecha, DateTime hoy)
    {
        var dia = fecha.ToString("dddd d 'de' MMMM", Cultura);
        return fecha.Year == hoy.Year ? dia : $"{dia} de {fecha.Year}";
    }

    public static string Hora(TimeSpan hora) => hora.ToString(@"hh\:mm");

    public static string Creado(DateTime fecha, TimeSpan hora, DateTime hoy) =>
        $"Se creó tu turno para el {Dia(fecha, hoy)} a las {Hora(hora)}.";

    public static string Reprogramado(DateTime fechaAnterior, TimeSpan horaAnterior, DateTime fecha, TimeSpan hora, DateTime hoy) =>
        $"Tu turno del {Dia(fechaAnterior, hoy)} a las {Hora(horaAnterior)} se pasó al {Dia(fecha, hoy)} a las {Hora(hora)}.";

    public static string Cancelado(DateTime fecha, TimeSpan hora, DateTime hoy) =>
        $"Se canceló tu turno del {Dia(fecha, hoy)} a las {Hora(hora)}.";

    public static string EstadoCambiado(DateTime fecha, TimeSpan hora, string estado, DateTime hoy)
    {
        var cuando = $"{Dia(fecha, hoy)} a las {Hora(hora)}";
        return estado switch
        {
            EstadosTurno.Confirmado => $"Se confirmó tu turno del {cuando}.",
            EstadosTurno.Completado => $"Tu turno del {cuando} quedó como atendido.",
            EstadosTurno.Ausente    => $"Tu turno del {cuando} quedó registrado como ausente.",
            EstadosTurno.Cancelado  => $"Se canceló tu turno del {cuando}.",
            _                       => $"Tu turno del {cuando} ahora está «{estado}».",
        };
    }

    /// <summary>El asunto del correo según el aviso.</summary>
    public static string AsuntoDeEstado(string estado) => estado switch
    {
        EstadosTurno.Confirmado => "Tu turno está confirmado",
        EstadosTurno.Cancelado  => "Se canceló tu turno",
        _                       => "Novedades de tu turno",
    };
}
