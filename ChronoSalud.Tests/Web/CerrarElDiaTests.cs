using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de cerrar el día (paso 15): completado y ausente desde la lista,
/// la confirmación y los turnos sin cerrar.
/// </summary>
public class CerrarElDiaTests
{
    private static readonly DateTime Ahora = new DateTime(2026, 10, 9, 11, 0, 0);

    private static TurnoFilaViewModel Fila(string estado, DateTime fecha, string hora) => new()
    {
        IdTurno = 12,
        Estado = estado,
        FechaInicio = fecha,
        Hora = hora,
        Paciente = "Ana Duarte",
        Ahora = Ahora
    };

    [Fact]
    public void La_fila_ofrece_completado_y_ausente_solo_si_sigue_en_pie_y_ya_empezo()
    {
        var hoyMasTemprano = Fila("confirmado", Ahora.Date, "10:00");
        var deAyer = Fila("pendiente", Ahora.Date.AddDays(-1), "17:00");
        var masTarde = Fila("confirmado", Ahora.Date, "15:00");
        var yaCompletado = Fila("completado", Ahora.Date, "09:00");

        Assert.True(hoyMasTemprano.PuedeCerrarse);
        Assert.True(deAyer.PuedeCerrarse);
        Assert.False(masTarde.PuedeCerrarse);
        Assert.False(yaCompletado.PuedeCerrarse);
    }

    [Theory]
    [InlineData("completado", true)]
    [InlineData("ausente", true)]
    [InlineData("confirmado", false)]
    [InlineData("cancelado", false)]
    [InlineData(null, false)]
    public void Solo_completado_y_ausente_pasan_por_la_confirmacion(string? estado, bool valido)
    {
        Assert.Equal(valido, TurnoMarcarViewModel.EsEstadoValido(estado));
    }

    [Fact]
    public void La_confirmacion_dice_que_turno_y_que_va_a_pasar()
    {
        var turno = new TurnoDetalleViewModel
        {
            IdTurno = 12,
            PacienteNombre = "Ana Duarte",
            FechaInicio = new DateTime(2026, 10, 9),
            HoraInicio = "10:00",
            Estado = "confirmado"
        };

        var ausente = new TurnoMarcarViewModel { Turno = turno, Estado = "ausente" };
        var completado = new TurnoMarcarViewModel { Turno = turno, Estado = "completado" };

        Assert.Equal("¿Marcar ausente el turno #12?", ausente.Titulo);
        Assert.Equal("El turno de Ana Duarte del viernes 9 de octubre de 2026 a las 10:00 queda como ausente: el paciente no vino.", ausente.Pregunta);
        Assert.Equal("Sí, marcar ausente", ausente.TextoBoton);

        Assert.Equal("¿Marcar como completado el turno #12?", completado.Titulo);
        Assert.Equal("Sí, marcar como completado", completado.TextoBoton);
    }

    [Fact]
    public void Sin_cerrar_son_los_de_hasta_ayer_pendientes_o_confirmados()
    {
        Assert.Equal(new DateTime(2026, 10, 8), TurnosSinCerrar.Hasta(new DateTime(2026, 10, 9, 15, 30, 0)));
        Assert.Equal("pendiente,confirmado", TurnosSinCerrar.Estados);
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData(0, true, null)]
    [InlineData(1, true, "Tenés 1 turno de días anteriores sin cerrar.")]
    [InlineData(3, true, "Tenés 3 turnos de días anteriores sin cerrar.")]
    [InlineData(5, false, "Hay 5 turnos de días anteriores sin cerrar.")]
    public void El_aviso_del_inicio_sale_solo_si_hay_alguno(int? cantidad, bool esDoctor, string? texto)
    {
        Assert.Equal(texto, TurnosSinCerrar.Aviso(cantidad, esDoctor));
    }

    [Fact]
    public void El_aviso_lleva_al_listado_sin_cerrar()
    {
        var enlace = TurnosSinCerrar.Enlace;

        Assert.Equal("Turnos", enlace.Controlador);
        Assert.Equal("Index", enlace.Accion);
        Assert.Equal("sin-cerrar", enlace.Ruta!["ver"]);
    }

    [Fact]
    public void En_sin_cerrar_las_rutas_conservan_solo_el_modo_el_orden_y_la_pagina()
    {
        // Aunque lleguen estado y fechas, en este modo no cuentan.
        var filtros = new TurnosFiltroViewModel
        {
            SinCerrar = true,
            Estado = "pendiente",
            Desde = new DateTime(2026, 10, 1),
            Orden = "paciente",
            Pagina = 1
        };

        var ruta = filtros.RutaDePagina(2);

        Assert.Equal("sin-cerrar", ruta["ver"]);
        Assert.Equal("paciente", ruta["orden"]);
        Assert.Equal("2", ruta["pagina"]);
        Assert.False(ruta.ContainsKey("estado"));
        Assert.False(ruta.ContainsKey("desde"));
        Assert.False(filtros.SoloProximos);
    }

    [Fact]
    public void El_titulo_cambia_en_sin_cerrar()
    {
        var modelo = new TurnosIndexViewModel { Rol = "doctor", Filtros = new TurnosFiltroViewModel { SinCerrar = true } };

        Assert.Equal("Turnos sin cerrar", modelo.Titulo);
        Assert.StartsWith("Turnos de días anteriores", modelo.Subtitulo);
    }
}
