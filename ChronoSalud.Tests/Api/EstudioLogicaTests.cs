using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de pedir un estudio, cargar su resultado y quién los ve (paso 16).
/// </summary>
public class EstudioLogicaTests
{
    // La doctora (usuario 3, perfil 20) y la paciente (usuario 7, perfil 10).
    private const int IdUsuarioDoctora = 3;
    private const int IdDoctora = 20;
    private const int IdUsuarioPaciente = 7;
    private const int IdPaciente = 10;

    private const int TurnoPropio = 100;
    private const int TurnoDeOtroDoctor = 101;

    private readonly EstudioRepositoryFalso _estudios = new EstudioRepositoryFalso();
    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly NotificacionRepositoryFalso _notificaciones = new NotificacionRepositoryFalso();
    private readonly RelojFijo _reloj = new RelojFijo { Momento = new DateTime(2026, 10, 9, 11, 30, 0) };
    private readonly EstudioLogica _logica;

    public EstudioLogicaTests()
    {
        _logica = new EstudioLogica(_estudios, _pacientes, _doctores, _turnos, _notificaciones, _reloj,
            NullLogger<EstudioLogica>.Instance);

        var doctora = new Doctor
        {
            Id = IdDoctora,
            IdUsuario = IdUsuarioDoctora,
            Especialidad = "Clínica médica",
            Matricula = "MN 1234",
            Usuario = new Usuario { Id = IdUsuarioDoctora, Nombre = "Laura", Apellido = "Méndez", Rol = "doctor" }
        };
        _doctores.Doctores.Add(doctora);

        _pacientes.Pacientes.Add(new Paciente
        {
            Id = IdPaciente,
            IdUsuario = IdUsuarioPaciente,
            Usuario = new Usuario { Id = IdUsuarioPaciente, Nombre = "Ana", Apellido = "Duarte", Rol = "paciente" }
        });

        _turnos.Turnos.Add(new Turno { Id = TurnoPropio, IdPaciente = IdPaciente, IdDoctor = IdDoctora, Doctor = doctora });
        _turnos.Turnos.Add(new Turno { Id = TurnoDeOtroDoctor, IdPaciente = IdPaciente, IdDoctor = 21 });
    }

    private static EstudioCreateDto Pedido(int idPaciente = IdPaciente, int? idTurno = TurnoPropio) =>
        new EstudioCreateDto(idPaciente, idTurno, "sangre", "  Hemograma completo  ", default);

    [Fact]
    public async Task El_doctor_pide_un_estudio_desde_su_turno()
    {
        var (id, error) = await _logica.Crear(Pedido(), IdUsuarioDoctora);

        Assert.Null(error);
        var estudio = _estudios.Estudios.Single(e => e.Id == id);
        Assert.Equal("Hemograma completo", estudio.Descripcion);
        Assert.Equal("pendiente", estudio.Estado);
        // Sin fecha en el pedido, va la de hoy.
        Assert.Equal(new DateTime(2026, 10, 9), estudio.FechaSolicitud);
    }

    [Fact]
    public async Task Un_turno_de_otro_doctor_se_rechaza_como_no_encontrado()
    {
        var (id, error) = await _logica.Crear(Pedido(idTurno: TurnoDeOtroDoctor), IdUsuarioDoctora);

        Assert.Null(id);
        Assert.Equal(TurnoVinculado.NoEncontrado, error);
        Assert.Empty(_estudios.Estudios);
    }

    [Fact]
    public async Task Un_paciente_que_no_existe_da_no_encontrado()
    {
        var (id, error) = await _logica.Crear(Pedido(idPaciente: 999), IdUsuarioDoctora);

        Assert.Null(id);
        Assert.Contains("no encontrado", error);
    }

    [Fact]
    public async Task Sin_perfil_de_doctor_no_se_pide()
    {
        var (id, error) = await _logica.Crear(Pedido(), idUsuarioCaller: 55);

        Assert.Null(id);
        Assert.Contains("no encontrado", error);
    }

    [Fact]
    public async Task Cargar_el_resultado_avisa_al_paciente_nombrando_el_estudio()
    {
        var (id, _) = await _logica.Crear(Pedido(), IdUsuarioDoctora);

        var (ok, error) = await _logica.CargarResultado(id!.Value,
            new EstudioResultadoDto("Valores normales.", null, "entregado", default));

        Assert.True(ok, error);
        var estudio = _estudios.Estudios.Single();
        Assert.Equal("entregado", estudio.Estado);
        Assert.Equal(_reloj.Momento, estudio.FechaResultado);

        var aviso = _notificaciones.Notificaciones.Single();
        Assert.Equal(IdUsuarioPaciente, aviso.IdUsuario);
        Assert.Equal("estudio", aviso.Tipo);
        Assert.Equal("El resultado de tu estudio «Hemograma completo» ya está disponible.", aviso.Mensaje);
        Assert.Equal(_reloj.Momento, aviso.Fecha);
    }

    [Fact]
    public async Task El_paciente_ve_los_suyos_con_la_descripcion_y_quien_lo_pidio()
    {
        var (id, _) = await _logica.Crear(Pedido(), IdUsuarioDoctora);
        _estudios.Estudios.Single().Turno = _turnos.Turnos.Single(t => t.Id == TurnoPropio);

        var (estudios, error, prohibido) = await _logica.ObtenerDePaciente(IdPaciente, null, null, null, IdUsuarioPaciente, callerEsStaff: false);

        Assert.Null(error);
        Assert.False(prohibido);
        var estudio = estudios.Single();
        Assert.Equal(id, estudio.IdEstudio);
        Assert.Equal("Hemograma completo", estudio.Descripcion);
        Assert.Equal(IdDoctora, estudio.Doctor!.IdDoctor);
        Assert.Equal("Méndez", estudio.Doctor.Apellido);
    }

    [Fact]
    public async Task Un_paciente_no_ve_los_estudios_de_otro()
    {
        await _logica.Crear(Pedido(), IdUsuarioDoctora);
        _pacientes.Pacientes.Add(new Paciente { Id = 11, IdUsuario = 8 });

        var (_, _, prohibido) = await _logica.ObtenerDePaciente(IdPaciente, null, null, null, idUsuarioCaller: 8, callerEsStaff: false);
        var (estudio, _, prohibidoUno) = await _logica.ObtenerPorId(1, idUsuarioCaller: 8, callerEsStaff: false);

        Assert.True(prohibido);
        Assert.True(prohibidoUno);
        Assert.Null(estudio);
    }

    [Theory]
    [InlineData("Hemograma completo", "El resultado de tu estudio «Hemograma completo» ya está disponible.")]
    [InlineData("  ", "El resultado de tu estudio de sangre ya está disponible.")]
    public void El_mensaje_nombra_el_estudio_o_su_tipo(string descripcion, string mensaje)
    {
        var estudio = new Estudio { Tipo = "sangre", Descripcion = descripcion };

        Assert.Equal(mensaje, EstudioLogica.MensajeDeResultado(estudio));
    }
}
