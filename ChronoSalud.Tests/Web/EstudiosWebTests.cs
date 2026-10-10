using ChronoSalud.Tests.Falsos;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;
using Microsoft.AspNetCore.Http;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de las pantallas de estudios (paso 16): cómo se muestra cada uno,
/// el enlace al archivo y quién puede pedir o cargar resultados.
/// </summary>
public class EstudiosWebTests
{
    private static Estudio UnEstudio(string estado = "pendiente", string descripcion = "Hemograma completo", string? archivo = null) =>
        new Estudio(
            5,
            "sangre",
            estado,
            new DateTime(2026, 10, 9),
            12,
            estado == "pendiente" ? null : "Valores normales.",
            archivo,
            estado == "pendiente" ? null : new DateTime(2026, 10, 12),
            descripcion,
            new Profesional(20, "Laura", "Méndez", "Clínica médica", "MN 1234"));

    [Fact]
    public void Un_estudio_pendiente_espera_su_resultado()
    {
        var fila = EstudioFilaViewModel.Desde(UnEstudio());

        Assert.False(fila.TieneResultado);
        Assert.Equal("Esperando resultado", fila.EstadoTexto);
        Assert.Equal(TonoChip.Aviso, fila.Tono);
        Assert.Equal("Hemograma completo", fila.Titulo);
        Assert.Equal("Análisis de sangre", fila.TipoTexto);
        Assert.Equal("9 oct 2026", fila.FechaSolicitudTexto);
        Assert.Equal("Laura Méndez", fila.PedidoPor!.NombreCompleto);
    }

    [Theory]
    [InlineData("validado")]
    [InlineData("entregado")]
    public void Validado_o_entregado_es_resultado_disponible(string estado)
    {
        var fila = EstudioFilaViewModel.Desde(UnEstudio(estado));

        Assert.True(fila.TieneResultado);
        Assert.Equal("Resultado disponible", fila.EstadoTexto);
        Assert.Equal(TonoChip.Exito, fila.Tono);
        Assert.Equal("12 oct 2026", fila.FechaResultadoTexto);
    }

    [Fact]
    public void Sin_descripcion_el_titulo_es_el_tipo()
    {
        var fila = EstudioFilaViewModel.Desde(UnEstudio(descripcion: "  "));

        Assert.Equal("Análisis de sangre", fila.Titulo);
    }

    [Theory]
    [InlineData("https://laboratorio.com/informe/123", true)]
    [InlineData("  https://laboratorio.com/a.pdf  ", true)]
    [InlineData("http://laboratorio.com/a.pdf", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("laboratorio.com/a.pdf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void El_enlace_al_archivo_tiene_que_ser_https(string? enlace, bool valido)
    {
        Assert.Equal(valido, EstudioResultadoViewModel.EsEnlaceValido(enlace));
    }

    [Fact]
    public void La_vista_solo_arma_el_enlace_si_es_seguro()
    {
        var seguro = EstudioFilaViewModel.Desde(UnEstudio("entregado", archivo: "https://lab.com/a.pdf"));
        var raro = EstudioFilaViewModel.Desde(UnEstudio("entregado", archivo: "javascript:alert(1)"));

        Assert.Equal("https://lab.com/a.pdf", seguro.ArchivoSeguro);
        Assert.Null(raro.ArchivoSeguro);
    }

    [Theory]
    [InlineData("sangre", true)]
    [InlineData("imagen", true)]
    [InlineData("biopsia", true)]
    [InlineData("otro", true)]
    [InlineData("radiografia", false)]
    [InlineData(null, false)]
    public void Solo_se_aceptan_los_tipos_de_la_api(string? tipo, bool valido)
    {
        Assert.Equal(valido, EstudioCrearViewModel.EsTipoValido(tipo));
    }

    [Theory]
    [InlineData("paciente", false, false)]
    [InlineData("doctor", true, true)]
    [InlineData("administrador", false, true)]
    public void Pide_el_doctor_y_cargan_resultados_doctor_y_administracion(string rol, bool pide, bool carga)
    {
        var contexto = new DefaultHttpContext();
        contexto.Session = new SesionFalsa();
        contexto.Session.GuardarSesion(new SesionUsuario("token-de-prueba", rol, 1, "Prueba"));
        var accesor = new HttpContextAccessor { HttpContext = contexto };
        var auth = new AuthService(new ApiClient(new HttpClient(), accesor), accesor);

        Assert.Equal(pide, auth.PuedePedirEstudios);
        Assert.Equal(carga, auth.PuedeCargarResultados);
    }

    [Fact]
    public void Con_la_api_anterior_sin_descripcion_ni_doctor_se_muestra_el_tipo()
    {
        // Mientras se publica, la Web nueva puede hablar con la API vieja,
        // que no manda "descripcion" ni "doctor".
        const string json = """
            { "idEstudio": 5, "tipo": "imagen", "estado": "pendiente", "fechaSolicitud": "2026-10-09T00:00:00",
              "idTurno": null, "resultado": null, "archivoUrl": null, "fechaResultado": null }
            """;
        var estudio = System.Text.Json.JsonSerializer.Deserialize<Estudio>(json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var fila = EstudioFilaViewModel.Desde(estudio!);

        Assert.Equal("Estudio por imágenes", fila.Titulo);
        Assert.Null(fila.PedidoPor);
    }

    [Fact]
    public void La_notificacion_de_un_estudio_lleva_a_mis_estudios()
    {
        var fila = NotificacionFilaViewModel.Desde(
            new NotificacionLista(1, "estudio", "Ya está tu resultado.", new DateTime(2026, 10, 9), false));

        Assert.True(fila.EsDeEstudio);
        Assert.False(fila.EsDeTurno);
        Assert.Equal("estudio", fila.Icono);
    }
}
