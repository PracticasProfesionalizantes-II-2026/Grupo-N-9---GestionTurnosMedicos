using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using Microsoft.Extensions.Configuration;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del registro de cuentas, sobre todo del alta de paciente con su
/// ficha que hace la administración.
/// </summary>
public class UsuarioLogicaTests
{
    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly UsuarioRepositoryFalso _usuarios;
    private readonly UsuarioLogica _logica;

    public UsuarioLogicaTests()
    {
        _usuarios = new UsuarioRepositoryFalso(_pacientes);

        // La lógica firma un token al registrar: necesita la configuración de
        // JWT. Acá se arma en memoria, con una clave de prueba.
        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres",
            ["Jwt:Issuer"] = "ChronoSaludApi",
            ["Jwt:Audience"] = "ChronoSaludClients"
        };
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        _logica = new UsuarioLogica(_usuarios, _pacientes, new TurnoRepositoryFalso(), new RelojFijo(), configuracion);
    }

    [Fact]
    public async Task El_administrador_registra_un_paciente_con_toda_su_ficha()
    {
        // Preparar
        var ficha = new PacienteUpdateDto(
            Dni: "30111222",
            TipoDocumento: "DNI",
            Sexo: "femenino",
            Provincia: "Córdoba",
            Localidad: "Villa María",
            ContactoEmergenciaNombre: "Marta Pérez",
            ContactoEmergenciaTelefono: "353-4000000");
        var pedido = PedidoDePaciente("lucia@demo.com", ficha);

        // Ejecutar
        var (resultado, error, sinPermiso) = await _logica.Registrar(pedido, esAdministrador: true);

        // Verificar: se creó la cuenta y el paciente con todos los datos.
        Assert.Null(error);
        Assert.False(sinPermiso);
        Assert.NotNull(resultado!.IdPaciente);

        var paciente = _pacientes.Pacientes.Single();
        Assert.Equal(resultado.IdPaciente, paciente.Id);
        Assert.Equal("30111222", paciente.Dni);
        Assert.Equal("DNI", paciente.TipoDocumento);
        Assert.Equal("Córdoba", paciente.Provincia);
        Assert.Equal("Villa María", paciente.Localidad);
        Assert.Equal("Marta Pérez", paciente.ContactoEmergenciaNombre);
        Assert.Equal("353-4000000", paciente.ContactoEmergenciaTelefono);
    }

    [Fact]
    public async Task Un_DNI_repetido_no_crea_la_cuenta()
    {
        // Preparar: ya hay un paciente con ese DNI.
        _pacientes.Pacientes.Add(new Paciente { Id = 1, IdUsuario = 99, Dni = "30111222" });
        var pedido = PedidoDePaciente("otra@demo.com", new PacienteUpdateDto(Dni: "30111222"));

        // Ejecutar
        var (resultado, error, _) = await _logica.Registrar(pedido, esAdministrador: true);

        // Verificar: avisa el DNI y no se creó ninguna cuenta.
        Assert.Null(resultado);
        Assert.Equal("Ya existe un paciente registrado con ese DNI.", error);
        Assert.Empty(_usuarios.Usuarios);
    }

    [Fact]
    public async Task En_el_registro_publico_la_ficha_se_ignora()
    {
        // Preparar: alguien sin sesión manda una ficha con DNI.
        var pedido = PedidoDePaciente("publico@demo.com", new PacienteUpdateDto(Dni: "30111222"));

        // Ejecutar
        var (resultado, error, _) = await _logica.Registrar(pedido, esAdministrador: false);

        // Verificar: la cuenta se crea, pero sin los datos de la ficha.
        Assert.Null(error);
        Assert.NotNull(resultado);
        Assert.Null(_pacientes.Pacientes.Single().Dni);
    }

    [Fact]
    public async Task Si_la_base_rechaza_un_dato_repetido_contesta_con_un_mensaje()
    {
        // Preparar: dos altas iguales se cruzaron y la base frena la segunda.
        _usuarios.SimularDatoRepetido = true;
        var pedido = PedidoDePaciente("cruce@demo.com", null);

        // Ejecutar
        var (resultado, error, _) = await _logica.Registrar(pedido, esAdministrador: true);

        // Verificar: un mensaje claro en vez de un error 500.
        Assert.Null(resultado);
        Assert.Equal("Ya existe una cuenta con ese email o un paciente con ese DNI.", error);
    }

    private static UsuarioRegistroDto PedidoDePaciente(string email, PacienteUpdateDto? ficha)
    {
        return new UsuarioRegistroDto("Lucía", "Gómez", email, "Clave2026!", null, "paciente", ficha);
    }
}
