using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del resumen de reportes (paso 22): los conteos, el ausentismo, el
/// desglose por especialidad y por doctor, y qué ve cada rol.
/// </summary>
public class ReporteLogicaTests
{
    // Dos clínicas y una cardióloga. El doctor 23 está dado de baja.
    private const int Laura = 20, Bruno = 21, Carla = 22, DadoDeBaja = 23;
    private const int IdUsuarioLaura = 3;

    private static readonly DateTime Desde = new(2026, 10, 1);
    private static readonly DateTime Hasta = new(2026, 10, 31);

    private static readonly Solicitante Admin = new(1, "administrador");
    private static readonly Solicitante DoctoraLaura = new(IdUsuarioLaura, "doctor");

    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly ReporteLogica _logica;

    public ReporteLogicaTests()
    {
        _logica = new ReporteLogica(_turnos, _doctores);

        Doctor(Laura, IdUsuarioLaura, "Laura", "Gomez", "Clínica");
        Doctor(Bruno, 4, "Bruno", "Diaz", "clínica ");
        Doctor(Carla, 5, "Carla", "Ruiz", "Cardiología");
        Doctor(DadoDeBaja, 6, "Hugo", "Paz", "Cardiología", activo: false);
    }

    private void Doctor(int id, int idUsuario, string nombre, string apellido, string especialidad, bool activo = true) =>
        _doctores.Doctores.Add(new Doctor
        {
            Id = id,
            IdUsuario = idUsuario,
            Especialidad = especialidad,
            Activo = activo,
            Usuario = new Usuario { Id = idUsuario, Nombre = nombre, Apellido = apellido, Rol = "doctor" }
        });

    private void Turno(int idDoctor, string estado, int dia = 10, int idPaciente = 50)
    {
        _turnos.Turnos.Add(new Turno
        {
            Id = _turnos.Turnos.Count + 1,
            IdDoctor = idDoctor,
            IdPaciente = idPaciente,
            FechaInicio = new DateTime(2026, 10, dia),
            HoraInicio = new TimeSpan(10, 0, 0),
            Estado = estado,
            Doctor = _doctores.Doctores.Single(d => d.Id == idDoctor)
        });
    }

    [Fact]
    public async Task Cuenta_cada_estado_y_el_total()
    {
        Turno(Laura, "pendiente");
        Turno(Laura, "confirmado");
        Turno(Laura, "completado");
        Turno(Bruno, "completado");
        Turno(Bruno, "ausente");
        Turno(Carla, "cancelado");

        var (reporte, error, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Null(error);
        var turnos = reporte!.Turnos;
        Assert.Equal(6, turnos.Total);
        Assert.Equal((1, 1, 2, 1, 1), (turnos.Pendientes, turnos.Confirmados, turnos.Completados, turnos.Ausentes, turnos.Cancelados));
    }

    [Theory]
    [InlineData(3, 1, 25.0)]
    [InlineData(2, 1, 33.3)]
    [InlineData(1, 2, 66.7)]
    [InlineData(4, 0, 0.0)]
    public void El_ausentismo_es_sobre_completados_y_ausentes(int completados, int ausentes, double esperado)
    {
        Assert.Equal(esperado, ReporteLogica.Ausentismo(completados, ausentes));
    }

    [Fact]
    public void Sin_turnos_cerrados_no_hay_ausentismo()
    {
        Assert.Null(ReporteLogica.Ausentismo(0, 0));
    }

    [Fact]
    public async Task Los_pendientes_y_cancelados_no_mueven_el_ausentismo()
    {
        Turno(Laura, "completado");
        Turno(Laura, "ausente");
        Turno(Laura, "pendiente");
        Turno(Laura, "cancelado");

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Equal(50.0, reporte!.Turnos.Ausentismo);
    }

    [Fact]
    public async Task Solo_cuenta_los_turnos_del_periodo_con_los_dos_dias_incluidos()
    {
        Turno(Laura, "pendiente", dia: 1);
        Turno(Laura, "pendiente", dia: 31);
        _turnos.Turnos.Add(new Turno { Id = 99, IdDoctor = Laura, FechaInicio = new DateTime(2026, 11, 1), Estado = "pendiente" });
        _turnos.Turnos.Add(new Turno { Id = 98, IdDoctor = Laura, FechaInicio = new DateTime(2026, 9, 30), Estado = "pendiente" });

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Equal(2, reporte!.Turnos.Total);
    }

    [Fact]
    public async Task Los_pacientes_atendidos_se_cuentan_una_vez()
    {
        Turno(Laura, "completado", idPaciente: 50);
        Turno(Bruno, "completado", idPaciente: 50);
        Turno(Laura, "completado", idPaciente: 51);
        Turno(Laura, "ausente", idPaciente: 52);

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Equal(2, reporte!.PacientesAtendidos);
    }

    [Fact]
    public async Task Por_especialidad_junta_sin_mirar_mayusculas_ni_espacios()
    {
        Turno(Laura, "completado");
        Turno(Bruno, "ausente");
        Turno(Carla, "completado");

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Equal(new[] { "Clínica", "Cardiología" }, reporte!.PorEspecialidad.Select(e => e.Especialidad));
        var clinica = reporte.PorEspecialidad[0].Turnos;
        Assert.Equal((2, 1, 1, 50.0), (clinica.Total, clinica.Completados, clinica.Ausentes, clinica.Ausentismo!.Value));
    }

    [Fact]
    public async Task Por_doctor_va_de_mas_turnos_a_menos_e_incluye_a_los_que_no_tuvieron()
    {
        Turno(Carla, "pendiente");
        Turno(Carla, "pendiente");
        Turno(Laura, "pendiente");

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        Assert.Equal(new[] { "Carla Ruiz", "Laura Gomez", "Bruno Diaz" }, reporte!.PorDoctor.Select(d => d.Doctor));
        Assert.Equal(0, reporte.PorDoctor[2].Turnos.Total);
    }

    [Fact]
    public async Task Un_doctor_dado_de_baja_aparece_solo_si_tuvo_turnos_y_marcado()
    {
        var (sinTurnos, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);
        Assert.DoesNotContain(sinTurnos!.PorDoctor, d => d.IdDoctor == DadoDeBaja);

        Turno(DadoDeBaja, "completado");
        var (conTurnos, _, _, _) = await _logica.Resumen(Desde, Hasta, null, null, Admin);

        var fila = Assert.Single(conTurnos!.PorDoctor, d => d.IdDoctor == DadoDeBaja);
        Assert.False(fila.Activo);
        Assert.Equal("Hugo Paz", fila.Doctor);
        Assert.Equal(conTurnos.Turnos.Total, conTurnos.PorDoctor.Sum(d => d.Turnos.Total));
    }

    [Fact]
    public async Task El_admin_filtra_por_especialidad()
    {
        Turno(Laura, "completado");
        Turno(Bruno, "completado");
        Turno(Carla, "completado");

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, null, " CLÍNICA ", Admin);

        Assert.Equal(2, reporte!.Turnos.Total);
        Assert.Equal("CLÍNICA", reporte.Especialidad);
        Assert.Equal(new[] { Bruno, Laura }.OrderBy(i => i), reporte.PorDoctor.Select(d => d.IdDoctor).OrderBy(i => i));
    }

