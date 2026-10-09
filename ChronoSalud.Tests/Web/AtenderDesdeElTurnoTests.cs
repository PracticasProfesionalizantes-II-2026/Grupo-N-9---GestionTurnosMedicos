using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la atención desde el turno y de la edición de recetas y
/// entradas de la historia clínica (paso 11).
/// </summary>
public class AtenderDesdeElTurnoTests
{
    private static TurnoDetalle Turno(string estado, DateTime fecha) =>
        new TurnoDetalle(12, fecha, "10:00", "10:30", estado, 10, 20, null);

    [Theory]
    [InlineData("pendiente", true)]
    [InlineData("confirmado", true)]
    [InlineData("completado", true)]
    [InlineData("cancelado", false)]
    [InlineData("ausente", false)]
    public void Se_atiende_un_turno_que_no_esta_cancelado_ni_ausente(string estado, bool seAtiende)
    {
        var detalle = new TurnoDetalleViewModel { Estado = estado };

        Assert.Equal(seAtiende, detalle.SePuedeAtender);
    }

    [Fact]
    public void El_turno_atendido_se_describe_con_numero_dia_y_hora()
    {
        var atendido = TurnoAtendidoViewModel.Desde(Turno("confirmado", new DateTime(2026, 10, 9)));

        Assert.Equal("turno #12 del 9/10/2026 a las 10:00", atendido.Descripcion);
        Assert.Equal(10, atendido.IdPaciente);
    }

    [Fact]
    public void Un_turno_que_ya_empezo_se_puede_completar_y_uno_futuro_no()
    {
        var pasado = TurnoAtendidoViewModel.Desde(Turno("confirmado", new DateTime(2020, 1, 1)));
        var futuro = TurnoAtendidoViewModel.Desde(Turno("confirmado", new DateTime(2099, 1, 1)));

        Assert.True(pasado.PuedeCompletarse);
        Assert.False(futuro.PuedeCompletarse);
    }

    [Fact]
    public void La_casilla_de_completar_sale_solo_en_una_consulta_nueva_de_un_turno_que_empezo()
    {
        var empezado = TurnoAtendidoViewModel.Desde(Turno("confirmado", new DateTime(2020, 1, 1)));
        var futuro = TurnoAtendidoViewModel.Desde(Turno("confirmado", new DateTime(2099, 1, 1)));

        var nueva = new EntradaHistorialCrearViewModel { IdTurno = 12, Turno = empezado };
        var deUnTurnoFuturo = new EntradaHistorialCrearViewModel { IdTurno = 12, Turno = futuro };
        var edicion = new EntradaHistorialCrearViewModel { IdHistorial = 5, IdTurno = 12, Turno = empezado };

        Assert.True(nueva.MostrarCompletarTurno);
        Assert.False(deUnTurnoFuturo.MostrarCompletarTurno);
        Assert.False(edicion.MostrarCompletarTurno);
    }

    [Fact]
    public void Desde_un_turno_o_al_editar_el_paciente_no_se_cambia()
    {
        Assert.True(new EntradaHistorialCrearViewModel().PuedeCambiarPaciente);
        Assert.False(new EntradaHistorialCrearViewModel { IdTurno = 12 }.PuedeCambiarPaciente);
        Assert.False(new EntradaHistorialCrearViewModel { IdHistorial = 5 }.PuedeCambiarPaciente);

        Assert.True(new RecetaCrearViewModel().PuedeCambiarPaciente);
        Assert.False(new RecetaCrearViewModel { IdTurno = 12 }.PuedeCambiarPaciente);
        Assert.False(new RecetaCrearViewModel { IdReceta = 7 }.PuedeCambiarPaciente);
    }

    [Fact]
    public void Editar_una_receta_carga_el_formulario_con_sus_datos()
    {
        var receta = new Receta(
            7,
            new DateTime(2026, 10, 9),
            new DateTime(2026, 11, 8),
            "Tomar con las comidas.",
            new[]
            {
                new MedicamentoRecetado(1, "IBUPIRAC", "IBUPROFENO", "400 mg", "COMPRIMIDO",
                    "1 comprimido", "cada 8 horas", "5 días", "Con agua."),
                new MedicamentoRecetado(5, MedicamentoService.NombreMarcadorOtro, null, null, null,
                    "1 sobre", "cada 12 horas", null,
                    IndicacionesDeOtro.Componer("Sales de rehidratación", "Disolver en agua."))
            },
            new Profesional(20, "Laura", "Méndez", "Clínica médica", "MN 1234"),
            IdTurno: 12);

        var modelo = RecetaCrearViewModel.DesdeReceta(receta, idPaciente: 10);

        Assert.True(modelo.EsEdicion);
        Assert.Equal(7, modelo.IdReceta);
        Assert.Equal(12, modelo.IdTurno);
        Assert.Equal(10, modelo.IdPaciente);
        Assert.Equal(new DateTime(2026, 11, 8), modelo.Vigencia);
        Assert.Equal("Tomar con las comidas.", modelo.Detalles);

        var comun = modelo.Medicamentos[0];
        Assert.Equal(1, comun.IdMedicamento);
        Assert.Equal("1 comprimido", comun.Dosis);
        Assert.Equal("Con agua.", comun.Indicaciones);

        // La fila "Otro..." vuelve a separar el nombre de las indicaciones.
        var otro = modelo.Medicamentos[1];
        Assert.True(otro.EsOtro);
        Assert.Equal("Sales de rehidratación", otro.NombreOtro);
        Assert.Equal("Disolver en agua.", otro.Indicaciones);
    }

    [Fact]
    public void La_firma_sabe_que_doctor_la_escribio()
    {
        var firma = FirmaViewModel.Desde(new Profesional(20, "Laura", "Méndez", "Clínica médica", "MN 1234"));

        Assert.Equal(20, firma!.IdDoctor);
    }
}
