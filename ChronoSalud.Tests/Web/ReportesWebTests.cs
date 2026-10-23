using System.Text;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la pantalla Reportes (paso 22): los períodos, cómo se dicen,
/// las barras, el ausentismo en pantalla y el CSV.
/// </summary>
public class ReportesWebTests
{
    // Jueves 22 de octubre de 2026.
    private static readonly DateTime Hoy = new(2026, 10, 22);

    // ---- Períodos ----------------------------------------------------------

    [Theory]
    [InlineData(PeriodoDeReporte.EstaSemana, "2026-10-19", "2026-10-25")]
    [InlineData(PeriodoDeReporte.SemanaPasada, "2026-10-12", "2026-10-18")]
    [InlineData(PeriodoDeReporte.EsteMes, "2026-10-01", "2026-10-31")]
    [InlineData(PeriodoDeReporte.MesPasado, "2026-09-01", "2026-09-30")]
    public void Cada_periodo_con_nombre_va_de_lunes_a_domingo_o_del_primero_al_ultimo(string clave, string desde, string hasta)
    {
        var periodo = PeriodoDeReporte.Resolver(clave, null, null, Hoy);

        Assert.Equal(clave, periodo.Clave);
        Assert.Equal(DateTime.Parse(desde), periodo.Desde);
        Assert.Equal(DateTime.Parse(hasta), periodo.Hasta);
    }

    [Fact]
    public void Un_domingo_sigue_siendo_de_la_semana_que_empezo_el_lunes()
    {
        var periodo = PeriodoDeReporte.Resolver(PeriodoDeReporte.EstaSemana, null, null, new DateTime(2026, 10, 25));

        Assert.Equal(new DateTime(2026, 10, 19), periodo.Desde);
    }

    [Fact]
    public void El_mes_pasado_en_enero_es_diciembre_del_anio_anterior()
    {
        var periodo = PeriodoDeReporte.Resolver(PeriodoDeReporte.MesPasado, null, null, new DateTime(2027, 1, 10));

        Assert.Equal((new DateTime(2026, 12, 1), new DateTime(2026, 12, 31)), (periodo.Desde, periodo.Hasta));
    }

    [Fact]
    public void Sin_nada_es_este_mes()
    {
        Assert.Equal(PeriodoDeReporte.EsteMes, PeriodoDeReporte.Resolver(null, null, null, Hoy).Clave);
        Assert.Equal(PeriodoDeReporte.EsteMes, PeriodoDeReporte.Resolver("otro", null, null, Hoy).Clave);
    }

    [Fact]
    public void Las_fechas_a_mano_son_un_periodo_elegido()
    {
        var periodo = PeriodoDeReporte.Resolver(null, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9), Hoy);

