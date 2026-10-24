using System.Text;
using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del paso 23: el PDF del resultado, los estados del estudio
/// (validado, entregado) y los avisos de la campana para el personal.
/// </summary>
public class EstudiosYAvisosAlPersonalTests
{
    // Paciente Ana (usuario 7, perfil 10), doctora Laura (usuario 3, perfil 20),
    // dos administradores activos (1 y 2) y uno dado de baja (9).
    private const int IdUsuarioPaciente = 7, IdPaciente = 10;
    private const int IdUsuarioDoctora = 3, IdDoctora = 20;

    private static readonly Solicitante Paciente = new(IdUsuarioPaciente, "paciente");
    private static readonly Solicitante Doctora = new(IdUsuarioDoctora, "doctor");
    private static readonly Solicitante Admin = new(1, "administrador");

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n%%EOF");

    private readonly RelojFijo _reloj = new RelojFijo { Momento = new DateTime(2026, 10, 12, 9, 0, 0) };
    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly TurnoRepositoryFalso _turnos = new TurnoRepositoryFalso();
    private readonly EstudioRepositoryFalso _estudios = new EstudioRepositoryFalso();
    private readonly EstudioArchivoRepositoryFalso _archivos = new EstudioArchivoRepositoryFalso();
    private readonly NotificacionRepositoryFalso _notificaciones = new NotificacionRepositoryFalso();
    private readonly UsuarioRepositoryFalso _usuarios;
    private readonly AvisosAlPersonal _personal;
    private readonly TurnoLogica _turnoLogica;
    private readonly EstudioLogica _estudioLogica;
    private readonly Doctor _laura;

    public EstudiosYAvisosAlPersonalTests()
    {
        _usuarios = new UsuarioRepositoryFalso(_pacientes);
        _usuarios.Usuarios.Add(new Usuario { Id = 1, Rol = "administrador", Activo = true });
        _usuarios.Usuarios.Add(new Usuario { Id = 2, Rol = "administrador", Activo = true });
        _usuarios.Usuarios.Add(new Usuario { Id = 9, Rol = "administrador", Activo = false });

        _pacientes.Pacientes.Add(new Paciente
        {
            Id = IdPaciente,
            IdUsuario = IdUsuarioPaciente,
            Usuario = new Usuario { Id = IdUsuarioPaciente, Nombre = "Ana", Apellido = "Duarte", Rol = "paciente", Email = "ana@x.com" }
        });
        _laura = new Doctor
        {
            Id = IdDoctora,
            IdUsuario = IdUsuarioDoctora,
            Especialidad = "Clínica",
            Activo = true,
            Usuario = new Usuario { Id = IdUsuarioDoctora, Nombre = "Laura", Apellido = "Gomez", Rol = "doctor", Activo = true }
        };
        _doctores.Doctores.Add(_laura);

        _personal = new AvisosAlPersonal(_notificaciones, _usuarios, _doctores, _pacientes, _reloj,
            NullLogger<AvisosAlPersonal>.Instance);

        _turnoLogica = new TurnoLogica(_turnos, _pacientes, _doctores, _notificaciones,
            NullLogger<TurnoLogica>.Instance, new HorarioLaboralLogicaFalsa(), new RegistroMovimientosFalso(), _reloj,
            personal: _personal);

        _estudioLogica = new EstudioLogica(_estudios, _pacientes, _doctores, _turnos, _notificaciones, _archivos, _reloj,
            NullLogger<EstudioLogica>.Instance, personal: _personal);
    }

    private IEnumerable<Notificacion> AvisosDe(int idUsuario) => _notificaciones.Notificaciones.Where(n => n.IdUsuario == idUsuario);

    private Turno TurnoDeAna(string estado = "pendiente", int dias = 1)
    {
        var turno = new Turno
        {
            Id = _turnos.Turnos.Count + 1,
            IdPaciente = IdPaciente,
            IdDoctor = IdDoctora,
            FechaInicio = _reloj.Momento.Date.AddDays(dias),
            HoraInicio = new TimeSpan(10, 0, 0),
            HoraFin = new TimeSpan(10, 30, 0),
            Estado = estado,
            Doctor = _laura
        };
        _turnos.Turnos.Add(turno);
        return turno;
    }

    private Estudio EstudioDeAna(string estado = "pendiente", bool conTurno = true)
    {
        var turno = conTurno ? TurnoDeAna("completado", dias: -1) : null;
        var estudio = new Estudio
        {
            Id = _estudios.Estudios.Count + 1,
            IdPaciente = IdPaciente,
            IdTurno = turno?.Id,
            Turno = turno,
            Tipo = "sangre",
            Descripcion = "Hemograma completo",
            Estado = estado
        };
        _estudios.Estudios.Add(estudio);
        return estudio;
    }

