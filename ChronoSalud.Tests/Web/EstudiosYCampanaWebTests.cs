using System.Text;
using ChronoSalud.Tests.Falsos;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;
using Microsoft.AspNetCore.Http;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la Web del paso 23: los filtros de estudios, el PDF y la
/// campana para el personal, con el enlace de cada aviso.
/// </summary>
public class EstudiosYCampanaWebTests
{
    private static EstudioFilaViewModel Fila(string estado, string tipo = "sangre", bool paraPaciente = false) =>
        EstudioFilaViewModel.Desde(new Estudio(1, tipo, estado, new DateTime(2026, 10, 9), null,
            estado == "pendiente" ? null : "Normal.", null, null, "Hemograma", null, TienePdf: true, NombrePdf: "a.pdf"), paraPaciente);

    // ---- Filtros de estudios -----------------------------------------------------

    [Theory]
    [InlineData("pendiente", FiltroDeEstudios.Esperando, true)]
    [InlineData("validado", FiltroDeEstudios.Esperando, false)]
    [InlineData("validado", FiltroDeEstudios.ConResultado, true)]
    [InlineData("entregado", FiltroDeEstudios.ConResultado, true)]
    [InlineData("validado", FiltroDeEstudios.SinVer, true)]
    [InlineData("entregado", FiltroDeEstudios.SinVer, false)]
    [InlineData("entregado", FiltroDeEstudios.Visto, true)]
    [InlineData("pendiente", FiltroDeEstudios.Visto, false)]
    [InlineData("pendiente", null, true)]
    public void Cada_filtro_de_estado_junta_lo_que_corresponde(string estado, string? filtro, bool entra)
    {
        Assert.Equal(entra, Fila(estado).Coincide(null, filtro));
    }

    [Fact]
    public void El_filtro_de_tipo_se_suma_al_de_estado()
    {
        Assert.True(Fila("validado", "imagen").Coincide("imagen", FiltroDeEstudios.ConResultado));
        Assert.False(Fila("validado", "sangre").Coincide("imagen", null));
    }

    [Fact]
    public void Al_paciente_no_se_le_ofrece_sin_ver_y_lo_que_no_es_opcion_queda_en_todos()
    {
        Assert.DoesNotContain(FiltroDeEstudios.Opciones(paraPaciente: true), o => o.Valor == FiltroDeEstudios.SinVer);
        Assert.Contains(FiltroDeEstudios.Opciones(paraPaciente: false), o => o.Valor == FiltroDeEstudios.SinVer);

        Assert.Null(FiltroDeEstudios.Validar(FiltroDeEstudios.SinVer, paraPaciente: true));
        Assert.Null(FiltroDeEstudios.Validar("cualquiera", paraPaciente: false));
        Assert.Equal(FiltroDeEstudios.Visto, FiltroDeEstudios.Validar(FiltroDeEstudios.Visto, paraPaciente: false));
    }

    [Fact]
    public void Ningun_estudio_coincide_solo_si_hay_filtro_y_el_paciente_tiene_estudios()
    {
        Assert.True(new EstudiosIndexViewModel { Tipo = "imagen", TotalSinFiltrar = 3 }.NingunoCoincide);
        Assert.False(new EstudiosIndexViewModel { Tipo = "imagen", TotalSinFiltrar = 0 }.NingunoCoincide);
        Assert.False(new EstudiosIndexViewModel { TotalSinFiltrar = 3 }.NingunoCoincide);
    }

    [Fact]
    public void Los_resultados_nuevos_se_cuentan_solo_para_el_paciente()
    {
        var filas = new[] { Fila("validado", paraPaciente: true), Fila("entregado", paraPaciente: true), Fila("validado", paraPaciente: true) };

        Assert.Equal(2, new EstudiosIndexViewModel { EsPaciente = true, Estudios = filas }.Nuevos);
        Assert.Equal(0, new EstudiosIndexViewModel { EsPaciente = false, Estudios = filas }.Nuevos);
    }

    [Fact]
    public void La_fila_sabe_si_tiene_pdf_y_su_nombre()
    {
        var fila = Fila("validado");

        Assert.True(fila.TienePdf);
        Assert.Equal("a.pdf", fila.NombrePdf);
    }

