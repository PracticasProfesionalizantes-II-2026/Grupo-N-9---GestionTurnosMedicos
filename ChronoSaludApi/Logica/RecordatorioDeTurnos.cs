using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public interface IRecordatorioDeTurnos
{
    /// <summary>Manda el recordatorio de los turnos de hoy que todavía no lo tienen. Devuelve cuántos mandó.</summary>
    Task<int> EnviarLosDeHoy(CancellationToken cancelacion = default);
}

/// <summary>
/// El recordatorio del día del turno: a cada paciente con un turno hoy
/// (pendiente o confirmado, que todavía no empezó) le llega un aviso en la
/// campana y un correo. Lo dispara <see cref="RecordatoriosProgramados"/>.
///
/// Para no mandarlo dos veces no hace falta una columna nueva: el aviso de la
/// campana queda guardado (tipo "recordatorio"), y si ya hay uno de hoy con el
/// mismo texto, ese turno ya está recordado. Si el turno se pasa a otra hora
/// del mismo día, el texto cambia y sale un recordatorio nuevo, que es lo que
/// corresponde.
/// </summary>
public class RecordatorioDeTurnos : IRecordatorioDeTurnos
{
    public const string Tipo = "recordatorio";
    public const string Asunto = "Hoy tenés turno";

    private readonly ITurnoRepository _turnos;
    private readonly INotificacionRepository _notificaciones;
    private readonly AvisosPorCorreo _avisos;
    private readonly IReloj _reloj;
    private readonly ILogger<RecordatorioDeTurnos> _log;

    public RecordatorioDeTurnos(
        ITurnoRepository turnos,
        INotificacionRepository notificaciones,
        AvisosPorCorreo avisos,
        IReloj reloj,
        ILogger<RecordatorioDeTurnos> log)
    {
        _turnos = turnos;
        _notificaciones = notificaciones;
        _avisos = avisos;
        _reloj = reloj;
        _log = log;
    }

    public async Task<int> EnviarLosDeHoy(CancellationToken cancelacion = default)
    {
        var ahora = _reloj.Ahora();
        var hoy = ahora.Date;

        // El listado trae al paciente y al doctor con sus usuarios.
        var deHoy = (await _turnos.ObtenerTodos(null, null, null, hoy, hoy))
            .Where(t => t.FechaInicio.Date == hoy
                        && EstadosTurno.EstaEnPie(t.Estado)
                        && hoy + t.HoraInicio > ahora)
            .OrderBy(t => t.HoraInicio);

        var enviados = 0;
        foreach (var turno in deHoy)
        {
            cancelacion.ThrowIfCancellationRequested();

            var usuario = turno.Paciente?.Usuario;
            if (usuario is null || !usuario.Activo)
                continue;

            var mensaje = Mensaje(turno);
            if (await _notificaciones.Existe(usuario.Id, Tipo, mensaje, hoy))
                continue;

            // Un turno que falla no frena a los demás.
            try
            {
                await _notificaciones.Agregar(new Notificacion
                {
                    IdUsuario = usuario.Id,
                    Tipo      = Tipo,
                    Mensaje   = mensaje,
                    Fecha     = ahora
                });
                await _avisos.Enviar(usuario, Asunto, Cuerpo(turno, usuario, hoy, _avisos.UrlDeLaWeb));
                enviados++;
            }
            catch (Exception error)
            {
                _log.LogError(error, "No se pudo mandar el recordatorio del turno {IdTurno}.", turno.Id);
            }
        }

        if (enviados > 0)
            _log.LogInformation("Se mandaron {Cuantos} recordatorios de turnos de hoy.", enviados);

        return enviados;
    }

    /// <summary>"Hoy a las 10:00 tenés turno con Laura Gomez (Clínica), en el consultorio 101."</summary>
    public static string Mensaje(Turno turno) =>
        $"Hoy a las {TextosDeTurno.Hora(turno.HoraInicio)} tenés turno con {ConQuien(turno)}.";

    public static string Cuerpo(Turno turno, Usuario paciente, DateTime hoy, string urlWeb) =>
        $"Hola, {paciente.Nombre}:\n\n" +
        $"Te recordamos que hoy, {TextosDeTurno.Dia(hoy, hoy)}, a las {TextosDeTurno.Hora(turno.HoraInicio)} " +
        $"tenés turno con {ConQuien(turno)}.\n\n" +
        "Llevá tu DNI y la credencial de tu cobertura, si tenés.\n\n" +
        $"Si no podés ir, cancelalo desde tus turnos, así el horario le queda a otra persona: {urlWeb}/Turnos";

    /// <summary>"Laura Gomez (Clínica), en el consultorio 101": el doctor, la especialidad y el consultorio, si están.</summary>
    private static string ConQuien(Turno turno)
    {
        var doctor = turno.Doctor;
        var nombre = $"{doctor?.Usuario?.Nombre} {doctor?.Usuario?.Apellido}".Trim();
        var texto = string.IsNullOrEmpty(nombre) ? "tu doctor" : nombre;

        if (!string.IsNullOrWhiteSpace(doctor?.Especialidad))
            texto += $" ({doctor.Especialidad.Trim()})";

        var consultorio = doctor?.Consultorio?.Trim();
        if (!string.IsNullOrEmpty(consultorio))
        {
            // El dato a veces ya trae la palabra: "Consultorio 101".
            texto += consultorio.StartsWith("consultorio", StringComparison.OrdinalIgnoreCase)
                ? $", en el {char.ToLowerInvariant(consultorio[0])}{consultorio[1..]}"
                : $", en el consultorio {consultorio}";
        }

        return texto;
    }
}
