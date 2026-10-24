using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Los avisos de la campana para el personal (paso 23): a la administración,
/// cuando un paciente pide un turno que hay que confirmar; al doctor, cuando le
/// cancelan un turno o llega el resultado de un estudio que pidió. Solo van a
/// la campana, sin correo, para no llenarle la casilla a nadie. Como los del
/// paciente, un fallo acá nunca hace fracasar la operación que ya se guardó.
/// </summary>
public class AvisosAlPersonal
{
    private readonly INotificacionRepository _notificaciones;
    private readonly IUsuarioRepository _usuarios;
    private readonly IDoctorRepository _doctores;
    private readonly IPacienteRepository _pacientes;
    private readonly IReloj _reloj;
    private readonly ILogger<AvisosAlPersonal> _log;

    public AvisosAlPersonal(
        INotificacionRepository notificaciones,
        IUsuarioRepository usuarios,
        IDoctorRepository doctores,
        IPacienteRepository pacientes,
        IReloj reloj,
        ILogger<AvisosAlPersonal> log)
    {
        _notificaciones = notificaciones;
        _usuarios = usuarios;
        _doctores = doctores;
        _pacientes = pacientes;
        _reloj = reloj;
        _log = log;
    }

    /// <summary>Un paciente pidió un turno: le llega a cada administrador activo, que es quien lo confirma.</summary>
    public async Task TurnoPedido(Turno turno)
    {
        try
        {
            var paciente = NombreDe((await _pacientes.ObtenerPorId(turno.IdPaciente))?.Usuario, "Un paciente");
            var doctor = NombreDe((await _doctores.ObtenerPorId(turno.IdDoctor))?.Usuario, "un doctor");
            var mensaje = MensajeTurnoPedido(paciente, doctor, turno.FechaInicio, turno.HoraInicio, _reloj.Ahora());

            foreach (var idUsuario in await _usuarios.ObtenerIdsDeAdministradoresActivos())
                await Agregar(idUsuario, "turno", mensaje, EnlaceDeTurno(turno.Id));
        }
        catch (Exception error)
        {
            _log.LogError(error, "No se pudo avisar a la administración del turno {IdTurno}.", turno.Id);
        }
    }

    /// <summary>
    /// Se canceló un turno: le llega al doctor, salvo que lo haya cancelado él
    /// mismo. Dice si fue el paciente o la administración.
    /// </summary>
    public async Task TurnoCancelado(Turno turno, Solicitante quien)
    {
        try
        {
            var doctor = await _doctores.ObtenerPorId(turno.IdDoctor);
            if (doctor?.Usuario is not { Activo: true } || doctor.IdUsuario == quien.IdUsuario)
                return;

            var paciente = NombreDe((await _pacientes.ObtenerPorId(turno.IdPaciente))?.Usuario, "el paciente");
            var mensaje = MensajeTurnoCancelado(paciente, quien.EsPaciente, turno.FechaInicio, turno.HoraInicio, _reloj.Ahora());

            await Agregar(doctor.IdUsuario, "turno", mensaje, EnlaceDeTurno(turno.Id));
        }
        catch (Exception error)
        {
            _log.LogError(error, "No se pudo avisar al doctor de la cancelación del turno {IdTurno}.", turno.Id);
        }
    }

    /// <summary>
    /// Llegó el resultado de un estudio: le llega al doctor que lo pidió (el
    /// del turno vinculado), salvo que lo haya cargado él mismo.
    /// </summary>
    public async Task ResultadoCargado(Estudio estudio, Doctor? pidio, Solicitante quien)
    {
        try
        {
            if (pidio?.Usuario is not { Activo: true } || pidio.IdUsuario == quien.IdUsuario)
                return;

            var paciente = NombreDe((await _pacientes.ObtenerPorId(estudio.IdPaciente))?.Usuario, "un paciente");
            await Agregar(pidio.IdUsuario, "estudio", MensajeResultado(estudio, paciente), $"/Estudios?paciente={estudio.IdPaciente}");
        }
        catch (Exception error)
        {
            _log.LogError(error, "No se pudo avisar al doctor del resultado del estudio {IdEstudio}.", estudio.Id);
        }
    }

    /// <summary>"Ana Duarte pidió un turno con Laura Gomez para el lunes 13 de octubre a las 10:00. Falta confirmarlo."</summary>
    public static string MensajeTurnoPedido(string paciente, string doctor, DateTime fecha, TimeSpan hora, DateTime hoy) =>
        $"{paciente} pidió un turno con {doctor} para el {TextosDeTurno.Dia(fecha, hoy)} a las {TextosDeTurno.Hora(hora)}. Falta confirmarlo.";

    /// <summary>
    /// "Ana Duarte canceló su turno del lunes 13 de octubre a las 10:00." o
    /// "La administración canceló el turno de Ana Duarte del …".
    /// </summary>
    public static string MensajeTurnoCancelado(string paciente, bool loCanceloElPaciente, DateTime fecha, TimeSpan hora, DateTime hoy)
    {
        var cuando = $"{TextosDeTurno.Dia(fecha, hoy)} a las {TextosDeTurno.Hora(hora)}";
        return loCanceloElPaciente
            ? $"{paciente} canceló su turno del {cuando}."
            : $"La administración canceló el turno de {paciente} del {cuando}.";
    }

    /// <summary>"Llegó el resultado del estudio «Hemograma completo» de Ana Duarte."</summary>
    public static string MensajeResultado(Estudio estudio, string paciente) =>
        string.IsNullOrWhiteSpace(estudio.Descripcion)
            ? $"Llegó el resultado del estudio de {estudio.Tipo} de {paciente}."
            : $"Llegó el resultado del estudio «{estudio.Descripcion.Trim()}» de {paciente}.";

    public static string EnlaceDeTurno(int idTurno) => $"/Turnos/Detalle/{idTurno}";

    private static string NombreDe(Usuario? usuario, string siFalta)
    {
        var nombre = $"{usuario?.Nombre} {usuario?.Apellido}".Trim();
        return string.IsNullOrEmpty(nombre) ? siFalta : nombre;
    }

    private Task Agregar(int idUsuario, string tipo, string mensaje, string enlace) =>
        _notificaciones.Agregar(new Notificacion
        {
            IdUsuario = idUsuario,
            Tipo      = tipo,
            Mensaje   = mensaje,
            Fecha     = _reloj.Ahora(),
            Enlace    = enlace
        });
}
