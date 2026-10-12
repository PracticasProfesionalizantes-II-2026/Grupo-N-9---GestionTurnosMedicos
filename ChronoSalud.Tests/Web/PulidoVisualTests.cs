using ChronoSaludWeb.Models;
using ChronoSaludWeb.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la lógica que acompaña al pulido visual (paso 20): los grupos de
/// la línea de tiempo, la vigencia de las recetas, el color de los avatares,
/// el bloque de calendario, los íconos y la franja del estado.
/// </summary>
public class PulidoVisualTests
{
    // Lunes 12 de octubre de 2026.
    private static readonly DateTime Hoy = new(2026, 10, 12);

    private record Entrada(string Nombre, DateTime? Fecha);

    // ---- Línea de tiempo ---------------------------------------------------

    [Fact]
    public void Por_dia_titula_hoy_ayer_y_el_resto_con_el_dia_y_el_mes()
    {
        var grupos = AgrupadorPorFecha.PorDia(new[]
        {
            new Entrada("a", new DateTime(2026, 10, 12, 15, 0, 0)),
            new Entrada("b", new DateTime(2026, 10, 11, 9, 0, 0)),
            new Entrada("c", new DateTime(2026, 10, 7, 9, 0, 0)),
        }, e => e.Fecha, Hoy);

        Assert.Equal(new[] { "Hoy", "Ayer", "Miércoles 7 de octubre" }, grupos.Select(g => g.Titulo));
    }

    [Fact]
    public void Por_dia_de_otro_anio_suma_el_anio()
    {
        Assert.Equal("Martes 30 de diciembre de 2025", AgrupadorPorFecha.TituloDelDia(new DateTime(2025, 12, 30), Hoy));
    }

    [Fact]
    public void Junta_los_seguidos_del_mismo_dia_y_respeta_el_orden()
    {
        var grupos = AgrupadorPorFecha.PorDia(new[]
        {
            new Entrada("a", new DateTime(2026, 10, 12, 18, 0, 0)),
            new Entrada("b", new DateTime(2026, 10, 12, 8, 0, 0)),
            new Entrada("c", new DateTime(2026, 10, 10, 8, 0, 0)),
        }, e => e.Fecha, Hoy);

        Assert.Equal(2, grupos.Count);
        Assert.Equal(new[] { "a", "b" }, grupos[0].Elementos.Select(e => e.Nombre));
        Assert.Equal(new[] { "c" }, grupos[1].Elementos.Select(e => e.Nombre));
    }

    [Fact]
    public void Por_mes_titula_con_el_mes_en_mayuscula_y_el_anio()
    {
        var grupos = AgrupadorPorFecha.PorMes(new[]
        {
            new Entrada("a", new DateTime(2026, 10, 3)),
            new Entrada("b", new DateTime(2026, 10, 1)),
            new Entrada("c", new DateTime(2026, 9, 20)),
        }, e => e.Fecha);

        Assert.Equal(new[] { "Octubre de 2026", "Septiembre de 2026" }, grupos.Select(g => g.Titulo));
        Assert.Equal(2, grupos[0].Elementos.Count);
    }

    [Fact]
    public void Lo_que_no_tiene_fecha_va_al_final_en_sin_fecha()
    {
        var grupos = AgrupadorPorFecha.PorMes(new[]
        {
            new Entrada("a", null),
            new Entrada("b", new DateTime(2026, 10, 3)),
        }, e => e.Fecha);

        Assert.Equal(new[] { "Octubre de 2026", "Sin fecha" }, grupos.Select(g => g.Titulo));
        Assert.Equal("a", grupos[^1].Elementos.Single().Nombre);
    }

    [Fact]
    public void Una_lista_vacia_no_arma_grupos()
    {
        Assert.Empty(AgrupadorPorFecha.PorDia(Array.Empty<Entrada>(), e => e.Fecha, Hoy));
    }

    // ---- Vigencia de las recetas -------------------------------------------

