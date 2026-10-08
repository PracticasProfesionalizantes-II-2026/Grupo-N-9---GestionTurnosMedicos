using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de TurnoLogica: reservar y cancelar. Cada prueba arma sus datos en
/// los repositorios falsos (Preparar), llama a un método (Ejecutar) y revisa
/// el resultado (Verificar).
/// </summary>
public class TurnoLogicaTests
{
    // Los datos que comparten todas las pruebas: dos pacientes y un doctor.
    private const int IdUsuarioPaciente = 10;
    private const int IdPacientePropio = 1;
    private const int IdPacienteAjeno = 2;
    private const int IdDoctor = 5;

    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly NotificacionRepositoryFalso _notificaciones = new NotificacionRepositoryFalso();
    private readonly HorarioLaboralLogicaFalsa _horarios = new HorarioLaboralLogicaFalsa();
    private readonly RegistroMovimientosFalso _movimientos = new RegistroMovimientosFalso();
    private readonly RelojFijo _reloj = new RelojFijo();

    private readonly TurnoLogica _logica;

    // xUnit crea una instancia nueva de esta clase para cada prueba, así que
    // cada una arranca con los datos limpios.
    public TurnoLogicaTests()
    {
        _pacientes.Pacientes.Add(new Paciente
        {
            Id = IdPacientePropio,
            IdUsuario = IdUsuarioPaciente,
            Usuario = new Usuario { Id = IdUsuarioPaciente }
        });
        _pacientes.Pacientes.Add(new Paciente { Id = IdPacienteAjeno, IdUsuario = 11 });
        _doctores.Doctores.Add(new Doctor { Id = IdDoctor, IdUsuario = 20, Activo = true });

        _logica = new TurnoLogica(
            _turnos,
            _pacientes,
            _doctores,
            _notificaciones,
            NullLogger<TurnoLogica>.Instance,
            _horarios,
            _movimientos,
            _reloj);
    }

    [Fact]
    public async Task El_paciente_reserva_a_su_nombre_aunque_mande_el_id_de_otro()
    {
        // Preparar: el paciente pide un turno con el id de otro paciente.
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var pedido = new TurnoCreateDto(IdPacienteAjeno, IdDoctor, DateTime.Today.AddDays(1), "10:00", "10:30", null);

        // Ejecutar
        var (id, error) = await _logica.Crear(pedido, paciente);

        // Verificar: se creó, y a nombre de quien llamó.
        Assert.Null(error);
        var creado = _turnos.Turnos.Single(t => t.Id == id);
        Assert.Equal(IdPacientePropio, creado.IdPaciente);
        Assert.Equal("pendiente", creado.Estado);
    }

    [Fact]
    public async Task No_se_reserva_si_el_horario_del_doctor_no_lo_permite()
    {
        // Preparar: la validación del horario rechaza el pedido.
        _horarios.ErrorDeHorario = "El doctor no atiende los domingos.";
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var pedido = new TurnoCreateDto(0, IdDoctor, DateTime.Today.AddDays(1), "10:00", "10:30", null);

        // Ejecutar
        var (id, error) = await _logica.Crear(pedido, paciente);

        // Verificar: vuelve el mensaje del horario y no se creó nada.
        Assert.Null(id);
        Assert.Equal("El doctor no atiende los domingos.", error);
        Assert.Empty(_turnos.Turnos);
    }

    [Fact]
    public async Task No_se_reserva_una_franja_que_ya_esta_ocupada()
    {
        // Preparar: ya hay un turno de ese doctor a esa hora.
        var fecha = DateTime.Today.AddDays(1);
        AgregarTurno(IdPacienteAjeno, "confirmado", fecha);
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var pedido = new TurnoCreateDto(0, IdDoctor, fecha, "10:00", "10:30", null);

        // Ejecutar
        var (id, error) = await _logica.Crear(pedido, paciente);

        // Verificar
        Assert.Null(id);
        Assert.Contains("Conflicto", error);
    }

    [Fact]
    public async Task Cancelar_un_turno_ajeno_responde_que_no_existe()
    {
        // Preparar: el turno es de otro paciente.
        var turno = AgregarTurno(IdPacienteAjeno, "pendiente", DateTime.Today.AddDays(1));
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");

        // Ejecutar
        var (ok, error) = await _logica.Cancelar(turno.Id, paciente);

        // Verificar: se contesta como si no existiera y el turno no cambió.
        Assert.False(ok);
        Assert.Equal("Turno no encontrado.", error);
        Assert.Equal("pendiente", turno.Estado);
    }