        Assert.Equal(PeriodoDeReporte.Elegido, periodo.Clave);
        Assert.Equal(new DateTime(2026, 10, 9), periodo.Hasta);
    }

    [Fact]
    public void Con_una_sola_fecha_el_periodo_es_ese_dia()
    {
        var periodo = PeriodoDeReporte.Resolver(null, null, new DateTime(2026, 10, 5), Hoy);

        Assert.Equal((new DateTime(2026, 10, 5), new DateTime(2026, 10, 5)), (periodo.Desde, periodo.Hasta));
    }

    [Fact]
    public void Unas_fechas_que_son_las_de_un_periodo_con_nombre_se_muestran_como_ese()
    {
        var periodo = PeriodoDeReporte.Resolver(null, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Hoy);

        Assert.Equal(PeriodoDeReporte.EsteMes, periodo.Clave);
    }

    [Fact]
    public void El_periodo_con_nombre_gana_sobre_las_fechas()
    {
        var periodo = PeriodoDeReporte.Resolver(PeriodoDeReporte.MesPasado, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), Hoy);

        Assert.Equal(new DateTime(2026, 9, 1), periodo.Desde);
    }

    [Fact]
    public void Un_rango_invertido_o_de_mas_de_un_anio_tiene_problema()
    {
        Assert.Contains("anterior", PeriodoDeReporte.Problema(new Periodo("fechas", new DateTime(2026, 10, 9), new DateTime(2026, 10, 5))));
        Assert.Contains("un año", PeriodoDeReporte.Problema(new Periodo("fechas", new DateTime(2026, 1, 1), new DateTime(2027, 1, 2))));
        Assert.Null(PeriodoDeReporte.Problema(new Periodo("fechas", new DateTime(2028, 1, 1), new DateTime(2028, 12, 31))));
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-31", "Del 1 al 31 de octubre de 2026")]
    [InlineData("2026-09-28", "2026-10-04", "Del 28 de septiembre al 4 de octubre de 2026")]
    [InlineData("2026-12-28", "2027-01-03", "Del 28 de diciembre de 2026 al 3 de enero de 2027")]
    [InlineData("2026-10-13", "2026-10-13", "El 13 de octubre de 2026")]
    public void El_periodo_se_dice_con_palabras(string desde, string hasta, string esperado)
    {
        Assert.Equal(esperado, PeriodoDeReporte.Texto(DateTime.Parse(desde), DateTime.Parse(hasta)));
    }

    // ---- Barras y ausentismo -----------------------------------------------

    [Theory]
    [InlineData(1, 4, 25)]
    [InlineData(1, 3, 33)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 1000, 1)]
    [InlineData(5, 0, 0)]
    public void El_largo_de_la_barra_es_la_parte_del_total_y_nunca_desaparece(int valor, int total, int esperado)
    {
        Assert.Equal(esperado, ReportesViewModel.Porcentaje(valor, total));
    }

    [Fact]
    public void El_ausentismo_se_escribe_con_coma_y_sin_decimal_de_mas()
    {
        Assert.Equal("25 %", ReportesViewModel.TextoDeAusentismo(25.0));
        Assert.Equal("33,3 %", ReportesViewModel.TextoDeAusentismo(33.3));
        Assert.Equal("—", ReportesViewModel.TextoDeAusentismo(null));
    }

    [Fact]
    public void Las_barras_de_estado_van_en_el_orden_del_turno_con_el_tono_de_su_chip()
    {
        var modelo = Modelo(Reporte());

        Assert.Equal(new[] { "Pendientes", "Confirmados", "Completados", "Ausentes", "Cancelados" }, modelo.Estados.Select(e => e.Etiqueta));
        Assert.Equal(new[] { "aviso", "exito", "info", "neutro", "peligro" }, modelo.Estados.Select(e => e.Tono));
        Assert.Equal(new[] { 10, 20, 40, 10, 20 }, modelo.Estados.Select(e => e.Porcentaje));
    }

    [Fact]
    public void La_ruta_lleva_el_periodo_con_nombre_o_las_fechas_y_los_filtros()
    {
        var conNombre = Modelo(null, PeriodoDeReporte.Resolver(PeriodoDeReporte.EsteMes, null, null, Hoy), doctor: 20);
        Assert.Equal(new Dictionary<string, string> { ["periodo"] = "mes", ["doctor"] = "20" }, conNombre.RutaActual());

        var aMano = Modelo(null, new Periodo(PeriodoDeReporte.Elegido, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9)), especialidad: "Clínica");
        Assert.Equal(new Dictionary<string, string> { ["desde"] = "2026-10-05", ["hasta"] = "2026-10-09", ["especialidad"] = "Clínica" }, aMano.RutaActual());
    }

    [Fact]
    public void De_quien_es_el_reporte_sale_del_select_y_la_especialidad()
    {
        var modelo = new ReportesViewModel
        {
            Periodo = PeriodoDeReporte.Resolver(null, null, null, Hoy),
            Doctor = 20,
            Especialidad = "Clínica",
            Doctores = new[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("Laura Gomez", "20") }
        };

        Assert.Equal("Laura Gomez (Clínica)", modelo.DeQuien);
    }

    // ---- CSV -----------------------------------------------------------------

    [Fact]
    public void El_csv_trae_los_totales_y_los_dos_desgloses_con_punto_y_coma()
    {
        var csv = ReporteCsv.Armar(Reporte(), "Del 1 al 31 de octubre de 2026", null, deUnDoctor: false);
        var lineas = csv.Split("\r\n");

        Assert.Equal("Reporte de turnos;Del 1 al 31 de octubre de 2026", lineas[0]);
        Assert.Contains("Total;10", lineas);
        Assert.Contains("Ausentismo (%);20,0", lineas);
        Assert.Contains("Especialidad;Turnos;Pendientes;Confirmados;Completados;Ausentes;Cancelados;Ausentismo (%)", lineas);
        Assert.Contains("Clínica;10;1;2;4;1;2;20,0", lineas);
        Assert.Contains("Hugo Paz (dado de baja);Cardiología;0;0;0;0;0;0;", lineas);
    }

    [Fact]
    public void Con_un_solo_doctor_el_csv_no_repite_el_bloque_por_especialidad()
    {
        var csv = ReporteCsv.Armar(Reporte(), "Este mes", "Laura Gomez", deUnDoctor: true);

        Assert.DoesNotContain(csv.Split("\r\n"), linea => linea.StartsWith("Especialidad;"));
        Assert.Contains("De;Laura Gomez", csv);
    }

    [Theory]
    [InlineData("Gomez; Laura", "\"Gomez; Laura\"")]
    [InlineData("La \"Dra\"", "\"La \"\"Dra\"\"\"")]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("+54 11", "'+54 11")]
    [InlineData("@usuario", "'@usuario")]
    [InlineData("Clínica", "Clínica")]
    [InlineData(null, "")]
    public void Cada_celda_se_escapa_y_no_se_toma_como_formula(string? valor, string esperado)
    {
        Assert.Equal(esperado, ReporteCsv.Celda(valor));
    }

    [Fact]
    public void El_archivo_va_en_utf8_con_bom_para_que_excel_lea_los_acentos()
    {
        var bytes = ReporteCsv.Bytes("Clínica");

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
        Assert.Equal("Clínica", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        Assert.Equal("reporte-turnos-2026-10-01-al-2026-10-31.csv",
            ReporteCsv.NombreDeArchivo(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)));
    }

    // ---- Datos -----------------------------------------------------------------

    private static ReporteResumen Reporte()
    {
        var total = new ConteoTurnos(10, 1, 2, 4, 1, 2, 20.0);
        var vacio = new ConteoTurnos(0, 0, 0, 0, 0, 0, null);

        return new ReporteResumen(
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), null, null, total, 3,
            new[] { new ReporteEspecialidad("Clínica", total), new ReporteEspecialidad("Cardiología", vacio) },
            new[]
            {
                new ReporteDoctor(20, "Laura Gomez", "Clínica", true, total),
                new ReporteDoctor(23, "Hugo Paz", "Cardiología", false, vacio)
            });
    }

    private static ReportesViewModel Modelo(ReporteResumen? reporte, Periodo? periodo = null, int? doctor = null, string? especialidad = null) =>
        new()
        {
            Periodo = periodo ?? PeriodoDeReporte.Resolver(null, null, null, Hoy),
            Reporte = reporte,
            Doctor = doctor,
            Especialidad = especialidad
        };
}
