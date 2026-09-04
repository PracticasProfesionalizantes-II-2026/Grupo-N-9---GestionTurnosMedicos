using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class TurnoLogica : ITurnoLogica
{
    private readonly ITurnoRepository _repo;

    public TurnoLogica(ITurnoRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<TurnoListaDto> turnos)> ObtenerTodos(
        int? pacienteId, int? doctorId, string? estado,
        DateTime? desde, DateTime? hasta, int pagina, int limite)
    {
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
        return (total, resultado);
    }

    public async Task<TurnoDto?> ObtenerPorId(int id)
    {
        var t = await _repo.ObtenerPorId(id);
        if (t == null) return null;

        return new TurnoDto(
            t.Id,
            t.FechaInicio,
            t.HoraInicio.ToString(@"hh\:mm"),
            t.HoraFin.ToString(@"hh\:mm"),
            t.Estado,
            t.IdPaciente,
            t.IdDoctor,
            t.Observaciones
        );
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