    [Fact]
    public async Task Cancelar_un_turno_completado_da_conflicto()
    {
        // Preparar
        var turno = AgregarTurno(IdPacientePropio, "completado", DateTime.Today);
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");

        // Ejecutar
        var (ok, error) = await _logica.Cancelar(turno.Id, paciente);

        // Verificar
        Assert.False(ok);
        Assert.Contains("Conflicto", error);
        Assert.Equal("completado", turno.Estado);
    }

    [Fact]
    public async Task Cancelar_un_turno_propio_lo_deja_cancelado_y_avisa_al_paciente()
    {
        // Preparar
        var turno = AgregarTurno(IdPacientePropio, "pendiente", DateTime.Today.AddDays(1));
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");

        // Ejecutar
        var (ok, error) = await _logica.Cancelar(turno.Id, paciente);

        // Verificar: queda cancelado, se anota en el historial y se le avisa.
        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("cancelado", turno.Estado);
        Assert.Contains(AccionesMovimiento.TurnoCancelado, _movimientos.Acciones);
        Assert.Single(_notificaciones.Notificaciones);
    }

    /// <summary>Carga un turno de 10:00 a 10:30 con el doctor de las pruebas.</summary>
    private Turno AgregarTurno(int idPaciente, string estado, DateTime fecha)
    {
        var turno = new Turno
        {
            Id = _turnos.Turnos.Count + 1,
            IdPaciente = idPaciente,
            IdDoctor = IdDoctor,
            FechaInicio = fecha,
            HoraInicio = new TimeSpan(10, 0, 0),
            HoraFin = new TimeSpan(10, 30, 0),
            Estado = estado
        };

        _turnos.Turnos.Add(turno);
        return turno;
    }

    [Fact]
    public async Task El_listado_del_paciente_trae_solo_sus_turnos_y_cuenta_cada_estado()
    {
        // Preparar: dos turnos propios (uno pendiente y uno cancelado) y uno de otro paciente.
        _turnos.Turnos.Add(new Turno { Id = 1, IdPaciente = IdPacientePropio, IdDoctor = IdDoctor, Estado = "pendiente", FechaInicio = DateTime.Today.AddDays(1) });
        _turnos.Turnos.Add(new Turno { Id = 2, IdPaciente = IdPacientePropio, IdDoctor = IdDoctor, Estado = "cancelado", FechaInicio = DateTime.Today.AddDays(2) });
        _turnos.Turnos.Add(new Turno { Id = 3, IdPaciente = IdPacienteAjeno, IdDoctor = IdDoctor, Estado = "pendiente", FechaInicio = DateTime.Today.AddDays(1) });

        // Ejecutar: pide los pendientes y, en la URL, los del otro paciente.
        var filtro = new FiltroTurnos { PacienteId = IdPacienteAjeno, Estados = new List<string> { "pendiente" } };
        var (total, turnos, conteos, error) = await _logica.ObtenerTodos(
            filtro, "fecha", false, 1, 20, IdUsuarioPaciente, callerEsPaciente: true, callerEsDoctor: false);

        // Verificar: la tabla trae solo su turno pendiente, y los conteos
        // cuentan sus dos turnos, sin el filtro de estado.
        Assert.Null(error);
        Assert.Equal(1, total);
        Assert.Equal(1, turnos.Single().IdTurno);
        Assert.Equal(1, conteos["pendiente"]);
        Assert.Equal(1, conteos["cancelado"]);
        Assert.Equal(0, conteos["confirmado"]);
    }

    // ---- Reglas del paso 5: hora de Argentina, estados y doble reserva ----
    // El reloj de las pruebas marca hoy a las 10:00 (RelojFijo), y AgregarTurno
    // carga los turnos a las 10:00: uno de hoy "ya empezó".

    [Fact]
    public async Task El_paciente_no_reserva_un_horario_de_hoy_que_ya_paso()
    {
        // Preparar: son las 10:00 y pide las 09:00 de hoy.
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var pedido = new TurnoCreateDto(0, IdDoctor, DateTime.Today, "09:00", "09:30", null);

        // Ejecutar
        var (id, error) = await _logica.Crear(pedido, paciente);

        // Verificar
        Assert.Null(id);
        Assert.Equal("Ese horario ya pasó. Elegí uno más adelante.", error);
        Assert.Empty(_turnos.Turnos);
    }

    [Fact]
    public async Task El_personal_si_carga_un_turno_de_hoy_que_ya_paso()
    {
        var administrador = new Solicitante(1, "administrador");
        var pedido = new TurnoCreateDto(IdPacientePropio, IdDoctor, DateTime.Today, "09:00", "09:30", null);

        var (id, error) = await _logica.Crear(pedido, administrador);

        Assert.Null(error);
        Assert.NotNull(id);
    }