    // ---- Avisos de turnos ------------------------------------------------------

    [Fact]
    public async Task Cuando_el_paciente_pide_un_turno_le_llega_a_cada_administrador_activo()
    {
        var (id, error) = await _turnoLogica.Crear(
            new TurnoCreateDto(0, IdDoctora, new DateTime(2026, 10, 13), "10:00", "10:30", null), Paciente);

        Assert.Null(error);
        Assert.Equal(new[] { 1, 2 }, _notificaciones.Notificaciones.Where(n => n.IdUsuario != IdUsuarioPaciente).Select(n => n.IdUsuario));
        var aviso = AvisosDe(1).Single();
        Assert.Equal("turno", aviso.Tipo);
        Assert.Equal("Ana Duarte pidió un turno con Laura Gomez para el martes 13 de octubre a las 10:00. Falta confirmarlo.", aviso.Mensaje);
        Assert.Equal($"/Turnos/Detalle/{id}", aviso.Enlace);
        Assert.Empty(AvisosDe(9));
    }

    [Fact]
    public async Task Si_el_turno_lo_carga_el_personal_no_se_avisa_a_la_administracion()
    {
        await _turnoLogica.Crear(new TurnoCreateDto(IdPaciente, IdDoctora, new DateTime(2026, 10, 13), "10:00", "10:30", null), Admin);

        Assert.Empty(AvisosDe(1));
        Assert.Empty(AvisosDe(2));
    }

    [Fact]
    public async Task Si_cancela_el_paciente_le_llega_a_la_doctora()
    {
        var turno = TurnoDeAna();

        var (ok, _) = await _turnoLogica.Cancelar(turno.Id, Paciente);

        Assert.True(ok);
        var aviso = AvisosDe(IdUsuarioDoctora).Single();
        Assert.Equal("Ana Duarte canceló su turno del martes 13 de octubre a las 10:00.", aviso.Mensaje);
        Assert.Equal($"/Turnos/Detalle/{turno.Id}", aviso.Enlace);
    }

    [Fact]
    public async Task Si_cancela_la_administracion_el_aviso_lo_dice()
    {
        var turno = TurnoDeAna("confirmado");

        await _turnoLogica.Cancelar(turno.Id, Admin);

        Assert.Equal("La administración canceló el turno de Ana Duarte del martes 13 de octubre a las 10:00.",
            AvisosDe(IdUsuarioDoctora).Single().Mensaje);
    }

    [Fact]
    public async Task Cancelar_con_un_cambio_de_estado_tambien_avisa()
    {
        var turno = TurnoDeAna();

        var (ok, error, _) = await _turnoLogica.Actualizar(turno.Id, new TurnoUpdateDto(null, null, null, "cancelado", null), Admin);

        Assert.True(ok, error);
        Assert.Single(AvisosDe(IdUsuarioDoctora));
    }

    [Fact]
    public async Task Si_cancela_la_propia_doctora_no_se_avisa_a_si_misma()
    {
        var turno = TurnoDeAna();

        await _turnoLogica.Cancelar(turno.Id, Doctora);

        Assert.Empty(AvisosDe(IdUsuarioDoctora));
    }

    [Fact]
    public async Task Confirmar_no_le_avisa_al_personal()
    {
        var turno = TurnoDeAna();

        await _turnoLogica.Actualizar(turno.Id, new TurnoUpdateDto(null, null, null, "confirmado", null), Admin);

        Assert.Empty(AvisosDe(IdUsuarioDoctora));
    }

    [Fact]
    public async Task Si_falla_el_aviso_la_cancelacion_igual_se_hace()
    {
        var personal = new AvisosAlPersonal(new NotificacionesQueFallan(), _usuarios, _doctores, _pacientes, _reloj,
            NullLogger<AvisosAlPersonal>.Instance);
        var logica = new TurnoLogica(_turnos, _pacientes, _doctores, new NotificacionesQueFallan(),
            NullLogger<TurnoLogica>.Instance, new HorarioLaboralLogicaFalsa(), new RegistroMovimientosFalso(), _reloj,
            personal: personal);
        var turno = TurnoDeAna();

        var (ok, error) = await logica.Cancelar(turno.Id, Paciente);

        Assert.True(ok, error);
        Assert.Equal("cancelado", turno.Estado);
    }

    // ---- Aviso del resultado ----------------------------------------------------

