using System.Security.Claims;
using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using Microsoft.Extensions.Configuration;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de la baja y la reactivación de cuentas (DELETE /usuarios/{id} y
/// POST /usuarios/{id}/reactivar), y del chequeo de cuenta activa del token.
/// </summary>
public class BajaUsuarioTests
{
    // El administrador que hace los pedidos.
    private const int IdAdmin = 1;

    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly RelojFijo _reloj = new RelojFijo();
    private readonly UsuarioRepositoryFalso _usuarios;
    private readonly UsuarioLogica _logica;

    private readonly Usuario _paciente;
    private readonly Usuario _doctor;

    public BajaUsuarioTests()
    {
        _usuarios = new UsuarioRepositoryFalso(_pacientes);

        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres",
            ["Jwt:Issuer"] = "ChronoSaludApi",
            ["Jwt:Audience"] = "ChronoSaludClients"
        };
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        _logica = new UsuarioLogica(_usuarios, _pacientes, _turnos, _reloj, configuracion);

        // Un administrador, una paciente (con su ficha 10) y una doctora (perfil 20).
        _usuarios.Usuarios.Add(new Usuario { Id = IdAdmin, Nombre = "Admin", Rol = "administrador", Activo = true });

        _paciente = new Usuario { Id = 2, Nombre = "Ana", Rol = "paciente", Activo = true };
        _usuarios.Usuarios.Add(_paciente);
        _pacientes.Pacientes.Add(new Paciente { Id = 10, IdUsuario = 2, Usuario = _paciente });

        _doctor = new Usuario { Id = 3, Nombre = "Laura", Rol = "doctor", Activo = true };
        _doctor.Doctor = new Doctor { Id = 20, IdUsuario = 3, Activo = true, Usuario = _doctor };
        _usuarios.Usuarios.Add(_doctor);
    }

    [Fact]
    public async Task Da_de_baja_a_un_paciente_sin_turnos_por_venir()
    {
        var (ok, error) = await _logica.DarDeBaja(_paciente.Id, IdAdmin);

        Assert.True(ok);
        Assert.Null(error);
        Assert.False(_paciente.Activo);
    }

    [Fact]
    public async Task La_baja_de_un_doctor_tambien_apaga_su_perfil()
    {
        var (ok, _) = await _logica.DarDeBaja(_doctor.Id, IdAdmin);

        Assert.True(ok);
        Assert.False(_doctor.Activo);
        Assert.False(_doctor.Doctor!.Activo);
    }

    [Fact]
    public async Task Nadie_se_da_de_baja_a_si_mismo()
    {
        // Preparar: hay otro administrador, así no salta el freno del último.
        _usuarios.Usuarios.Add(new Usuario { Id = 4, Nombre = "Otro", Rol = "administrador", Activo = true });

        var (ok, error) = await _logica.DarDeBaja(IdAdmin, IdAdmin);

        Assert.False(ok);
        Assert.Equal("No podés dar de baja tu propia cuenta.", error);
        Assert.True(_usuarios.Usuarios[0].Activo);
    }

    [Fact]
    public async Task No_se_da_de_baja_al_ultimo_administrador()
    {
        // Preparar: el pedido lo hace alguien que ya no figura como
        // administrador activo, así queda uno solo.
        _usuarios.Usuarios[0].Activo = false;
        var unico = new Usuario { Id = 5, Nombre = "Único", Rol = "administrador", Activo = true };
        _usuarios.Usuarios.Add(unico);

        var (ok, error) = await _logica.DarDeBaja(unico.Id, IdAdmin);

        Assert.False(ok);
        Assert.Contains("único administrador activo", error);
        Assert.True(unico.Activo);
    }

    [Fact]
    public async Task Un_turno_confirmado_de_manana_frena_la_baja()
    {
        // Preparar: la doctora tiene un turno confirmado mañana.
        _turnos.Turnos.Add(new Turno { Id = 1, IdPaciente = 10, IdDoctor = 20, Estado = "confirmado", FechaInicio = DateTime.Today.AddDays(1) });

        var (ok, error) = await _logica.DarDeBaja(_doctor.Id, IdAdmin);

        Assert.False(ok);
        Assert.Contains("1 turno pendiente o confirmado", error);
        Assert.True(_doctor.Activo);
        Assert.True(_doctor.Doctor!.Activo);
    }

    [Fact]
    public async Task Los_turnos_cancelados_o_pasados_no_frenan()
    {
        // Preparar: uno cancelado para mañana y uno pendiente de ayer.
        _turnos.Turnos.Add(new Turno { Id = 1, IdPaciente = 10, IdDoctor = 20, Estado = "cancelado", FechaInicio = DateTime.Today.AddDays(1) });
        _turnos.Turnos.Add(new Turno { Id = 2, IdPaciente = 10, IdDoctor = 20, Estado = "pendiente", FechaInicio = DateTime.Today.AddDays(-1) });

        var (ok, _) = await _logica.DarDeBaja(_paciente.Id, IdAdmin);

        Assert.True(ok);
    }

    [Fact]
    public async Task Una_cuenta_ya_dada_de_baja_no_se_vuelve_a_dar_de_baja()
    {
        _paciente.Activo = false;

        var (ok, error) = await _logica.DarDeBaja(_paciente.Id, IdAdmin);

        Assert.False(ok);
        Assert.Equal("La cuenta ya está dada de baja.", error);
    }

    [Fact]
    public async Task Reactivar_vuelve_a_encender_la_cuenta_y_el_doctor()
    {
        _doctor.Activo = false;
        _doctor.Doctor!.Activo = false;

        var (ok, _) = await _logica.Reactivar(_doctor.Id);

        Assert.True(ok);
        Assert.True(_doctor.Activo);
        Assert.True(_doctor.Doctor.Activo);
    }

    [Fact]
    public async Task Reactivar_una_cuenta_activa_da_error()
    {
        var (ok, error) = await _logica.Reactivar(_paciente.Id);

        Assert.False(ok);
        Assert.Equal("La cuenta ya está activa.", error);
    }

    [Fact]
    public async Task El_usuario_trae_si_esta_activo()
    {
        _paciente.Activo = false;

        var usuario = await _logica.ObtenerPorId(_paciente.Id);

        Assert.NotNull(usuario);
        Assert.False(usuario.Activo);
    }

    // [Theory] corre la prueba una vez por cada [InlineData]: el id del token
    // (null es un token sin id) y lo que se espera.
    [Theory]
    [InlineData("2", true)]
    [InlineData("9", false)]
    [InlineData(null, false)]
    public async Task El_token_solo_vale_si_la_cuenta_sigue_activa(string? idDelToken, bool esperado)
    {
        var claims = new List<Claim>();
        if (idDelToken != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, idDelToken));
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "prueba"));

        var activa = await CuentaActiva.EstaActivaAsync(usuario, _usuarios);

        Assert.Equal(esperado, activa);
    }

    [Fact]
    public async Task El_token_de_una_cuenta_dada_de_baja_deja_de_valer()
    {
        await _logica.DarDeBaja(_paciente.Id, IdAdmin);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "2") }, "prueba"));

        Assert.False(await CuentaActiva.EstaActivaAsync(usuario, _usuarios));
    }
}