    [Theory]
    [InlineData(-1, "Vencida")]
    [InlineData(0, "Vence hoy")]
    [InlineData(1, "Vence mañana")]
    [InlineData(5, "Vence en 5 días")]
    public void La_vigencia_se_dice_con_palabras(int dias, string esperado)
    {
        Assert.Equal(esperado, RecetaFilaViewModel.TextoDeVigencia(Hoy.AddDays(dias), Hoy));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void Vence_pronto_si_le_queda_una_semana_o_menos(int dias, bool esperado)
    {
        Assert.Equal(esperado, RecetaFilaViewModel.VencePorVencer(Hoy.AddDays(dias), Hoy));
    }

    // ---- Avatares ------------------------------------------------------------

    [Fact]
    public void La_variante_del_avatar_es_siempre_la_misma_para_el_mismo_nombre()
    {
        var una = new AvatarViewModel { Nombre = "Laura Gomez" }.Variante;
        var otra = new AvatarViewModel { Nombre = "  laura gomez " }.Variante;

        Assert.Equal(una, otra);
        Assert.InRange(una, 0, 2);
    }

    [Fact]
    public void Sin_nombre_el_avatar_usa_la_primera_variante()
    {
        Assert.Equal(0, new AvatarViewModel { Nombre = null }.Variante);
    }

    [Fact]
    public void Los_nombres_se_reparten_entre_las_tres_variantes()
    {
        var nombres = new[] { "Ana", "Bruno", "Carla", "Diego", "Elena", "Fabián", "Gisela", "Hugo" };
        var variantes = nombres.Select(n => new AvatarViewModel { Nombre = n }.Variante).Distinct().Count();

        Assert.Equal(3, variantes);
    }

    // ---- Bloque de calendario --------------------------------------------------

    [Fact]
    public void El_bloque_de_calendario_lleva_mes_dia_y_semana_sin_punto()
    {
        var tile = new FechaTileViewModel { Fecha = new DateTime(2026, 10, 16) };

        Assert.Equal("oct", tile.Mes);
        Assert.Equal("16", tile.Dia);
        Assert.Equal("vie", tile.Semana);
    }

    // ---- Pasos ---------------------------------------------------------------

    [Theory]
    [InlineData(1, 2, EstadoPaso.Hecho)]
    [InlineData(2, 2, EstadoPaso.Actual)]
    [InlineData(3, 2, EstadoPaso.Pendiente)]
    public void El_estado_de_un_paso_sale_del_paso_actual(int numero, int actual, EstadoPaso esperado)
    {
        Assert.Equal(esperado, PasoViewModel.Para(numero, actual));
    }

    // ---- Franja del estado y actividad -----------------------------------------

    [Theory]
    [InlineData("confirmado", "franja franja-exito")]
    [InlineData("Pendiente", "franja franja-aviso")]
    [InlineData("completado", "franja franja-info")]
    [InlineData("cancelado", "franja franja-peligro")]
    [InlineData("ausente", "franja franja-neutro")]
    [InlineData(null, "franja franja-neutro")]
    public void La_franja_sigue_el_tono_del_chip_del_estado(string? estado, string esperado)
    {
        Assert.Equal(esperado, FranjaEstado.Clase(estado));
    }

    [Fact]
    public void Cada_accion_de_la_actividad_tiene_su_icono_del_catalogo()
    {
        foreach (var (codigo, _) in ActividadViewModel.Acciones)
        {
            var icono = ActividadViewModel.IconoDeAccion(codigo);
            // Una clave que no está en el catálogo cae en el círculo vacío.
            Assert.NotEqual(IconosLucide.Trazos("no-existe"), IconosLucide.Trazos(icono));
        }

        Assert.Equal("actividad", ActividadViewModel.IconoDeAccion("algo.nuevo"));
    }

    // ---- Campana ---------------------------------------------------------------

    [Fact]
    public void La_campana_se_mueve_solo_en_el_inicio_y_con_avisos_sin_leer()
    {
        Assert.True(new CampanaViewModel { NoLeidas = 2, EnElInicio = true }.Llamar);
        Assert.False(new CampanaViewModel { NoLeidas = 2, EnElInicio = false }.Llamar);
        Assert.False(new CampanaViewModel { NoLeidas = 0, EnElInicio = true }.Llamar);
        Assert.False(new CampanaViewModel { NoLeidas = null, EnElInicio = true }.Llamar);
    }

    // ---- Íconos ----------------------------------------------------------------

    private static TagHelperOutput Icono(string nombre, params (string Nombre, string Valor)[] atributos)
    {
        var lista = new TagHelperAttributeList();
        foreach (var (atributo, valor) in atributos)
            lista.Add(atributo, valor);

        var salida = new TagHelperOutput("icono", lista,
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var contexto = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "prueba");

        new IconoTagHelper { Nombre = nombre }.Process(contexto, salida);
        return salida;
    }

    [Fact]
    public void El_icono_se_dibuja_como_svg_oculto_con_los_trazos_del_catalogo()
    {
        var salida = Icono("volver", ("class", "size-4"));

        Assert.Equal("svg", salida.TagName);
        Assert.Equal(TagMode.StartTagAndEndTag, salida.TagMode);
        Assert.Equal("size-4", salida.Attributes["class"].Value.ToString());
        Assert.Equal("true", salida.Attributes["aria-hidden"].Value.ToString());
        Assert.Equal("1.5", salida.Attributes["stroke-width"].Value.ToString());
        Assert.Equal(IconosLucide.Trazos("volver"), salida.Content.GetContent());
    }

    [Fact]
    public void El_icono_respeta_un_trazo_propio()
    {
        var salida = Icono("check", ("stroke-width", "2.5"));

        Assert.Equal("2.5", salida.Attributes["stroke-width"].Value.ToString());
    }
}