    [Fact]
    public async Task Si_el_resultado_lo_carga_la_administracion_le_llega_a_la_doctora_que_lo_pidio()
    {
        var estudio = EstudioDeAna();

        await _estudioLogica.CargarResultado(estudio.Id, new EstudioResultadoDto("Normal.", null, "validado", default), Admin);

        var aviso = AvisosDe(IdUsuarioDoctora).Single();
        Assert.Equal("estudio", aviso.Tipo);
        Assert.Equal("Llegó el resultado del estudio «Hemograma completo» de Ana Duarte.", aviso.Mensaje);
        Assert.Equal($"/Estudios?paciente={IdPaciente}", aviso.Enlace);
        Assert.Equal("validado", estudio.Estado);
    }

    [Fact]
    public async Task Si_lo_carga_la_misma_doctora_o_el_estudio_no_tiene_turno_no_hay_aviso()
    {
        var propio = EstudioDeAna();
        var sinTurno = EstudioDeAna(conTurno: false);

        await _estudioLogica.CargarResultado(propio.Id, new EstudioResultadoDto("Normal.", null, "validado", default), Doctora);
        await _estudioLogica.CargarResultado(sinTurno.Id, new EstudioResultadoDto("Normal.", null, "validado", default), Admin);

        Assert.Empty(AvisosDe(IdUsuarioDoctora));
    }

    // ---- Estados: validado y entregado ---------------------------------------------

    [Fact]
    public async Task Al_ver_sus_estudios_el_paciente_pasa_los_validados_a_entregados()
    {
        var pendiente = EstudioDeAna("pendiente");
        var validado = EstudioDeAna("validado");
        var entregado = EstudioDeAna("entregado");

        var (marcados, error) = await _estudioLogica.MarcarVistos(IdUsuarioPaciente);

        Assert.Null(error);
        Assert.Equal(1, marcados);
        Assert.Equal(("pendiente", "entregado", "entregado"), (pendiente.Estado, validado.Estado, entregado.Estado));
    }

