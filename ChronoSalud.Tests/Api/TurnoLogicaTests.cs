using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
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
            _movimientos);
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
}
