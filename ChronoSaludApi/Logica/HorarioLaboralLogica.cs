using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class HorarioLaboralLogica : IHorarioLaboralLogica
{
    private const int DuracionFranjaMinutos = 30;

    private static readonly string[] NombresDia =
        ["domingos", "lunes", "martes", "miércoles", "jueves", "viernes", "sábados"];

    private readonly IHorarioLaboralRepository _repo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ITurnoRepository _turnoRepo;
    private readonly ILogger<HorarioLaboralLogica> _logger;

    public HorarioLaboralLogica(
        IHorarioLaboralRepository repo,
        IDoctorRepository doctorRepo,
        ITurnoRepository turnoRepo,
        ILogger<HorarioLaboralLogica> logger)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
        _turnoRepo = turnoRepo;
        _logger = logger;
    }

    public async Task<(IEnumerable<HorarioLaboralDto>? horarios, string? error)> ObtenerPorDoctor(int idDoctor)
    {
        var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
        if (doctor == null) return (null, "Doctor no encontrado.");

        var horarios = await _repo.ObtenerPorDoctor(idDoctor);
        return (horarios.Select(h => new HorarioLaboralDto(
            h.DiaSemana,
            h.HoraInicio.ToString(@"hh\:mm"),
            h.HoraFin.ToString(@"hh\:mm")
        )), null);
    }

    // El solicitante todavía no decide nada acá: el endpoint solo deja pasar al
    // administrador. Se recibe para no volver a cambiar la firma cuando el
    // doctor pueda editar su propio horario.
    public async Task<(bool ok, string? error, IReadOnlyList<TurnoEnConflictoDto> conflictos, bool reintentar)> Reemplazar(
        int idDoctor, HorarioSemanalDto dto, Solicitante solicitante)
    {
        var (nuevos, error) = await Validar(idDoctor, dto);
        if (nuevos == null)
            return (false, error, SinConflictos, false);

        IReadOnlyList<Turno> frenan;
        try
        {
            frenan = await _repo.ReemplazarHorarios(idDoctor, nuevos, FechaArgentina.Hoy(), TurnosQueQuedanFuera(nuevos));
        }
        catch (BaseOcupadaException ex)
        {
            _logger.LogWarning(ex, "Bloqueo al reemplazar el horario del doctor {IdDoctor}.", idDoctor);
            return (false,
                "No se pudo guardar el horario porque en ese instante se estaba registrando un turno del doctor. " +
                "No se guardó nada: probá de nuevo.",
                SinConflictos, true);
        }

        if (frenan.Count == 0)
            return (true, null, SinConflictos, false);

        var conflictos = frenan
            .Select(t => new TurnoEnConflictoDto(
                t.Id,
                t.FechaInicio,
                t.HoraInicio.ToString(@"hh\:mm"),
                t.Estado,
                $"{t.Paciente?.Usuario?.Nombre} {t.Paciente?.Usuario?.Apellido}".Trim()))
            .ToList();

        return (false,
            conflictos.Count == 1
                ? "Hay 1 turno reservado que quedaría fuera del horario. No se guardó nada."
                : $"Hay {conflictos.Count} turnos reservados que quedarían fuera del horario. No se guardó nada.",
            conflictos, false);
    }

    private static readonly IReadOnlyList<TurnoEnConflictoDto> SinConflictos = Array.Empty<TurnoEnConflictoDto>();

    // Un turno en estos estados sigue en pie: es el que no puede quedar fuera
    // del horario. Los completados y los cancelados ya no ocupan la agenda.
    private static readonly string[] EstadosReservados = ["pendiente", "confirmado"];

    /// <summary>
    /// La regla del control: frenan los turnos pendientes o confirmados que
    /// entran en el horario actual y dejarían de entrar en el nuevo. Uno que
    /// ya está fuera del horario actual no frena: no es este cambio el que lo
    /// deja ahí. Se compara con la hora de fin real de cada turno.
    /// </summary>
    private static Func<IReadOnlyList<HorarioLaboral>, IReadOnlyList<Turno>, IReadOnlyList<Turno>> TurnosQueQuedanFuera(
        IReadOnlyList<HorarioLaboral> nuevos) =>
        (actuales, turnos) => turnos
            .Where(t => EstadosReservados.Contains(t.Estado))
            .Where(t => EntraEn(actuales, t) && !EntraEn(nuevos, t))
            .OrderBy(t => t.FechaInicio)
            .ThenBy(t => t.HoraInicio)
            .ThenBy(t => t.Id)
            .ToList();

    private static bool EntraEn(IReadOnlyList<HorarioLaboral> horario, Turno turno)
    {
        var dia = horario.FirstOrDefault(h => h.DiaSemana == (int)turno.FechaInicio.DayOfWeek);
        return dia != null && turno.HoraInicio >= dia.HoraInicio && turno.HoraFin <= dia.HoraFin;
    }

    /// <summary>Las filas a guardar, o null con el motivo si el pedido no es válido.</summary>
    private async Task<(List<HorarioLaboral>? nuevos, string? error)> Validar(int idDoctor, HorarioSemanalDto dto)
    {
        var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
        if (doctor == null) return (null, "Doctor no encontrado.");
        if (!doctor.Activo) return (null, "El doctor no está activo.");

        // Una lista vacía es válida: el doctor deja de atender todos los días.
        var items = dto.Horarios ?? new List<HorarioLaboralDto>();
        var nuevos = new List<HorarioLaboral>();

        foreach (var item in items)
        {
            if (item.DiaSemana < 0 || item.DiaSemana > 6)
                return (null, "DiaSemana debe estar entre 0 (domingo) y 6 (sábado).");
            if (nuevos.Any(n => n.DiaSemana == item.DiaSemana))
                return (null, $"El día {item.DiaSemana} está repetido en el horario.");
            if (!TryParseHora(item.HoraInicio, out var horaInicio))
                return (null, "Formato de hora inicio inválido. Use HH:MM.");
            if (!TryParseHora(item.HoraFin, out var horaFin))
                return (null, "Formato de hora fin inválido. Use HH:MM.");
            if (horaFin <= horaInicio)
                return (null, "La hora de fin debe ser posterior a la hora de inicio.");
            if (!EsEnPuntoOYMedia(horaInicio) || !EsEnPuntoOYMedia(horaFin))
                return (null, "Las horas del horario tienen que ser en punto o y media (por ejemplo, 08:00 u 08:30).");

            nuevos.Add(new HorarioLaboral
            {
                IdDoctor   = idDoctor,
                DiaSemana  = item.DiaSemana,
                HoraInicio = horaInicio,
                HoraFin    = horaFin
            });
        }

        return (nuevos, null);
    }

    public async Task<(IEnumerable<FranjaDisponibleDto>? franjas, string? error)> ObtenerDisponibilidad(int idDoctor, DateTime fecha)
    {
        var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
        if (doctor == null) return (null, "Doctor no encontrado.");
        if (!doctor.Activo) return (null, "El doctor no está activo.");

        var dia = fecha.Date;
        var hoy = DateTime.Today;
        if (dia < hoy) return (Enumerable.Empty<FranjaDisponibleDto>(), null);

        var horario = await _repo.ObtenerPorDoctorYDia(idDoctor, (int)dia.DayOfWeek);
        if (horario == null) return (Enumerable.Empty<FranjaDisponibleDto>(), null);

        var ocupados = (await _turnoRepo.ObtenerTodos(null, idDoctor, null, dia, dia.AddDays(1).AddTicks(-1)))
            .Where(t => t.Estado != "cancelado")
            .ToList();

        var duracion = TimeSpan.FromMinutes(DuracionFranjaMinutos);
        var ahora = DateTime.Now.TimeOfDay;
        var franjas = new List<FranjaDisponibleDto>();

        for (var inicio = horario.HoraInicio; inicio + duracion <= horario.HoraFin; inicio += duracion)
        {
            var fin = inicio + duracion;

            if (dia == hoy && inicio < ahora) continue;
            if (ocupados.Any(t => t.HoraInicio < fin && t.HoraFin > inicio)) continue;

            franjas.Add(new FranjaDisponibleDto(inicio.ToString(@"hh\:mm"), fin.ToString(@"hh\:mm")));
        }

        return (franjas, null);
    }

    public async Task<IEnumerable<string>> ObtenerEspecialidades()
    {
        var doctores = await _doctorRepo.ObtenerTodos(null, null);
        return doctores
            .Select(d => d.Especialidad)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .OrderBy(e => e)
            .ToList();
    }

    public async Task<(bool ok, string? error)> ValidarHorarioLaboral(int idDoctor, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
    {
        var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
        if (doctor == null || !doctor.Activo)
            return (false, "El doctor no existe o no está activo.");

        if (horaFin <= horaInicio)
            return (false, "La hora de fin debe ser posterior a la hora de inicio.");

        if (fecha.Date < DateTime.Today)
            return (false, "No se pueden crear turnos en fechas pasadas.");

        var diaSemana = (int)fecha.DayOfWeek;
        var horario = await _repo.ObtenerPorDoctorYDia(idDoctor, diaSemana);
        if (horario == null)
            return (false, $"El doctor no atiende los {NombresDia[diaSemana]}.");

        if (horaInicio < horario.HoraInicio || horaFin > horario.HoraFin)
            return (false, $"El turno debe estar dentro del horario del doctor " +
                           $"({horario.HoraInicio:hh\\:mm} a {horario.HoraFin:hh\\:mm}).");

        return (true, null);
    }

    // Las franjas de turno se cuentan de a DuracionFranjaMinutos desde la hora
    // de inicio: un horario que no cae en punto o y media deja minutos que
    // nadie puede reservar.
    private static bool EsEnPuntoOYMedia(TimeSpan hora) =>
        hora.Minutes % DuracionFranjaMinutos == 0 && hora.Seconds == 0;

    private static bool TryParseHora(string? texto, out TimeSpan hora)
    {
        hora = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        if (!TimeSpan.TryParse(texto, out hora)) return false;
        return hora >= TimeSpan.Zero && hora < TimeSpan.FromDays(1);
    }
}
