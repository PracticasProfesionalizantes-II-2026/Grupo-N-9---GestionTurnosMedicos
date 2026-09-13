using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class TurnoLogica : ITurnoLogica
{
    private readonly ITurnoRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IDoctorRepository _doctorRepo;

    public TurnoLogica(ITurnoRepository repo, IPacienteRepository pacienteRepo, IDoctorRepository doctorRepo)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
    }

    public async Task<(int total, IEnumerable<TurnoListaDto> turnos, string? error)> ObtenerTodos(
        int? pacienteId, int? doctorId, string? estado,
        DateTime? desde, DateTime? hasta, int pagina, int limite,
        int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor)
    {
        // El paciente/doctor autenticado nunca elige de quién ve la agenda:
        // se ignora lo que venga en la query y se fuerza siempre lo propio.
        if (callerEsPaciente)
        {
            var paciente = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (paciente == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), "Tu usuario no tiene un perfil de paciente asociado.");
            pacienteId = paciente.Id;
        }
        else if (callerEsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (doctor == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), "Tu usuario no tiene un perfil de doctor asociado.");
            doctorId = doctor.Id;
        }

        var todos = await _repo.ObtenerTodos(pacienteId, doctorId, estado, desde, hasta);
        var total = todos.Count();
        var resultado = todos
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(t => new TurnoListaDto(
                t.Id,
                t.FechaInicio,
                t.HoraInicio.ToString(@"hh\:mm"),
                t.Estado,
                $"{t.Doctor?.Usuario?.Nombre} {t.Doctor?.Usuario?.Apellido}",
                t.Doctor?.Especialidad ?? string.Empty,
                $"{t.Paciente?.Usuario?.Nombre} {t.Paciente?.Usuario?.Apellido}"
            ));
        return (total, resultado, null);
    }

    public async Task<(TurnoDto? turno, string? error)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor, bool callerEsStaff)
    {
        var t = await _repo.ObtenerPorId(id);
        if (t == null) return (null, "Turno no encontrado.");

        if (!callerEsStaff)
        {
            if (callerEsPaciente)
            {
                var paciente = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
                if (paciente == null)
                    return (null, "Tu usuario no tiene un perfil de paciente asociado.");
                if (paciente.Id != t.IdPaciente)
                    return (null, "Turno no encontrado.");
            }
            else if (callerEsDoctor)
            {
                var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
                if (doctor == null)
                    return (null, "Tu usuario no tiene un perfil de doctor asociado.");
                if (doctor.Id != t.IdDoctor)
                    return (null, "Turno no encontrado.");
            }
            else
            {
                return (null, "Turno no encontrado.");
            }
        }

        return (new TurnoDto(
            t.Id,
            t.FechaInicio,
            t.HoraInicio.ToString(@"hh\:mm"),
            t.HoraFin.ToString(@"hh\:mm"),
            t.Estado,
            t.IdPaciente,
            t.IdDoctor,
            t.Observaciones
        ), null);
    }

    public async Task<(int? id, string? error)> Crear(TurnoCreateDto dto)
    {
        if (!TimeSpan.TryParse(dto.HoraInicio, out var horaInicio))
            return (null, "Formato de hora inicio inválido. Use HH:MM.");
        if (!TimeSpan.TryParse(dto.HoraFin, out var horaFin))
            return (null, "Formato de hora fin inválido. Use HH:MM.");

        var conflicto = await _repo.HayConflictoHorario(dto.IdDoctor, dto.FechaInicio, horaInicio, horaFin);
        if (conflicto)
            return (null, "Conflicto de horario: el doctor ya tiene un turno en ese rango.");

        var turno = new Turno
        {
            IdPaciente   = dto.IdPaciente,
            IdDoctor     = dto.IdDoctor,
            FechaInicio  = dto.FechaInicio,
            HoraInicio   = horaInicio,
            HoraFin      = horaFin,
            Estado       = "pendiente",
            Observaciones = dto.Observaciones
        };

        await _repo.Agregar(turno);
        return (turno.Id, null);
    }

    public async Task<(bool ok, string? error)> Actualizar(int id, TurnoUpdateDto dto)
    {
        var turno = await _repo.ObtenerPorId(id);
        if (turno == null) return (false, "Turno no encontrado.");

        if (dto.FechaInicio.HasValue) turno.FechaInicio = dto.FechaInicio.Value;

        if (!string.IsNullOrEmpty(dto.HoraInicio))
        {
            if (!TimeSpan.TryParse(dto.HoraInicio, out var hi))
                return (false, "Formato de hora inicio inválido.");
            turno.HoraInicio = hi;
        }
        if (!string.IsNullOrEmpty(dto.HoraFin))
        {
            if (!TimeSpan.TryParse(dto.HoraFin, out var hf))
                return (false, "Formato de hora fin inválido.");
            turno.HoraFin = hf;
        }

        // Verificar conflicto si se modificó la fecha u hora
        if (dto.FechaInicio.HasValue || dto.HoraInicio != null || dto.HoraFin != null)
        {
            var conflicto = await _repo.HayConflictoHorario(turno.IdDoctor, turno.FechaInicio, turno.HoraInicio, turno.HoraFin, id);
            if (conflicto)
                return (false, "Conflicto de horario al reprogramar.");
        }

        if (!string.IsNullOrEmpty(dto.Estado))       turno.Estado        = dto.Estado;
        if (!string.IsNullOrEmpty(dto.Observaciones)) turno.Observaciones = dto.Observaciones;

        await _repo.Actualizar(turno);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> Cancelar(int id)
    {
        var turno = await _repo.ObtenerPorId(id);
        if (turno == null) return (false, "Turno no encontrado.");

        turno.Estado = "cancelado";
        await _repo.Eliminar(turno);
        return (true, null);
    }
}