    [Fact]
    public async Task Si_la_base_frena_una_reserva_repetida_contesta_conflicto()
    {
        // Preparar: dos reservas del mismo horario a la vez; la base frena esta.
        _turnos.SimularDatoRepetido = true;
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var pedido = new TurnoCreateDto(0, IdDoctor, DateTime.Today.AddDays(1), "10:00", "10:30", null);

        // Ejecutar
        var (id, error) = await _logica.Crear(pedido, paciente);

        // Verificar: empieza con "Conflicto", así el endpoint responde 409.
        Assert.Null(id);
        Assert.StartsWith("Conflicto de horario", error);
    }

    [Fact]
    public async Task No_se_marca_completado_un_turno_que_todavia_no_empezo()
    {
        var turno = AgregarTurno(IdPacientePropio, "confirmado", DateTime.Today.AddDays(1));
        var administrador = new Solicitante(1, "administrador");

        var (ok, error, _) = await _logica.Actualizar(turno.Id, Estado("completado"), administrador);

        Assert.False(ok);
        Assert.StartsWith("Conflicto de estado", error);
        Assert.Equal("confirmado", turno.Estado);
    }

    [Fact]
    public async Task Un_turno_que_ya_empezo_se_marca_ausente_y_queda_en_el_historial()
    {
        // Preparar: el turno es hoy a las 10:00 y el reloj marca las 10:00.
        var turno = AgregarTurno(IdPacientePropio, "confirmado", DateTime.Today);
        var administrador = new Solicitante(1, "administrador");

        // Ejecutar: llega en mayúsculas y con espacios.
        var (ok, error, _) = await _logica.Actualizar(turno.Id, Estado(" Ausente "), administrador);

        // Verificar
        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("ausente", turno.Estado);
        Assert.Contains(AccionesMovimiento.TurnoAusente, _movimientos.Acciones);
    }

    [Fact]
    public async Task Un_estado_que_no_existe_se_rechaza()
    {
        var turno = AgregarTurno(IdPacientePropio, "pendiente", DateTime.Today.AddDays(1));
        var administrador = new Solicitante(1, "administrador");

        var (ok, _, _) = await _logica.Actualizar(turno.Id, Estado("borrado"), administrador);

        Assert.False(ok);
        Assert.Equal("pendiente", turno.Estado);
    }

    [Fact]
    public async Task No_se_reprograma_un_turno_completado()
    {
        var turno = AgregarTurno(IdPacientePropio, "completado", DateTime.Today);
        var administrador = new Solicitante(1, "administrador");
        var pedido = new TurnoUpdateDto(DateTime.Today.AddDays(3), null, null, null, null);

        var (ok, error, _) = await _logica.Actualizar(turno.Id, pedido, administrador);

        Assert.False(ok);
        Assert.StartsWith("Conflicto de estado", error);
        Assert.Equal(DateTime.Today, turno.FechaInicio);
    }

    [Fact]
    public async Task El_paciente_no_cancela_un_turno_que_ya_empezo_pero_el_personal_si()
    {
        // Preparar: el turno es hoy a las 10:00 y ya son las 10:00.
        var turno = AgregarTurno(IdPacientePropio, "confirmado", DateTime.Today);
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");
        var administrador = new Solicitante(1, "administrador");

        // Ejecutar
        var (okPaciente, errorPaciente) = await _logica.Cancelar(turno.Id, paciente);
        var estadoDespuesDelPaciente = turno.Estado;
        var (okAdministrador, _) = await _logica.Cancelar(turno.Id, administrador);

        // Verificar
        Assert.False(okPaciente);
        Assert.NotNull(errorPaciente);
        Assert.Equal("confirmado", estadoDespuesDelPaciente);
        Assert.True(okAdministrador);
        Assert.Equal("cancelado", turno.Estado);
    }

    [Fact]
    public async Task La_notificacion_lleva_la_hora_del_reloj()
    {
        var turno = AgregarTurno(IdPacientePropio, "pendiente", DateTime.Today.AddDays(1));
        var paciente = new Solicitante(IdUsuarioPaciente, "paciente");

        await _logica.Cancelar(turno.Id, paciente);

        Assert.Equal(_reloj.Momento, _notificaciones.Notificaciones.Single().Fecha);
    }

    /// <summary>Un PUT que solo cambia el estado.</summary>
    private static TurnoUpdateDto Estado(string estado) => new TurnoUpdateDto(null, null, null, estado, null);
}
