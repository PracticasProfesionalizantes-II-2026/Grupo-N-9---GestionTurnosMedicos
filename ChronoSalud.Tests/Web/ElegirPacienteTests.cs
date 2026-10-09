using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas del buscador de pacientes que reemplaza a los desplegables
/// (partial _ElegirPaciente, paso 12).
/// </summary>
public class ElegirPacienteTests
{
    private static readonly PacienteLista Ana =
        new PacienteLista(1, "Ana", "Duarte", "ana.duarte@mail.com", "30111222");

    private static SeleccionDePaciente Resultados(int total, params PacienteLista[] pacientes) =>
        new SeleccionDePaciente(null, pacientes, total);

    [Fact]
    public void Un_resultado_conserva_los_otros_datos_de_la_pantalla_y_suma_el_paciente()
    {
        var ruta = new Dictionary<string, string> { ["idDoctor"] = "3", ["inicio"] = "10:00", ["paciente"] = "9", ["buscar"] = "x" };

        var modelo = ElegirPacienteViewModel.Desde(Resultados(1, Ana), "Turnos", "Confirmar", "ana", ruta);

        // "paciente" y "buscar" no se arrastran: los pone el buscador.
        Assert.False(modelo.Ruta.ContainsKey("paciente"));
        Assert.False(modelo.Ruta.ContainsKey("buscar"));

        var enlace = modelo.RutaCon(Ana.IdPaciente);
        Assert.Equal("3", enlace["idDoctor"]);
        Assert.Equal("10:00", enlace["inicio"]);
        Assert.Equal("1", enlace["paciente"]);

        Assert.False(modelo.RutaSinPaciente.ContainsKey("paciente"));
        Assert.Equal("3", modelo.RutaSinPaciente["idDoctor"]);
    }

    [Fact]
    public void Lo_buscado_se_guarda_sin_espacios_y_vacio_es_null()
    {
        Assert.Equal("ana", ElegirPacienteViewModel.Desde(Resultados(0), "Recetas", "Index", "  ana ").Buscar);
        Assert.Null(ElegirPacienteViewModel.Desde(Resultados(0), "Recetas", "Index", "   ").Buscar);
    }

    [Fact]
    public void Avisa_cuando_hay_mas_resultados_que_los_que_se_muestran()
    {
        Assert.True(ElegirPacienteViewModel.Desde(Resultados(154, Ana), "Recetas", "Index").HayMas);
        Assert.False(ElegirPacienteViewModel.Desde(Resultados(1, Ana), "Recetas", "Index").HayMas);
    }

    [Fact]
    public void Los_datos_de_cada_paciente_omiten_lo_que_falta()
    {
        Assert.Equal("DNI 30111222 · ana.duarte@mail.com", ElegirPacienteViewModel.DatosDe(Ana));
        Assert.Equal("DNI 30111222", ElegirPacienteViewModel.DatosDe(Ana with { Email = null }));
        Assert.Null(ElegirPacienteViewModel.DatosDe(new PacienteLista(2, "Bruno", "Salas")));
        Assert.Equal("Ana Duarte", ElegirPacienteViewModel.NombreDe(Ana));
        Assert.Equal("Paciente #7", ElegirPacienteViewModel.NombreDe(new PacienteLista(7, "", "")));
    }

    [Fact]
    public void Con_un_paciente_elegido_lo_muestra_y_dice_si_se_puede_cambiar()
    {
        var elegido = new SeleccionDePaciente(Ana, Array.Empty<PacienteLista>(), 0);

        var nueva = ElegirPacienteViewModel.Desde(elegido, "Recetas", "Crear");
        var desdeTurno = ElegirPacienteViewModel.Desde(elegido, "Recetas", "Crear", puedeCambiar: false);

        Assert.Equal(Ana, nueva.Elegido);
        Assert.True(nueva.PuedeCambiar);
        Assert.False(desdeTurno.PuedeCambiar);
    }

    [Fact]
    public void Confirmar_un_turno_muestra_el_formulario_cuando_ya_hay_paciente()
    {
        var paciente = new TurnoConfirmarViewModel { Rol = "paciente" };
        var personalSinElegir = new TurnoConfirmarViewModel { Rol = "doctor" };
        var personalConPaciente = new TurnoConfirmarViewModel { Rol = "administrador", IdPaciente = 4 };

        Assert.True(paciente.MostrarFormulario);
        Assert.False(personalSinElegir.MostrarFormulario);
        Assert.True(personalConPaciente.MostrarFormulario);
    }

    [Fact]
    public void El_buscador_de_confirmar_vuelve_con_el_mismo_horario()
    {
        var modelo = new TurnoConfirmarViewModel
        {
            Rol = "doctor",
            IdDoctor = 3,
            Fecha = new DateTime(2026, 10, 12),
            HoraInicio = "10:00",
            HoraFin = "10:30",
            Especialidad = "Clínica",
            DoctorElegido = 3
        };

        var ruta = modelo.RutaConfirmar;

        Assert.Equal("3", ruta["idDoctor"]);
        Assert.Equal("2026-10-12", ruta["fecha"]);
        Assert.Equal("10:00", ruta["inicio"]);
        Assert.Equal("10:30", ruta["fin"]);
        Assert.Equal("Clínica", ruta["especialidad"]);
        Assert.Equal("3", ruta["elegido"]);
    }

    [Fact]
    public void Las_fechas_de_la_historia_se_conservan_al_cambiar_de_paciente()
    {
        var filtros = new HistorialFiltroViewModel { Desde = new DateTime(2026, 1, 1) };

        Assert.Equal("2026-01-01", filtros.Ruta["desde"]);
        Assert.False(filtros.Ruta.ContainsKey("hasta"));
    }
}
