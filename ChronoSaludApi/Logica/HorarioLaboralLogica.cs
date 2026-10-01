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

    public HorarioLaboralLogica(
        IHorarioLaboralRepository repo,
        IDoctorRepository doctorRepo,
        ITurnoRepository turnoRepo)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
        _turnoRepo = turnoRepo;
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

    public async Task<(bool ok, string? error)> Reemplazar(int idDoctor, HorarioSemanalDto dto)
    {
        var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
        if (doctor == null) return (false, "Doctor no encontrado.");
        if (!doctor.Activo) return (false, "El doctor no está activo.");

        // Una lista vacía es válida: el doctor deja de atender todos los días.
        var items = dto.Horarios ?? new List<HorarioLaboralDto>();
        var nuevos = new List<HorarioLaboral>();

        foreach (var item in items)
        {
            if (item.DiaSemana < 0 || item.DiaSemana > 6)
                return (false, "DiaSemana debe estar entre 0 (domingo) y 6 (sábado).");
            if (nuevos.Any(n => n.DiaSemana == item.DiaSemana))
                return (false, $"El día {item.DiaSemana} está repetido en el horario.");
            if (!TryParseHora(item.HoraInicio, out var horaInicio))
                return (false, "Formato de hora inicio inválido. Use HH:MM.");
            if (!TryParseHora(item.HoraFin, out var horaFin))
                return (false, "Formato de hora fin inválido. Use HH:MM.");
            if (horaFin <= horaInicio)
                return (false, "La hora de fin debe ser posterior a la hora de inicio.");

            nuevos.Add(new HorarioLaboral
            {
                IdDoctor   = idDoctor,
                DiaSemana  = item.DiaSemana,
                HoraInicio = horaInicio,
                HoraFin    = horaFin
            });
        }

        await _repo.ReemplazarHorarios(idDoctor, nuevos);
        return (true, null);
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

    private static bool TryParseHora(string? texto, out TimeSpan hora)
    {
        hora = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        if (!TimeSpan.TryParse(texto, out hora)) return false;
        return hora >= TimeSpan.Zero && hora < TimeSpan.FromDays(1);
    }
}
