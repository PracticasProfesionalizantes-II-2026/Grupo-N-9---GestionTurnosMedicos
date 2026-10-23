using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del correo real y de los recordatorios (paso 21): qué transporte se
/// elige, el mensaje que sale por SMTP, la plantilla HTML, la cola con sus
/// reintentos, los textos de los avisos y el recordatorio del día.
/// </summary>
public class CorreoYRecordatoriosTests
{
    // ---- Qué transporte se usa --------------------------------------------------

    [Fact]
    public void Con_servidor_smtp_los_correos_salen_por_smtp_aun_en_desarrollo()
    {
        var opciones = new OpcionesDeCorreo { Smtp = { Host = "smtp-relay.brevo.com" } };

        Assert.Equal(TipoDeTransporte.Smtp, EleccionDeTransporte.Para(opciones, esDesarrollo: true));
        Assert.Equal(TipoDeTransporte.Smtp, EleccionDeTransporte.Para(opciones, esDesarrollo: false));
    }

    [Fact]
    public void Sin_servidor_en_desarrollo_se_escriben_en_el_log()
    {
        Assert.Equal(TipoDeTransporte.Consola, EleccionDeTransporte.Para(new OpcionesDeCorreo(), esDesarrollo: true));
    }

    [Fact]
    public void Sin_servidor_fuera_de_desarrollo_no_salen_ni_se_escriben()
    {
        Assert.Equal(TipoDeTransporte.SinConfigurar, EleccionDeTransporte.Para(new OpcionesDeCorreo(), esDesarrollo: false));
    }

    [Fact]
    public void Fuera_de_desarrollo_se_pueden_ver_en_el_log_si_se_pide()
    {
        var opciones = new OpcionesDeCorreo { MostrarEnLog = true };

        Assert.Equal(TipoDeTransporte.Consola, EleccionDeTransporte.Para(opciones, esDesarrollo: false));
    }

    [Theory]
    [InlineData("ana.duarte@gmail.com", "a***@gmail.com")]
    [InlineData("x@dominio.com", "x***@dominio.com")]
    [InlineData("sin-arroba", "***")]
    [InlineData("@dominio.com", "***")]
    public void El_email_se_enmascara_para_el_log(string email, string esperado)
    {
        Assert.Equal(esperado, EleccionDeTransporte.Enmascarar(email));
    }

    [Theory]
    [InlineData("StartTls", SecureSocketOptions.StartTls)]
    [InlineData("sslonconnect", SecureSocketOptions.SslOnConnect)]
    [InlineData("Auto", SecureSocketOptions.Auto)]
    [InlineData("None", SecureSocketOptions.None)]
    [InlineData("cualquier cosa", SecureSocketOptions.StartTls)]
    public void La_seguridad_del_smtp_se_lee_de_la_configuracion(string valor, SecureSocketOptions esperada)
    {
        Assert.Equal(esperada, new OpcionesDeSmtp { Seguridad = valor }.OpcionDeSeguridad);
    }

    // ---- El mensaje que sale por SMTP -------------------------------------------

    [Fact]
    public void El_mensaje_lleva_remitente_destinatario_asunto_texto_y_html()
    {
        var opciones = new OpcionesDeCorreo { Remitente = "turnos@chronosalud.com", NombreRemitente = "ChronoSalud" };
        var correo = new CorreoSaliente("ana@correo.com", "Hoy tenés turno", "Hola, Ana:\n\nA las 10:00.");

        var mensaje = CorreoPorSmtp.Armar(correo, opciones);

        var remitente = Assert.IsType<MailboxAddress>(Assert.Single(mensaje.From));
        Assert.Equal("turnos@chronosalud.com", remitente.Address);
        Assert.Equal("ChronoSalud", remitente.Name);
        Assert.Equal("ana@correo.com", Assert.IsType<MailboxAddress>(Assert.Single(mensaje.To)).Address);
        Assert.Equal("Hoy tenés turno", mensaje.Subject);
        Assert.Equal("Hola, Ana:\n\nA las 10:00.", mensaje.TextBody);
        Assert.Contains("A las 10:00.", mensaje.HtmlBody);
    }

