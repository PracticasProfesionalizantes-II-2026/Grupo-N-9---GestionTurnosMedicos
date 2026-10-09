using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del turno al que se vincula una receta o una consulta: tiene que
/// ser de ese paciente y de ese doctor (paso 11).
/// </summary>
public class TurnoVinculadoTests
{
    // La doctora que atiende (usuario 3, perfil 20) y la paciente 10.
    private const int IdUsuarioDoctora = 3;
    private const int IdDoctora = 20;
    private const int IdPaciente = 10;

    // Turnos: 100 es de la paciente con la doctora, 101 de otro paciente con
    // la doctora y 102 de la paciente con otro doctor.
    private const int TurnoPropio = 100;
    private const int TurnoDeOtroPaciente = 101;
    private const int TurnoDeOtroDoctor = 102;

    private readonly RecetaRepositoryFalso _recetas = new RecetaRepositoryFalso();
    private readonly HistorialClinicoRepositoryFalso _entradas = new HistorialClinicoRepositoryFalso();
    private readonly MedicamentoRepositoryFalso _medicamentos = new MedicamentoRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly RecetaLogica _recetaLogica;
    private readonly HistorialClinicoLogica _historialLogica;

    public TurnoVinculadoTests()
    {
        var pacientes = new PacienteRepositoryFalso();
        _recetaLogica = new RecetaLogica(_recetas, _medicamentos, pacientes, _doctores, _turnos);
        _historialLogica = new HistorialClinicoLogica(_entradas, _doctores, pacientes, _turnos);

        var cuenta = new Usuario { Id = IdUsuarioDoctora, Nombre = "Laura", Apellido = "Méndez", Rol = "doctor" };
        _doctores.Doctores.Add(new Doctor { Id = IdDoctora, IdUsuario = IdUsuarioDoctora, Usuario = cuenta });
        _medicamentos.Medicamentos.Add(new Medicamento { Id = 1, Nombre = "IBUPIRAC" });

        _turnos.Turnos.Add(new Turno { Id = TurnoPropio, IdPaciente = IdPaciente, IdDoctor = IdDoctora });
        _turnos.Turnos.Add(new Turno { Id = TurnoDeOtroPaciente, IdPaciente = 11, IdDoctor = IdDoctora });
        _turnos.Turnos.Add(new Turno { Id = TurnoDeOtroDoctor, IdPaciente = IdPaciente, IdDoctor = 21 });
    }

    private static RecetaCreateDto Receta(int? idTurno) => new RecetaCreateDto(
        IdPaciente,
        0,
        idTurno,
        new DateTime(2026, 10, 9),
        new DateTime(2026, 11, 8),
        null,
        new List<RecetaMedicamentoDto> { new RecetaMedicamentoDto(1, "1 comprimido", "cada 8 horas", null, null) });

    private static HistorialClinicoCreateDto Consulta(int? idTurno) =>
        new HistorialClinicoCreateDto(new DateTime(2026, 10, 9), "Control.", "Sano", idTurno);

    [Fact]
    public async Task La_receta_se_vincula_al_turno_del_paciente_con_la_doctora()
    {
        var (id, error, _) = await _recetaLogica.Crear(Receta(TurnoPropio), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.Null(error);
        Assert.Equal(TurnoPropio, _recetas.Recetas.Single(r => r.Id == id).IdTurno);

        // Y la receta lo informa, para poder editarla sin perder el vínculo.
        var (receta, _, _) = await _recetaLogica.ObtenerPorId(id!.Value, IdUsuarioDoctora, callerEsStaff: true);
        Assert.Equal(TurnoPropio, receta!.IdTurno);
    }

    [Theory]
    [InlineData(TurnoDeOtroPaciente)]
    [InlineData(TurnoDeOtroDoctor)]
    [InlineData(999)]
    public async Task La_receta_no_se_vincula_a_un_turno_ajeno_ni_inexistente(int idTurno)
    {
        var (id, error, _) = await _recetaLogica.Crear(Receta(idTurno), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.Null(id);
        Assert.Equal(TurnoVinculado.NoEncontrado, error);
        Assert.Empty(_recetas.Recetas);
    }

    [Fact]
    public async Task La_receta_sin_turno_se_emite_como_siempre()
    {
        var (id, error, _) = await _recetaLogica.Crear(Receta(null), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.Null(error);
        Assert.NotNull(id);
    }

    [Fact]
    public async Task Al_modificar_la_receta_tampoco_acepta_un_turno_ajeno()
    {
        var (id, _, _) = await _recetaLogica.Crear(Receta(TurnoPropio), IdUsuarioDoctora, callerEsDoctor: true);

        var (ok, error, _) = await _recetaLogica.Actualizar(
            id!.Value, Receta(TurnoDeOtroPaciente), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.False(ok);
        Assert.Equal(TurnoVinculado.NoEncontrado, error);
        Assert.Equal(TurnoPropio, _recetas.Recetas.Single().IdTurno);
    }

    [Fact]
    public async Task La_consulta_se_vincula_al_turno_del_paciente_con_la_doctora()
    {
        var (ok, error) = await _historialLogica.Crear(IdPaciente, IdUsuarioDoctora, Consulta(TurnoPropio));

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(TurnoPropio, _entradas.Entradas.Single().IdTurno);
    }

    [Theory]
    [InlineData(TurnoDeOtroPaciente)]
    [InlineData(TurnoDeOtroDoctor)]
    [InlineData(999)]
    public async Task La_consulta_no_se_vincula_a_un_turno_ajeno_ni_inexistente(int idTurno)
    {
        var (ok, error) = await _historialLogica.Crear(IdPaciente, IdUsuarioDoctora, Consulta(idTurno));

        Assert.False(ok);
        Assert.Equal(TurnoVinculado.NoEncontrado, error);
        Assert.Empty(_entradas.Entradas);
    }

    [Fact]
    public async Task Al_modificar_la_consulta_tampoco_acepta_un_turno_ajeno()
    {
        await _historialLogica.Crear(IdPaciente, IdUsuarioDoctora, Consulta(TurnoPropio));
        var idEntrada = _entradas.Entradas.Single().Id;

        var (ok, error, _) = await _historialLogica.Actualizar(
            IdPaciente, idEntrada, Consulta(TurnoDeOtroDoctor), IdUsuarioDoctora);

        Assert.False(ok);
        Assert.Equal(TurnoVinculado.NoEncontrado, error);
        Assert.Equal(TurnoPropio, _entradas.Entradas.Single().IdTurno);
    }
}