    [Fact]
    public async Task El_admin_filtra_por_doctor()
    {
        Turno(Laura, "completado");
        Turno(Carla, "completado");

        var (reporte, _, _, _) = await _logica.Resumen(Desde, Hasta, Carla, null, Admin);

        Assert.Equal(1, reporte!.Turnos.Total);
        Assert.Equal(Carla, Assert.Single(reporte.PorDoctor).IdDoctor);
        Assert.Equal(Carla, reporte.IdDoctor);
    }

    [Fact]
    public async Task Un_doctor_que_no_existe_da_no_encontrado()
    {
        var (reporte, error, noEncontrado, _) = await _logica.Resumen(Desde, Hasta, 999, null, Admin);

        Assert.Null(reporte);
        Assert.True(noEncontrado);
        Assert.Equal("Doctor no encontrado.", error);
    }

    [Fact]
    public async Task El_doctor_ve_solo_lo_suyo_aunque_pida_otro_doctor_u_otra_especialidad()
    {
        Turno(Laura, "completado");
        Turno(Laura, "ausente");
        Turno(Carla, "completado");

        var (reporte, error, _, _) = await _logica.Resumen(Desde, Hasta, Carla, "Cardiología", DoctoraLaura);

        Assert.Null(error);
        Assert.Equal(Laura, reporte!.IdDoctor);
        Assert.Null(reporte.Especialidad);
        Assert.Equal(2, reporte.Turnos.Total);
        Assert.Equal(Laura, Assert.Single(reporte.PorDoctor).IdDoctor);
    }

    [Fact]
    public async Task Un_doctor_sin_perfil_da_no_encontrado()
    {
        var (_, error, noEncontrado, _) = await _logica.Resumen(Desde, Hasta, null, null, new Solicitante(77, "doctor"));

        Assert.True(noEncontrado);
        Assert.Contains("perfil de doctor", error);
    }

    [Fact]
    public async Task El_paciente_no_ve_reportes()
    {
        var (reporte, _, _, prohibido) = await _logica.Resumen(Desde, Hasta, null, null, new Solicitante(9, "paciente"));

        Assert.Null(reporte);
        Assert.True(prohibido);
    }

    [Fact]
    public async Task Sin_fechas_o_con_el_rango_invertido_es_un_pedido_mal_armado()
    {
        var (_, sinFechas, noEncontrado, _) = await _logica.Resumen(null, Hasta, null, null, Admin);
        Assert.NotNull(sinFechas);
        Assert.False(noEncontrado);

        var (_, invertido, _, _) = await _logica.Resumen(Hasta, Desde, null, null, Admin);
        Assert.Equal("fecha_hasta no puede ser anterior a fecha_desde.", invertido);
    }

    [Fact]
    public async Task El_periodo_puede_ser_de_hasta_366_dias()
    {
        var inicio = new DateTime(2028, 1, 1);

        var (unAnio, error, _, _) = await _logica.Resumen(inicio, inicio.AddDays(365), null, null, Admin);
        Assert.Null(error);
        Assert.NotNull(unAnio);

        var (_, demasiado, _, _) = await _logica.Resumen(inicio, inicio.AddDays(366), null, null, Admin);
        Assert.NotNull(demasiado);
    }

    [Fact]
    public async Task Las_fechas_se_toman_como_dias_sin_hora()
    {
        Turno(Laura, "pendiente", dia: 31);

        var (reporte, _, _, _) = await _logica.Resumen(Desde.AddHours(15), Hasta.AddHours(3), null, null, Admin);

        Assert.Equal(Desde, reporte!.Desde);
        Assert.Equal(Hasta, reporte.Hasta);
        Assert.Equal(1, reporte.Turnos.Total);
    }
}
