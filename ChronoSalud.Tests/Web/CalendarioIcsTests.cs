using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// El archivo .ics de "Agregar a mi calendario": la hora tiene que quedar bien
/// en hora de Argentina, con los dos avisos, y el texto con el formato que
/// piden los calendarios (líneas cortas que terminan en CRLF).
/// </summary>
public class CalendarioIcsTests
{
    private static readonly DateTime Ahora = new(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);

    private static TurnoDetalleViewModel UnTurno(string horaInicio = "10:00", string horaFin = "10:30") => new()
    {
        IdTurno = 42,
        FechaInicio = new DateTime(2026, 10, 16),
        HoraInicio = horaInicio,
        HoraFin = horaFin,
        Estado = "confirmado",
        DoctorNombre = "Laura Pérez",
        Especialidad = "Clínica médica",
        Consultorio = "3"
    };

    private static string[] Lineas(string ics) =>
        ics.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Un_turno_a_las_10_de_Argentina_son_las_13_en_UTC()
    {
        var ics = CalendarioIcs.Armar(UnTurno(), null, Ahora);

        Assert.Contains("DTSTART:20261016T130000Z", Lineas(ics));
        Assert.Contains("DTEND:20261016T133000Z", Lineas(ics));
    }

    [Fact]
    public void Sin_hora_de_fin_dura_media_hora()
    {
        var ics = CalendarioIcs.Armar(UnTurno(horaFin: ""), null, Ahora);

        Assert.Contains("DTEND:20261016T133000Z", Lineas(ics));
    }

    [Fact]
    public void Lleva_un_aviso_el_dia_anterior_y_otro_una_hora_antes()
    {
        var lineas = Lineas(CalendarioIcs.Armar(UnTurno(), null, Ahora));

        Assert.Equal(2, lineas.Count(l => l == "BEGIN:VALARM"));
        Assert.Contains("TRIGGER:-P1D", lineas);
        Assert.Contains("TRIGGER:-PT1H", lineas);
    }

    [Fact]
    public void El_mismo_turno_siempre_tiene_el_mismo_identificador()
    {
        // Así, si se descarga dos veces, el calendario lo actualiza y no lo duplica.
        Assert.Contains("UID:turno-42@chronosalud", Lineas(CalendarioIcs.Armar(UnTurno(), null, Ahora)));
    }

    [Fact]
    public void El_lugar_suma_el_consultorio_y_la_direccion()
    {
        var ics = CalendarioIcs.Armar(UnTurno(), "Av. Siempreviva 742", Ahora);

        // La coma separa las dos partes y en el formato va escapada.
        Assert.Contains("LOCATION:Consultorio 3\\, Av. Siempreviva 742", Lineas(ics));
    }

    [Fact]
    public void Sin_consultorio_ni_direccion_no_hay_lugar()
    {
        var turno = UnTurno();
        var sinLugar = new TurnoDetalleViewModel
        {
            IdTurno = turno.IdTurno,
            FechaInicio = turno.FechaInicio,
            HoraInicio = turno.HoraInicio,
            HoraFin = turno.HoraFin,
            Estado = turno.Estado,
            DoctorNombre = turno.DoctorNombre
        };

        Assert.DoesNotContain(Lineas(CalendarioIcs.Armar(sinLugar, null, Ahora)), l => l.StartsWith("LOCATION:"));
    }

    [Theory]
    [InlineData("a,b", "a\\,b")]
    [InlineData("a;b", "a\\;b")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData("línea 1\nlínea 2", "línea 1\\nlínea 2")]
    public void Los_signos_especiales_salen_escapados(string texto, string esperado)
    {
        Assert.Equal(esperado, CalendarioIcs.Escapar(texto));
    }

    [Fact]
    public void Todas_las_lineas_terminan_en_CRLF_y_miden_75_bytes_o_menos()
    {
        var turno = new TurnoDetalleViewModel
        {
            IdTurno = 7,
            FechaInicio = new DateTime(2026, 10, 16),
            HoraInicio = "10:00",
            HoraFin = "10:30",
            Estado = "pendiente",
            // Un nombre largo y con acentos, para forzar el corte.
            DoctorNombre = "María José Fernández de la Cruz Ñuñez Echeverría Gutiérrez",
            Especialidad = "Otorrinolaringología pediátrica"
        };

        var ics = CalendarioIcs.Armar(turno, "Avenida Libertador General San Martín 12345, Ciudad Autónoma de Buenos Aires", Ahora);

        Assert.EndsWith("\r\n", ics);
        Assert.DoesNotContain("\n", ics.Replace("\r\n", ""));
        Assert.All(ics.Split("\r\n"), linea => Assert.True(System.Text.Encoding.UTF8.GetByteCount(linea) <= 75, linea));
    }

    [Fact]
    public void Una_linea_cortada_vuelve_a_ser_la_misma_al_juntarla()
    {
        var original = "SUMMARY:" + new string('á', 60);

        var pedazos = CalendarioIcs.Cortar(original).ToList();

        Assert.True(pedazos.Count > 1);
        Assert.All(pedazos.Skip(1), p => Assert.StartsWith(" ", p));

        // El calendario junta los pedazos sacándoles el espacio del principio.
        var junta = pedazos[0] + string.Concat(pedazos.Skip(1).Select(p => p[1..]));
        Assert.Equal(original, junta);
    }
}