    [Fact]
    public void Sin_remitente_se_usa_el_usuario_del_smtp()
    {
        var opciones = new OpcionesDeCorreo { Smtp = { Usuario = "cuenta@gmail.com" } };

        var mensaje = CorreoPorSmtp.Armar(new CorreoSaliente("a@b.com", "Asunto", "Texto"), opciones);

        Assert.Equal("cuenta@gmail.com", Assert.IsType<MailboxAddress>(Assert.Single(mensaje.From)).Address);
    }

    [Fact]
    public void Sin_remitente_ni_usuario_no_se_puede_armar_el_mensaje()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CorreoPorSmtp.Armar(new CorreoSaliente("a@b.com", "Asunto", "Texto"), new OpcionesDeCorreo()));
    }

    // ---- Plantilla HTML ------------------------------------------------------------

    [Fact]
    public void La_plantilla_escapa_el_texto()
    {
        var html = PlantillaDeCorreo.Html("Aviso <b>", "Hola, <script>alert(1)</script> & chau");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp; chau", html);
        Assert.Contains("Aviso &lt;b&gt;", html);
    }

    [Fact]
    public void La_plantilla_vuelve_enlaces_las_direcciones_y_separa_los_parrafos()
    {
        var html = PlantillaDeCorreo.Html("Asunto", "Primero.\n\nEntrá a https://chronosalud.com/Turnos?a=1&b=2\n\nÚltimo.");

        Assert.Contains("<a href=\"https://chronosalud.com/Turnos?a=1&amp;b=2\"", html);
        Assert.Equal(3, html.Split("<p style=").Length - 1 - 1); // tres párrafos más el pie
        Assert.Contains(">Primero.</p>", html);
        Assert.Contains(">Último.</p>", html);
    }

    // ---- Cola y reintentos ---------------------------------------------------------

    private class TransporteFalso : ITransporteDeCorreo
    {
        public int FallasPendientes { get; set; }
        public List<CorreoSaliente> Mandados { get; } = new();
        public int Intentos { get; private set; }

        public Task Mandar(CorreoSaliente correo, CancellationToken cancelacion)
        {
            Intentos++;
            if (FallasPendientes > 0)
            {
                FallasPendientes--;
                throw new InvalidOperationException("El servidor no contesta.");
            }

            Mandados.Add(correo);
            return Task.CompletedTask;
        }
    }

    private static async Task Procesar(ColaDeCorreos cola, TransporteFalso transporte)
    {
        var envio = new EnvioDeCorreos(cola, transporte, NullLogger<EnvioDeCorreos>.Instance,
            [TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)]);

        using var cancelacion = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await envio.StartAsync(cancelacion.Token);
        // Espera a que la cola quede vacía y a que termine el último intento.
        while (cola.Pendientes.Count > 0 && !cancelacion.IsCancellationRequested)
            await Task.Delay(10);
        await Task.Delay(100);
        await envio.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task La_cola_devuelve_enseguida_y_el_correo_sale_en_segundo_plano()
    {
        var cola = new ColaDeCorreos();
        var transporte = new TransporteFalso();

        await cola.Enviar("ana@correo.com", "Asunto", "Texto");
        Assert.Empty(transporte.Mandados);

        await Procesar(cola, transporte);

        Assert.Equal("ana@correo.com", Assert.Single(transporte.Mandados).Para);
    }

    [Fact]
    public async Task Sin_email_no_se_encola_nada()
    {
        var cola = new ColaDeCorreos();

        await cola.Enviar("  ", "Asunto", "Texto");

        Assert.Equal(0, cola.Pendientes.Count);
    }

    [Fact]
    public async Task Si_el_servidor_falla_se_reintenta()
    {
        var cola = new ColaDeCorreos();
        var transporte = new TransporteFalso { FallasPendientes = 2 };

        await cola.Enviar("ana@correo.com", "Asunto", "Texto");
        await Procesar(cola, transporte);

        Assert.Equal(3, transporte.Intentos);
        Assert.Single(transporte.Mandados);
    }

    [Fact]
    public async Task Despues_de_tres_intentos_se_deja_y_sigue_con_el_siguiente()
    {
        var cola = new ColaDeCorreos();
        var transporte = new TransporteFalso { FallasPendientes = 3 };

        await cola.Enviar("primero@correo.com", "Asunto", "Texto");
        await cola.Enviar("segundo@correo.com", "Asunto", "Texto");
        await Procesar(cola, transporte);

        Assert.Equal("segundo@correo.com", Assert.Single(transporte.Mandados).Para);
    }

    // ---- Textos de los avisos ------------------------------------------------------

    private static readonly DateTime Hoy = new(2026, 10, 12);

    [Fact]
    public void El_dia_se_dice_con_palabras_y_el_anio_solo_si_es_otro()
    {
        Assert.Equal("martes 13 de octubre", TextosDeTurno.Dia(new DateTime(2026, 10, 13), Hoy));
        Assert.Equal("viernes 15 de enero de 2027", TextosDeTurno.Dia(new DateTime(2027, 1, 15), Hoy));
    }

    [Theory]
    [InlineData("confirmado", "Se confirmó tu turno del martes 13 de octubre a las 09:30.")]
    [InlineData("completado", "Tu turno del martes 13 de octubre a las 09:30 quedó como atendido.")]
    [InlineData("ausente", "Tu turno del martes 13 de octubre a las 09:30 quedó registrado como ausente.")]
    [InlineData("cancelado", "Se canceló tu turno del martes 13 de octubre a las 09:30.")]
    [InlineData("otro", "Tu turno del martes 13 de octubre a las 09:30 ahora está «otro».")]
    public void Cada_estado_tiene_su_texto(string estado, string esperado)
    {
        Assert.Equal(esperado, TextosDeTurno.EstadoCambiado(new DateTime(2026, 10, 13), new TimeSpan(9, 30, 0), estado, Hoy));
    }

    [Fact]
    public void La_reprogramacion_dice_de_donde_a_donde()
    {
        var texto = TextosDeTurno.Reprogramado(new DateTime(2026, 10, 13), new TimeSpan(9, 30, 0),
            new DateTime(2026, 10, 14), new TimeSpan(11, 0, 0), Hoy);

        Assert.Equal("Tu turno del martes 13 de octubre a las 09:30 se pasó al miércoles 14 de octubre a las 11:00.", texto);
    }

    // ---- Avisos por correo ---------------------------------------------------------

    private static IConfiguration Configuracion() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Recuperacion:UrlWeb"] = "https://chronosalud.com/" })
        .Build();

    [Fact]
    public async Task El_aviso_saluda_repite_el_mensaje_y_lleva_el_enlace()
    {
        var correo = new EnviadorDeCorreoFalso();
        var avisos = new AvisosPorCorreo(correo, Configuracion());
        var usuario = new Usuario { Nombre = "Ana", Email = "ana@correo.com", Activo = true };

        await avisos.Avisar(usuario, "Se canceló tu turno", "Se canceló tu turno del martes.", "/Turnos");

        var enviado = Assert.Single(correo.Enviados);
        Assert.Equal("ana@correo.com", enviado.Para);
        Assert.Equal("Se canceló tu turno", enviado.Asunto);
        Assert.StartsWith("Hola, Ana:", enviado.Texto);
        Assert.Contains("Se canceló tu turno del martes.", enviado.Texto);
        Assert.Contains("https://chronosalud.com/Turnos", enviado.Texto);
    }

    [Fact]
    public async Task A_una_cuenta_dada_de_baja_o_sin_email_no_le_llega_nada()
    {
        var correo = new EnviadorDeCorreoFalso();
        var avisos = new AvisosPorCorreo(correo, Configuracion());

        await avisos.Avisar(new Usuario { Email = "baja@correo.com", Activo = false }, "A", "B", "/Turnos");
        await avisos.Avisar(new Usuario { Email = "", Activo = true }, "A", "B", "/Turnos");

        Assert.Empty(correo.Enviados);
    }

    [Fact]
    public async Task Al_reservar_el_paciente_recibe_el_aviso_tambien_por_correo()
    {
        var correo = new EnviadorDeCorreoFalso();
        var pacientes = new PacienteRepositoryFalso();
        pacientes.Pacientes.Add(new Paciente
        {
            Id = 1, IdUsuario = 10,
            Usuario = new Usuario { Id = 10, Nombre = "Ana", Email = "ana@correo.com", Activo = true }
        });
        var doctores = new DoctorRepositoryFalso();
        doctores.Doctores.Add(new Doctor { Id = 5, IdUsuario = 20, Activo = true });
        var notificaciones = new NotificacionRepositoryFalso();
        var reloj = new RelojFijo { Momento = new DateTime(2026, 10, 12, 10, 0, 0) };

        var logica = new TurnoLogica(new TurnoRepositoryFalso(), pacientes, doctores, notificaciones,
            NullLogger<TurnoLogica>.Instance, new HorarioLaboralLogicaFalsa(), new RegistroMovimientosFalso(), reloj,
            new AvisosPorCorreo(correo, Configuracion()));

        var (_, error) = await logica.Crear(
            new TurnoCreateDto(1, 5, new DateTime(2026, 10, 13), "09:30", "10:00", null), new Solicitante(10, "paciente"));

        Assert.Null(error);
        var aviso = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal("Se creó tu turno para el martes 13 de octubre a las 09:30.", aviso.Mensaje);
        var enviado = Assert.Single(correo.Enviados);
        Assert.Equal("Se creó tu turno", enviado.Asunto);
        Assert.Contains(aviso.Mensaje, enviado.Texto);
    }

    // ---- Recordatorio del día ------------------------------------------------------

    private readonly TurnoRepositoryFalso _turnos = new();
    private readonly NotificacionRepositoryFalso _notificaciones = new();
    private readonly EnviadorDeCorreoFalso _correo = new();
    private readonly RelojFijo _reloj = new() { Momento = new DateTime(2026, 10, 12, 7, 0, 0) };

    private RecordatorioDeTurnos Recordatorio() => new(_turnos, _notificaciones,
        new AvisosPorCorreo(_correo, Configuracion()), _reloj, NullLogger<RecordatorioDeTurnos>.Instance);

    private static readonly Doctor Laura = new()
    {
        Id = 5, Especialidad = "Clínica", Consultorio = "Consultorio 101",
        Usuario = new Usuario { Nombre = "Laura", Apellido = "Gomez" }
    };

    private Turno AgregarTurno(int id, DateTime dia, int hora, string estado = "confirmado", bool activo = true, int idUsuario = 0)
    {
        var usuario = new Usuario
        {
            Id = idUsuario == 0 ? 100 + id : idUsuario,
            Nombre = "Ana", Email = $"paciente{id}@correo.com", Activo = activo
        };
        var turno = new Turno
        {
            Id = id, IdDoctor = 5, FechaInicio = dia, HoraInicio = TimeSpan.FromHours(hora),
            HoraFin = TimeSpan.FromHours(hora).Add(TimeSpan.FromMinutes(30)), Estado = estado,
            Paciente = new Paciente { Id = id, Usuario = usuario }, Doctor = Laura
        };
        _turnos.Turnos.Add(turno);
        return turno;
    }

    [Fact]
    public async Task Se_recuerdan_los_turnos_de_hoy_pendientes_o_confirmados()
    {
        AgregarTurno(1, Hoy, 10, "confirmado");
        AgregarTurno(2, Hoy, 11, "pendiente");

        var enviados = await Recordatorio().EnviarLosDeHoy();

        Assert.Equal(2, enviados);
        Assert.Equal(2, _correo.Enviados.Count);
        Assert.All(_notificaciones.Notificaciones, n => Assert.Equal(RecordatorioDeTurnos.Tipo, n.Tipo));
    }

    [Fact]
    public async Task No_se_recuerdan_los_cancelados_los_de_otro_dia_ni_los_que_ya_empezaron()
    {
        _reloj.Momento = new DateTime(2026, 10, 12, 10, 15, 0);
        AgregarTurno(1, Hoy, 11, "cancelado");
        AgregarTurno(2, Hoy, 11, "completado");
        AgregarTurno(3, Hoy.AddDays(1), 11);
        AgregarTurno(4, Hoy, 10);          // empezó a las 10:00
        AgregarTurno(5, Hoy, 12);

        var enviados = await Recordatorio().EnviarLosDeHoy();

        Assert.Equal(1, enviados);
        Assert.Equal("paciente5@correo.com", Assert.Single(_correo.Enviados).Para);
    }

    [Fact]
    public async Task A_una_cuenta_dada_de_baja_no_se_le_recuerda()
    {
        AgregarTurno(1, Hoy, 10, activo: false);

        Assert.Equal(0, await Recordatorio().EnviarLosDeHoy());
        Assert.Empty(_notificaciones.Notificaciones);
    }

    [Fact]
    public async Task Cada_turno_se_recuerda_una_sola_vez()
    {
        AgregarTurno(1, Hoy, 10);

        await Recordatorio().EnviarLosDeHoy();
        _reloj.Momento = _reloj.Momento.AddMinutes(15);
        var segundaVuelta = await Recordatorio().EnviarLosDeHoy();

        Assert.Equal(0, segundaVuelta);
        Assert.Single(_correo.Enviados);
        Assert.Single(_notificaciones.Notificaciones);
    }

    [Fact]
    public async Task Si_el_turno_se_pasa_a_otra_hora_del_mismo_dia_se_vuelve_a_recordar()
    {
        var turno = AgregarTurno(1, Hoy, 10);
        await Recordatorio().EnviarLosDeHoy();

        turno.HoraInicio = TimeSpan.FromHours(16);
        var segundaVuelta = await Recordatorio().EnviarLosDeHoy();

        Assert.Equal(1, segundaVuelta);
        Assert.Equal(2, _correo.Enviados.Count);
    }

    [Fact]
    public async Task Dos_turnos_del_mismo_paciente_tienen_un_recordatorio_cada_uno()
    {
        AgregarTurno(1, Hoy, 10, idUsuario: 50);
        AgregarTurno(2, Hoy, 15, idUsuario: 50);

        Assert.Equal(2, await Recordatorio().EnviarLosDeHoy());
    }

    [Fact]
    public async Task El_recordatorio_dice_hora_doctor_especialidad_y_consultorio()
    {
        AgregarTurno(1, Hoy, 10);

        await Recordatorio().EnviarLosDeHoy();

        var aviso = Assert.Single(_notificaciones.Notificaciones);
        Assert.Equal("Hoy a las 10:00 tenés turno con Laura Gomez (Clínica), en el consultorio 101.", aviso.Mensaje);
        var enviado = Assert.Single(_correo.Enviados);
        Assert.Equal(RecordatorioDeTurnos.Asunto, enviado.Asunto);
        Assert.Contains("hoy, lunes 12 de octubre, a las 10:00", enviado.Texto);
        Assert.Contains("https://chronosalud.com/Turnos", enviado.Texto);
    }

    [Fact]
    public void Sin_datos_del_doctor_dice_tu_doctor()
    {
        var turno = new Turno { HoraInicio = TimeSpan.FromHours(9) };

        Assert.Equal("Hoy a las 09:00 tenés turno con tu doctor.", RecordatorioDeTurnos.Mensaje(turno));
    }

    // ---- Cuándo se revisa ----------------------------------------------------------

    [Fact]
    public void Se_revisa_desde_la_hora_de_envio()
    {
        var siete = TimeSpan.FromHours(7);

        Assert.False(RecordatoriosProgramados.TocaRevisar(new DateTime(2026, 10, 12, 6, 59, 0), siete));
        Assert.True(RecordatoriosProgramados.TocaRevisar(new DateTime(2026, 10, 12, 7, 0, 0), siete));
        Assert.True(RecordatoriosProgramados.TocaRevisar(new DateTime(2026, 10, 12, 18, 30, 0), siete));
    }

    [Theory]
    [InlineData("08:30", 8, 30)]
    [InlineData("no es una hora", 7, 0)]
    public void La_hora_de_envio_se_lee_de_la_configuracion(string valor, int horas, int minutos)
    {
        Assert.Equal(new TimeSpan(horas, minutos, 0), new OpcionesDeRecordatorios { Hora = valor }.HoraDeEnvio);
    }
}
