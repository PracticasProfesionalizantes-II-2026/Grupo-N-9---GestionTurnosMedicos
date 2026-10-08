using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del cambio de horario semanal: quién puede cambiarlo y qué pasa
/// con los turnos que quedarían afuera.
/// </summary>
public class HorarioLaboralLogicaTests
{
    private const int IdDoctorPropio = 1;
    private const int IdUsuarioDoctor = 20;
    private const int IdOtroDoctor = 2;
    private const int Lunes = 1;

    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly RegistroMovimientosFalso _movimientos = new RegistroMovimientosFalso();
    private readonly RelojFijo _reloj = new RelojFijo();
    private readonly HorarioLaboralRepositoryFalso _horarios;

    private readonly HorarioLaboralLogica _logica;

    public HorarioLaboralLogicaTests()
    {
        _doctores.Doctores.Add(new Doctor { Id = IdDoctorPropio, IdUsuario = IdUsuarioDoctor, Activo = true });
        _doctores.Doctores.Add(new Doctor { Id = IdOtroDoctor, IdUsuario = 21, Activo = true });

        _horarios = new HorarioLaboralRepositoryFalso(_turnos);

        _logica = new HorarioLaboralLogica(
            _horarios,
            _doctores,
            _turnos,
            NullLogger<HorarioLaboralLogica>.Instance,
            _movimientos,
            _reloj);
    }

    [Fact]
    public async Task Un_doctor_no_puede_cambiar_el_horario_de_otro_doctor()
    {
        // Preparar
        var doctor = new Solicitante(IdUsuarioDoctor, "doctor");
        var pedido = HorarioDeLunes("08:00", "12:00");

        // Ejecutar
        var resultado = await _logica.Reemplazar(IdOtroDoctor, pedido, doctor);

        // Verificar: se rechaza como prohibido y no se guardó nada.
        Assert.False(resultado.ok);
        Assert.True(resultado.prohibido);
        Assert.Empty(_horarios.Horarios);
    }

    [Fact]
    public async Task Informa_los_turnos_que_quedarian_afuera_y_no_guarda_nada()
    {
        // Preparar: atiende los lunes de 8 a 12 y tiene un turno el próximo
        // lunes a las 10. El horario nuevo termina a las 9.
        CargarHorarioActualDeLunes();
        CargarTurnoDelProximoLunesALas10();
        var administrador = new Solicitante(1, "administrador");

        // Ejecutar
        var resultado = await _logica.Reemplazar(IdDoctorPropio, HorarioDeLunes("08:00", "09:00"), administrador);

        // Verificar: avisa el turno y el horario sigue terminando a las 12.
        Assert.False(resultado.ok);
        Assert.Single(resultado.conflictos);
        Assert.Equal(new TimeSpan(12, 0, 0), _horarios.Horarios.Single().HoraFin);
    }

    [Fact]
    public async Task Guarda_el_horario_si_no_deja_turnos_afuera()
    {
        // Preparar: mismo turno, pero el horario nuevo lo sigue cubriendo.
        CargarHorarioActualDeLunes();
        CargarTurnoDelProximoLunesALas10();
        var administrador = new Solicitante(1, "administrador");

        // Ejecutar
        var resultado = await _logica.Reemplazar(IdDoctorPropio, HorarioDeLunes("08:00", "13:00"), administrador);

        // Verificar: se guardó y quedó anotado en el historial.
        Assert.True(resultado.ok);
        Assert.Equal(new TimeSpan(13, 0, 0), _horarios.Horarios.Single().HoraFin);
        Assert.Contains(AccionesMovimiento.HorarioCambiado, _movimientos.Acciones);
    }

    private static HorarioSemanalDto HorarioDeLunes(string desde, string hasta)
    {
        var dias = new List<HorarioLaboralDto>();
        dias.Add(new HorarioLaboralDto(Lunes, desde, hasta));
        return new HorarioSemanalDto(dias);
    }

    private void CargarHorarioActualDeLunes()
    {
        _horarios.Horarios.Add(new HorarioLaboral
        {
            IdDoctor = IdDoctorPropio,
            DiaSemana = Lunes,
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        });
    }

