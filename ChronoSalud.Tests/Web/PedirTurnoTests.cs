using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la pantalla de pedir turno: en qué paso está, los enlaces que
/// arma y cómo se muestra cada día de la tira.
/// </summary>
public class PedirTurnoTests
{
    // El 8 de octubre de 2026 es jueves.
    private static readonly DateTime Jueves = new DateTime(2026, 10, 8);

    [Fact]
    public void El_paso_depende_de_lo_que_ya_se_eligio()
    {
        Assert.Equal(1, new TurnoCrearViewModel().Paso);
        Assert.Equal(2, new TurnoCrearViewModel { Especialidad = "clínica" }.Paso);
        Assert.Equal(3, new TurnoCrearViewModel { Especialidad = "clínica", IdDoctor = 4 }.Paso);

        var cualquiera = new TurnoCrearViewModel { Especialidad = "clínica", IdDoctor = TurnoCrearViewModel.Cualquiera };
        Assert.Equal(3, cualquiera.Paso);
        Assert.True(cualquiera.CualquierDoctor);
    }

    [Fact]
    public void Los_enlaces_conservan_el_paciente_elegido()
    {
        // Preparar: el personal llegó desde la ficha del paciente 7.
        var modelo = new TurnoCrearViewModel { IdPaciente = 7, Especialidad = "clínica", IdDoctor = 0, Fecha = Jueves };

        // Ejecutar
        var alDia = modelo.Ruta("clínica", 0, fecha: Jueves);
        var aConfirmar = modelo.RutaConfirmar(new FranjaViewModel { IdDoctor = 4, HoraInicio = "10:00", HoraFin = "10:30" });

        // Verificar
        Assert.Equal("7", alDia["paciente"]);
        Assert.Equal("2026-10-08", alDia["fecha"]);
        Assert.Equal("4", aConfirmar["idDoctor"]);
        Assert.Equal("10:00", aConfirmar["inicio"]);
        Assert.Equal("10:30", aConfirmar["fin"]);
        Assert.Equal("2026-10-08", aConfirmar["fecha"]);
        Assert.Equal("7", aConfirmar["paciente"]);
        // Para "Cambiar horario": se había elegido "Cualquiera".
        Assert.Equal("0", aConfirmar["elegido"]);
    }

    [Theory]
    [InlineData(null, "Ver")]
    [InlineData(0, "Sin lugar")]
    [InlineData(1, "1 libre")]
    [InlineData(5, "5 libres")]
    public void Cada_dia_dice_cuantos_horarios_libres_tiene(int? libres, string esperado)
    {
        var dia = new DiaDeLaTira { Fecha = Jueves, Libres = libres };

        Assert.Equal(esperado, dia.TextoLibres);
        Assert.Equal(libres == 0, dia.SinLugar);
    }

    [Fact]
    public void El_dia_se_muestra_abreviado_y_se_lee_completo()
    {
        var dia = new DiaDeLaTira { Fecha = Jueves, Libres = 3 };

        Assert.Equal("jue", dia.DiaCorto);
        Assert.Equal("8 oct", dia.FechaCorta);
        Assert.Equal("jueves 8 de octubre, 3 libres", dia.Descripcion);
    }

    [Theory]
    [InlineData("10:00", "10:30", true)]
    [InlineData("10:30", "10:00", false)]
    [InlineData("25:00", "10:00", false)]
    [InlineData("10", "10:30", false)]
    [InlineData(null, "10:30", false)]
    public void La_confirmacion_revisa_el_horario_del_enlace(string? inicio, string? fin, bool esperado)
    {
        Assert.Equal(esperado, TurnoConfirmarViewModel.HorarioValido(inicio, fin));
    }

    [Fact]
    public void Cambiar_horario_vuelve_al_mismo_dia_y_sin_el_paciente_si_es_el_paciente()
    {
        var personal = new TurnoConfirmarViewModel
        {
            Rol = "administrador", IdPaciente = 7, Especialidad = "clínica", DoctorElegido = 4, Fecha = Jueves
        };
        var paciente = new TurnoConfirmarViewModel
        {
            Rol = "paciente", IdPaciente = 3, Especialidad = "clínica", DoctorElegido = 0, Fecha = Jueves
        };

        Assert.Equal("7", personal.RutaCambiarHorario["paciente"]);
        Assert.Equal("4", personal.RutaCambiarHorario["idDoctor"]);
        Assert.Equal("2026-10-08", personal.RutaCambiarHorario["fecha"]);
        Assert.False(paciente.RutaCambiarHorario.ContainsKey("paciente"));
        Assert.Equal("0", paciente.RutaCambiarHorario["idDoctor"]);
    }
}