    [Fact]
    public async Task Marcar_vistos_sin_perfil_de_paciente_da_error()
    {
        var (marcados, error) = await _estudioLogica.MarcarVistos(idUsuarioCaller: 55);

        Assert.Equal(0, marcados);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Bajar_el_pdf_lo_marca_entregado_si_es_el_paciente_y_no_si_es_el_personal()
    {
        var estudio = EstudioDeAna("validado");
        await _estudioLogica.GuardarArchivo(estudio.Id, Pdf, "informe.pdf");

        var (porPersonal, _, _, _) = await _estudioLogica.ObtenerArchivo(estudio.Id, 1, callerEsStaff: true);
        Assert.NotNull(porPersonal);
        Assert.Equal("validado", estudio.Estado);

        var (porPaciente, _, _, _) = await _estudioLogica.ObtenerArchivo(estudio.Id, IdUsuarioPaciente, callerEsStaff: false);
        Assert.Equal(Pdf, porPaciente!.Contenido);
        Assert.Equal("entregado", estudio.Estado);
    }

    [Fact]
    public async Task Otro_paciente_no_baja_el_pdf()
    {
        var estudio = EstudioDeAna("validado");
        await _estudioLogica.GuardarArchivo(estudio.Id, Pdf, "informe.pdf");
        _pacientes.Pacientes.Add(new Paciente { Id = 11, IdUsuario = 8 });

        var (archivo, _, _, prohibido) = await _estudioLogica.ObtenerArchivo(estudio.Id, 8, callerEsStaff: false);

        Assert.Null(archivo);
        Assert.True(prohibido);
    }

    [Fact]
    public async Task Sin_pdf_devuelve_el_enlace_del_laboratorio_y_sin_ninguno_da_error()
    {
        var conEnlace = EstudioDeAna("validado");
        conEnlace.ArchivoUrl = "https://laboratorio.com/r/1";
        var sinNada = EstudioDeAna("validado");

        var (_, enlace, _, _) = await _estudioLogica.ObtenerArchivo(conEnlace.Id, 1, callerEsStaff: true);
        var (archivo, otroEnlace, error, _) = await _estudioLogica.ObtenerArchivo(sinNada.Id, 1, callerEsStaff: true);

        Assert.Equal("https://laboratorio.com/r/1", enlace);
        Assert.Null(archivo);
        Assert.Null(otroEnlace);
        Assert.NotNull(error);
    }

    // ---- El PDF ------------------------------------------------------------------

    [Fact]
    public async Task Se_guarda_el_pdf_y_la_lista_dice_que_lo_tiene()
    {
        var estudio = EstudioDeAna("validado");

        var (ok, error) = await _estudioLogica.GuardarArchivo(estudio.Id, Pdf, @"C:\Escaneos\Informe Ana.PDF");

        Assert.True(ok, error);
        Assert.Equal("Informe Ana.pdf", _archivos.Archivos[estudio.Id].Nombre);
        Assert.Equal(Pdf.Length, _archivos.Archivos[estudio.Id].Tamano);

        var (estudios, _, _) = await _estudioLogica.ObtenerDePaciente(IdPaciente, null, null, null, 1, callerEsStaff: true);
        var fila = estudios.Single(e => e.IdEstudio == estudio.Id);
        Assert.True(fila.TienePdf);
        Assert.Equal("Informe Ana.pdf", fila.NombrePdf);
    }

    [Fact]
    public async Task Un_archivo_que_no_es_pdf_se_rechaza()
    {
        var estudio = EstudioDeAna();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var (ok, error) = await _estudioLogica.GuardarArchivo(estudio.Id, png, "informe.pdf");

        Assert.False(ok);
        Assert.Equal("El archivo no es un PDF.", error);
        Assert.Empty(_archivos.Archivos);
    }

    [Fact]
    public async Task Un_pdf_de_mas_de_5_mb_o_vacio_se_rechaza()
    {
        var estudio = EstudioDeAna();
        var grande = new byte[EstudioLogica.TamanoMaximoPdf + 1];
        Pdf.CopyTo(grande, 0);

        var (okGrande, errorGrande) = await _estudioLogica.GuardarArchivo(estudio.Id, grande, "a.pdf");
        var (okVacio, _) = await _estudioLogica.GuardarArchivo(estudio.Id, Array.Empty<byte>(), "a.pdf");

        Assert.False(okGrande);
        Assert.Contains("5 MB", errorGrande);
        Assert.False(okVacio);
    }

    [Fact]
    public async Task Subir_otro_pdf_reemplaza_y_quitarlo_lo_borra()
    {
        var estudio = EstudioDeAna();
        await _estudioLogica.GuardarArchivo(estudio.Id, Pdf, "uno.pdf");
        await _estudioLogica.GuardarArchivo(estudio.Id, Pdf, "dos.pdf");

        Assert.Equal("dos.pdf", _archivos.Archivos[estudio.Id].Nombre);

        var (ok, _) = await _estudioLogica.QuitarArchivo(estudio.Id);
        Assert.True(ok);
        Assert.Empty(_archivos.Archivos);
    }

    [Fact]
    public async Task Un_estudio_que_no_existe_no_recibe_pdf()
    {
        var (ok, error) = await _estudioLogica.GuardarArchivo(999, Pdf, "a.pdf");

        Assert.False(ok);
        Assert.Contains("no encontrado", error);
    }

    [Theory]
    [InlineData("informe.pdf", "informe.pdf")]
    [InlineData("../../etc/passwd", "passwd.pdf")]
    [InlineData("C:\\docs\\Resultado.PDF", "Resultado.pdf")]
    [InlineData("a\"b:c.pdf", "abc.pdf")]
    [InlineData("", "resultado-estudio-12.pdf")]
    [InlineData(null, "resultado-estudio-12.pdf")]
    [InlineData(".pdf", "resultado-estudio-12.pdf")]
    public void El_nombre_del_pdf_queda_limpio(string? nombre, string esperado)
    {
        Assert.Equal(esperado, EstudioLogica.NombreDeArchivo(nombre, 12));
    }

    [Fact]
    public void El_nombre_largo_se_corta_en_200_con_la_extension()
    {
        var nombre = EstudioLogica.NombreDeArchivo(new string('a', 300) + ".pdf", 1);

        Assert.Equal(200, nombre.Length);
        Assert.EndsWith(".pdf", nombre);
    }

    [Fact]
    public void Se_reconoce_un_pdf_por_su_firma()
    {
        Assert.True(EstudioLogica.EsPdf(Pdf));
        Assert.False(EstudioLogica.EsPdf(Encoding.ASCII.GetBytes("%PD")));
        Assert.False(EstudioLogica.EsPdf(Encoding.ASCII.GetBytes("hola, soy un texto")));
    }

    /// <summary>Un repositorio de notificaciones que siempre falla al guardar.</summary>
    private sealed class NotificacionesQueFallan : INotificacionRepository
    {
        public Task<(int total, List<Notificacion> notificaciones)> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo, int pagina, int limite)
            => Task.FromResult((0, new List<Notificacion>()));
        public Task<Notificacion?> ObtenerPorId(int id) => Task.FromResult<Notificacion?>(null);
        public Task<int> ContarNoLeidas(int usuarioId) => Task.FromResult(0);
        public Task<int> MarcarTodasLeidas(int usuarioId) => Task.FromResult(0);
        public Task Agregar(Notificacion notificacion) => throw new InvalidOperationException("La base no responde.");
        public Task Actualizar(Notificacion notificacion) => Task.CompletedTask;
        public Task<bool> Existe(int usuarioId, string tipo, string mensaje, DateTime desde) => Task.FromResult(false);
    }
}