    // ---- El PDF que se sube ----------------------------------------------------------

    [Fact]
    public void Un_pdf_valido_no_tiene_problema()
    {
        Assert.Null(EstudioResultadoViewModel.ProblemaDelPdf(Encoding.ASCII.GetBytes("%PDF-1.4 ...")));
    }

    [Fact]
    public void Un_archivo_que_no_empieza_como_pdf_se_rechaza_aunque_diga_pdf()
    {
        Assert.Contains("no es un PDF", EstudioResultadoViewModel.ProblemaDelPdf(Encoding.ASCII.GetBytes("<html>informe.pdf</html>")));
        Assert.Contains("no es un PDF", EstudioResultadoViewModel.ProblemaDelPdf(Encoding.ASCII.GetBytes("%PD")));
        Assert.Contains("vacío", EstudioResultadoViewModel.ProblemaDelPdf(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Un_pdf_de_mas_de_5_mb_se_rechaza()
    {
        var grande = new byte[EstudioResultadoViewModel.TamanoMaximoPdf + 1];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(grande, 0);

        Assert.Contains("5 MB", EstudioResultadoViewModel.ProblemaDelPdf(grande));
    }

    [Fact]
    public void El_resultado_se_carga_como_validado()
    {
        Assert.Equal("validado", new EstudioResultado("Normal.", null).Estado);
    }

    // ---- La campana --------------------------------------------------------------------

    [Theory]
    [InlineData("/Turnos/Detalle/12", true)]
    [InlineData("/Estudios?paciente=10", true)]
    [InlineData("//otro-sitio.com/robar", false)]
    [InlineData("/\\otro-sitio.com", false)]
    [InlineData("https://otro-sitio.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/Turnos/Detalle/12 onclick", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void El_enlace_de_un_aviso_tiene_que_ser_de_este_sitio(string? enlace, bool seguro)
    {
        Assert.Equal(seguro, NotificacionFilaViewModel.EsEnlaceSeguro(enlace));
    }

    [Fact]
    public void Un_enlace_que_no_es_seguro_no_llega_a_la_vista()
    {
        var fila = NotificacionFilaViewModel.Desde(new NotificacionLista(1, "turno", "x", DateTime.Now, false, "//otro.com"));

        Assert.Null(fila.Enlace);
        Assert.True(fila.EsDeTurno);
    }

    [Theory]
    [InlineData("/Turnos/Detalle/12", "Ver el turno")]
    [InlineData("/Estudios?paciente=10", "Ver los estudios")]
    [InlineData("/Recetas", "Ver el detalle")]
    public void El_texto_del_enlace_dice_adonde_lleva(string enlace, string texto)
    {
        var fila = NotificacionFilaViewModel.Desde(new NotificacionLista(1, "turno", "x", DateTime.Now, false, enlace));

        Assert.Equal(texto, fila.TextoDelEnlace);
    }

    [Theory]
    [InlineData("paciente")]
    [InlineData("doctor")]
    [InlineData("administrador")]
    public void La_campana_la_ven_los_tres_roles(string rol)
    {
        var contexto = new DefaultHttpContext();
        contexto.Session = new SesionFalsa();
        contexto.Session.GuardarSesion(new SesionUsuario("token-de-prueba", rol, 1, "Prueba"));
        var accesor = new HttpContextAccessor { HttpContext = contexto };

        Assert.True(new AuthService(new ApiClient(new HttpClient(), accesor), accesor).VeNotificaciones);
    }

    [Fact]
    public void Sin_sesion_no_hay_campana()
    {
        var contexto = new DefaultHttpContext { Session = new SesionFalsa() };
        var accesor = new HttpContextAccessor { HttpContext = contexto };

        Assert.False(new AuthService(new ApiClient(new HttpClient(), accesor), accesor).VeNotificaciones);
    }

    [Theory]
    [InlineData("doctor", "cancelaciones de tu agenda")]
    [InlineData("administrador", "falta confirmar")]
    [InlineData("paciente", "tus turnos")]
    public void El_subtitulo_dice_que_avisos_le_llegan_a_cada_rol(string rol, string parte)
    {
        Assert.Contains(parte, new NotificacionesIndexViewModel { Rol = rol }.Subtitulo);
    }
}
