using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de la edición de la ficha del paciente: copiar datos, borrarlos y
/// las reglas del DNI.
/// </summary>
public class PacienteLogicaTests
{
    private const int IdPaciente = 1;
    private const int IdUsuarioPaciente = 10;
    private const int IdUsuarioAdministrador = 1;

    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly PacienteLogica _logica;
    private readonly Paciente _paciente;

    public PacienteLogicaTests()
    {
        _paciente = new Paciente
        {
            Id = IdPaciente,
            IdUsuario = IdUsuarioPaciente,
            Dni = "30111222",
            Alergias = "Penicilina",
            Direccion = "San Martín 100",
            Provincia = "Córdoba"
        };
        _pacientes.Pacientes.Add(_paciente);

        _logica = new PacienteLogica(_pacientes);
    }

    [Fact]
    public async Task Borrar_vacia_los_campos_pedidos()
    {
        // Preparar
        var pedido = new PacienteUpdateDto(Borrar: new List<string> { "alergias", "Direccion" });

        // Ejecutar
        var (ok, error, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioAdministrador, callerEsStaff: true);

        // Verificar: se borraron los dos (sin importar mayúsculas) y el resto quedó.
        Assert.True(ok, error);
        Assert.Null(_paciente.Alergias);
        Assert.Null(_paciente.Direccion);
        Assert.Equal("Córdoba", _paciente.Provincia);
    }

    [Fact]
    public async Task Lo_que_no_viene_no_se_toca()
    {
        // Preparar: solo viene la localidad.
        var pedido = new PacienteUpdateDto(Localidad: "Villa María");

        // Ejecutar
        var (ok, _, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioAdministrador, callerEsStaff: true);

        // Verificar
        Assert.True(ok);
        Assert.Equal("Villa María", _paciente.Localidad);
        Assert.Equal("Penicilina", _paciente.Alergias);
        Assert.Equal("30111222", _paciente.Dni);
    }

    [Fact]
    public async Task El_paciente_no_puede_cambiar_un_DNI_ya_cargado()
    {
        // Preparar
        var pedido = new PacienteUpdateDto(Dni: "99888777");

        // Ejecutar
        var (ok, error, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioPaciente, callerEsStaff: false);

        // Verificar
        Assert.False(ok);
        Assert.Contains("pedíselo a la administración", error);
        Assert.Equal("30111222", _paciente.Dni);
    }

    [Fact]
    public async Task El_personal_puede_corregir_el_DNI()
    {
        // Preparar
        var pedido = new PacienteUpdateDto(Dni: "99888777");

        // Ejecutar
        var (ok, _, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioAdministrador, callerEsStaff: true);

        // Verificar
        Assert.True(ok);
        Assert.Equal("99888777", _paciente.Dni);
    }

    [Fact]
    public async Task El_paciente_no_puede_borrar_su_DNI()
    {
        // Preparar
        var pedido = new PacienteUpdateDto(Borrar: new List<string> { "dni" });

        // Ejecutar
        var (ok, error, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioPaciente, callerEsStaff: false);

        // Verificar
        Assert.False(ok);
        Assert.Equal("El DNI solo lo puede borrar la administración.", error);
    }

    [Fact]
    public async Task Un_campo_desconocido_en_Borrar_da_error()
    {
        // Preparar
        var pedido = new PacienteUpdateDto(Borrar: new List<string> { "sueldo" });

        // Ejecutar
        var (ok, error, _, _) = await _logica.Actualizar(IdPaciente, pedido, IdUsuarioAdministrador, callerEsStaff: true);

        // Verificar
        Assert.False(ok);
        Assert.Equal("No se puede borrar el campo \"sueldo\".", error);
    }
}