    private void CargarTurnoDelProximoLunesALas10()
    {
        // El próximo lunes, contando desde mañana.
        var dia = DateTime.Today.AddDays(1);
        while (dia.DayOfWeek != DayOfWeek.Monday)
        {
            dia = dia.AddDays(1);
        }

        _turnos.Turnos.Add(new Turno
        {
            Id = 1,
            IdPaciente = 1,
            IdDoctor = IdDoctorPropio,
            FechaInicio = dia,
            HoraInicio = new TimeSpan(10, 0, 0),
            HoraFin = new TimeSpan(10, 30, 0),
            Estado = "confirmado"
        });
    }

    [Fact]
    public async Task Hoy_no_ofrece_las_franjas_que_ya_empezaron()
    {
        // Preparar: el doctor atiende hoy de 08:00 a 12:00 y son las 10:00
        // en Argentina (el reloj de la prueba).
        _reloj.Momento = DateTime.Today.AddHours(10);
        _horarios.Horarios.Add(new HorarioLaboral
        {
            IdDoctor = IdDoctorPropio,
            DiaSemana = (int)DateTime.Today.DayOfWeek,
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        });

        // Ejecutar
        var (franjas, error) = await _logica.ObtenerDisponibilidad(IdDoctorPropio, DateTime.Today);

        // Verificar: quedan de las 10:00 en adelante.
        Assert.Null(error);
        Assert.Equal(new[] { "10:00", "10:30", "11:00", "11:30" }, franjas!.Select(f => f.HoraInicio));
    }

    [Fact]
    public async Task Los_dias_disponibles_cuentan_las_franjas_libres_de_cada_dia()
    {
        // Preparar: el doctor atiende todos los días de 08:00 a 10:00 (cuatro
        // franjas) y son las 10:00 de hoy. Mañana tiene un turno a las 08:00 y
        // uno cancelado a las 08:30, que no ocupa lugar.
        _reloj.Momento = DateTime.Today.AddHours(10);
        for (var dia = 0; dia <= 6; dia++)
        {
            _horarios.Horarios.Add(new HorarioLaboral
            {
                IdDoctor = IdDoctorPropio,
                DiaSemana = dia,
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(10, 0, 0)
            });
        }
        var manana = DateTime.Today.AddDays(1);
        _turnos.Turnos.Add(TurnoDe(manana, 8, 0, "pendiente"));
        _turnos.Turnos.Add(TurnoDe(manana, 8, 30, "cancelado"));

        // Ejecutar: tres días desde hoy.
        var (dias, error) = await _logica.ObtenerDiasDisponibles(IdDoctorPropio, null, 3);

        // Verificar: hoy ya pasaron todas; mañana quedan 3; pasado, las 4.
        Assert.Null(error);
        var lista = dias!.ToList();
        Assert.Equal(new[] { DateTime.Today, manana, DateTime.Today.AddDays(2) }, lista.Select(d => d.Fecha));
        Assert.Equal(new[] { 0, 3, 4 }, lista.Select(d => d.Libres));
    }

    [Fact]
    public async Task Los_dias_disponibles_arrancan_hoy_y_no_pasan_de_31()
    {
        // Ejecutar: pide desde la semana pasada y 100 días.
        var (dias, error) = await _logica.ObtenerDiasDisponibles(IdDoctorPropio, DateTime.Today.AddDays(-7), 100);

        // Verificar: el doctor no tiene horario, así que todos dan 0.
        Assert.Null(error);
        var lista = dias!.ToList();
        Assert.Equal(31, lista.Count);
        Assert.Equal(DateTime.Today, lista[0].Fecha);
        Assert.All(lista, d => Assert.Equal(0, d.Libres));
    }

    /// <summary>Un turno de media hora del doctor de las pruebas.</summary>
    private Turno TurnoDe(DateTime dia, int hora, int minutos, string estado) => new Turno
    {
        Id = _turnos.Turnos.Count + 1,
        IdDoctor = IdDoctorPropio,
        FechaInicio = dia,
        HoraInicio = new TimeSpan(hora, minutos, 0),
        HoraFin = new TimeSpan(hora, minutos + 30, 0),
        Estado = estado
    };
}
