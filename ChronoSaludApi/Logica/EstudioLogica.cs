using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class EstudioLogica : IEstudioLogica
{
    /// <summary>5 MB: alcanza para un informe escaneado y no infla la base.</summary>
    public const int TamanoMaximoPdf = 5 * 1024 * 1024;

    private readonly IEstudioRepository _repo;
    private readonly IEstudioArchivoRepository _archivos;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ITurnoRepository _turnoRepo;
    private readonly INotificacionRepository _notifRepo;
    private readonly AvisosPorCorreo? _avisos;
    private readonly AvisosAlPersonal? _personal;
    private readonly IReloj _reloj;
    private readonly ILogger<EstudioLogica> _logger;

    public EstudioLogica(
        IEstudioRepository repo,
        IPacienteRepository pacienteRepo,
        IDoctorRepository doctorRepo,
        ITurnoRepository turnoRepo,
        INotificacionRepository notifRepo,
        IEstudioArchivoRepository archivos,
        IReloj reloj,
        ILogger<EstudioLogica> logger,
        AvisosPorCorreo? avisos = null,
        AvisosAlPersonal? personal = null)
    {
        _repo = repo;
        _archivos = archivos;
        _personal = personal;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
        _turnoRepo = turnoRepo;
        _notifRepo = notifRepo;
        _reloj = reloj;
        _logger = logger;
        _avisos = avisos;
    }

    public async Task<(IEnumerable<EstudioDto> estudios, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, string? tipo, string? estado, DateTime? desde, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (Enumerable.Empty<EstudioDto>(), "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != pacienteId)
                return (Enumerable.Empty<EstudioDto>(), null, true);
        }

        var estudios = (await _repo.ObtenerDePaciente(pacienteId, tipo, estado, desde)).ToList();
        var pdfs = await _archivos.NombresDe(estudios.Select(e => e.Id));
        return (estudios.Select(e => MapearDto(e, pdfs.GetValueOrDefault(e.Id))), null, false);
    }

    public async Task<(EstudioDto? estudio, string? error, bool prohibido)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsStaff)
    {
        // Mismo criterio que ObtenerDePaciente: el staff ve cualquier estudio y
        // el paciente solo los propios. El perfil se resuelve antes de buscar el
        // estudio para que "no tenés perfil" no dependa de que el id exista.
        Paciente? propio = null;
        if (!callerEsStaff)
        {
            propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (null, "Tu usuario no tiene un perfil de paciente asociado.", false);
        }

        var e = await _repo.ObtenerConDoctor(id);
        if (e == null) return (null, "Estudio no encontrado.", false);

        if (propio != null && propio.Id != e.IdPaciente)
            return (null, null, true);

        var pdfs = await _archivos.NombresDe(new[] { e.Id });
        return (MapearDto(e, pdfs.GetValueOrDefault(e.Id)), null, false);
    }

    /// <summary>
    /// El PDF del resultado, o el enlace externo si no hay PDF. Mismo permiso
    /// que <see cref="ObtenerPorId"/>. Si lo baja el paciente, el estudio pasa
    /// a "entregado": ya lo vio.
    /// </summary>
    public async Task<(EstudioArchivo? archivo, string? enlace, string? error, bool prohibido)> ObtenerArchivo(
        int id, int idUsuarioCaller, bool callerEsStaff)
    {
        var (estudio, error, prohibido) = await ObtenerPorId(id, idUsuarioCaller, callerEsStaff);
        if (estudio == null) return (null, null, error, prohibido);

        var archivo = await _archivos.Obtener(id);
        var enlace = archivo == null && !string.IsNullOrEmpty(estudio.ArchivoUrl) ? estudio.ArchivoUrl : null;
        if (archivo == null && enlace == null)
            return (null, null, "El estudio aún no tiene resultado disponible.", false);

        if (!callerEsStaff)
            await _repo.MarcarVisto(id);

        return (archivo, enlace, null, false);
    }

    /// <summary>
    /// El paciente del token vio sus resultados: los "validado" pasan a
    /// "entregado". Devuelve cuántos cambiaron.
    /// </summary>
    public async Task<(int marcados, string? error)> MarcarVistos(int idUsuarioCaller)
    {
        var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
        if (propio == null)
            return (0, "Tu usuario no tiene un perfil de paciente asociado.");

        return (await _repo.MarcarVistos(propio.Id), null);
    }

    /// <summary>
    /// Guarda o reemplaza el PDF del resultado. Tiene que empezar con la firma
    /// de un PDF ("%PDF-"): lo que diga el cliente sobre el tipo no se usa.
    /// </summary>
    public async Task<(bool ok, string? error)> GuardarArchivo(int id, byte[] contenido, string? nombre)
    {
        if (contenido.Length == 0)
            return (false, "El archivo está vacío.");
        if (contenido.Length > TamanoMaximoPdf)
            return (false, "El PDF no puede superar los 5 MB.");
        if (!EsPdf(contenido))
            return (false, "El archivo no es un PDF.");

        if (await _repo.ObtenerPorId(id) == null)
            return (false, "Estudio no encontrado.");

        await _archivos.Guardar(new EstudioArchivo
        {
            IdEstudio = id,
            Contenido = contenido,
            Nombre    = NombreDeArchivo(nombre, id),
            Tamano    = contenido.Length,
            SubidoEn  = _reloj.Ahora()
        });

        return (true, null);
    }

    public async Task<(bool ok, string? error)> QuitarArchivo(int id)
    {
        if (await _repo.ObtenerPorId(id) == null)
            return (false, "Estudio no encontrado.");

        await _archivos.Eliminar(id);
        return (true, null);
    }

    /// <summary>Los primeros bytes de todo PDF son "%PDF-".</summary>
    public static bool EsPdf(byte[] contenido) =>
        contenido.Length >= 5 && contenido.AsSpan(0, 5).SequenceEqual("%PDF-"u8);

    /// <summary>
    /// El nombre con el que se baja: sin carpetas ni caracteres raros, con
    /// ".pdf" al final y hasta 200 caracteres. Sin nombre, "resultado-estudio-12.pdf".
    /// </summary>
    public static string NombreDeArchivo(string? nombre, int idEstudio)
    {
        var limpio = new string(Path.GetFileName((nombre ?? string.Empty).Replace('\\', '/'))
            .Where(c => !char.IsControl(c) && c != '"' && c != '/' && c != ':')
            .ToArray()).Trim();

        if (limpio.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            limpio = limpio[..^4].TrimEnd();
        if (limpio.Length == 0)
            limpio = $"resultado-estudio-{idEstudio}";
        if (limpio.Length > 196)
            limpio = limpio[..196];

        return limpio + ".pdf";
    }

    /// <summary>
    /// Lo pide un doctor (el del token). Si viene un turno, tiene que ser de
    /// este paciente con este doctor, como en recetas e historia clínica.
    /// </summary>
    public async Task<(int? id, string? error)> Crear(EstudioCreateDto dto, int idUsuarioCaller)
    {
        var tiposValidos = new[] { "sangre", "imagen", "biopsia", "otro" };
        if (!tiposValidos.Contains(dto.Tipo))
            return (null, "Tipo inválido. Valores: sangre, imagen, biopsia, otro.");

        // Sin esto, un paciente inexistente se iba contra la foreign key y salía como 500.
        var paciente = await _pacienteRepo.ObtenerPorId(dto.IdPaciente);
        if (paciente == null)
            return (null, $"Paciente con id {dto.IdPaciente} no encontrado.");

        var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
        if (doctor == null)
            return (null, "Perfil de doctor no encontrado: tu usuario no tiene uno asociado.");

        var errorTurno = await TurnoVinculado.Revisar(_turnoRepo, dto.IdTurno, dto.IdPaciente, doctor.Id);
        if (errorTurno != null)
            return (null, errorTurno);

        // Si no mandan la fecha del pedido, es hoy.
        var fecha = dto.FechaSolicitud;
        if (fecha == default)
            fecha = _reloj.Ahora().Date;

        var estudio = new Estudio
        {
            IdPaciente     = dto.IdPaciente,
            IdTurno        = dto.IdTurno,
            Tipo           = dto.Tipo,
            Descripcion    = dto.Descripcion.Trim(),
            FechaSolicitud = fecha,
            Estado         = "pendiente"
        };

        await _repo.Agregar(estudio);
        return (estudio.Id, null);
    }

    public async Task<(bool ok, string? error)> CargarResultado(int id, EstudioResultadoDto dto, Solicitante? quien = null)
    {
        var estudio = await _repo.ObtenerPorId(id);
        if (estudio == null) return (false, "Estudio no encontrado.");

        var estadosValidos = new[] { "validado", "entregado" };
        if (!estadosValidos.Contains(dto.Estado))
            return (false, "Estado inválido. Valores: validado, entregado.");

        estudio.Resultado      = dto.Resultado;
        estudio.ArchivoUrl     = dto.ArchivoUrl;
        estudio.Estado         = dto.Estado;
        estudio.FechaResultado = dto.FechaResultado == default ? _reloj.Ahora() : dto.FechaResultado;

        await _repo.Actualizar(estudio);

        // Un fallo al insertar la notificación o al encolar el correo nunca
        // puede hacer fracasar ni parecer que fracasó la carga del resultado,
        // que ya se guardó.
        try
        {
            var paciente = await _pacienteRepo.ObtenerPorId(estudio.IdPaciente);
            if (paciente?.Usuario != null)
            {
                var mensaje = MensajeDeResultado(estudio);
                await _notifRepo.Agregar(new Notificacion
                {
                    IdUsuario = paciente.Usuario.Id,
                    Tipo      = "estudio",
                    Mensaje   = mensaje,
                    Fecha     = _reloj.Ahora()
                });

                // El mismo aviso, también por correo.
                if (_avisos != null)
                    await _avisos.Avisar(paciente.Usuario, "Ya está el resultado de tu estudio", mensaje, "/Estudios");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la notificación de resultado de estudio para el paciente {IdPaciente}.", estudio.IdPaciente);
        }

        // Al doctor que lo pidió, salvo que lo haya cargado él (paso 23).
        if (_personal != null && quien != null)
        {
            var conDoctor = await _repo.ObtenerConDoctor(id);
            await _personal.ResultadoCargado(estudio, conDoctor?.Turno?.Doctor, quien);
        }

        return (true, null);
    }

    /// <summary>
    /// "El resultado de tu estudio «Hemograma completo» ya está disponible."
    /// Si el estudio no tiene descripción, se nombra por el tipo.
    /// </summary>
    public static string MensajeDeResultado(Estudio estudio)
    {
        if (string.IsNullOrWhiteSpace(estudio.Descripcion))
            return $"El resultado de tu estudio de {estudio.Tipo} ya está disponible.";

        return $"El resultado de tu estudio «{estudio.Descripcion.Trim()}» ya está disponible.";
    }

    private static EstudioDto MapearDto(Estudio e, string? nombrePdf) => new EstudioDto(
        e.Id,
        e.Tipo,
        e.Estado,
        e.FechaSolicitud,
        e.IdTurno,
        e.Resultado,
        e.ArchivoUrl,
        e.FechaResultado,
        e.Descripcion,
        Profesional.Armar(e.Turno?.Doctor),
        nombrePdf != null,
        nombrePdf
    );
}
