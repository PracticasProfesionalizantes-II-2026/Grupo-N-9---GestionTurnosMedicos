using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class TurnoRepository : ITurnoRepository
{
    private readonly AppDbContext _db;

    public TurnoRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Turno>> ObtenerTodos(int? pacienteId, int? doctorId, string? estado, DateTime? desde, DateTime? hasta)
    {
        var query = _db.Turnos
            .Include(t => t.Paciente).ThenInclude(p => p!.Usuario)
            .Include(t => t.Doctor).ThenInclude(d => d!.Usuario)
            .AsQueryable();

        if (pacienteId.HasValue) query = query.Where(t => t.IdPaciente == pacienteId);
        if (doctorId.HasValue)   query = query.Where(t => t.IdDoctor == doctorId);
        if (!string.IsNullOrEmpty(estado)) query = query.Where(t => t.Estado == estado);
        if (desde.HasValue)      query = query.Where(t => t.FechaInicio >= desde);
        if (hasta.HasValue)      query = query.Where(t => t.FechaInicio <= hasta);

        return await query.ToListAsync();
    }

    public async Task<Turno?> ObtenerPorId(int id)
        => await _db.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<bool> HayConflictoHorario(int doctorId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, int? turnoId = null)
        => await _db.Turnos.AnyAsync(t =>
            t.IdDoctor == doctorId &&
            t.FechaInicio.Date == fecha.Date &&
            t.Estado != "cancelado" &&
            (turnoId == null || t.Id != turnoId) &&
            t.HoraInicio < horaFin &&
            t.HoraFin > horaInicio);

    public async Task Agregar(Turno turno)
    {
        _db.Turnos.Add(turno);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Turno turno)
    {
        _db.Turnos.Update(turno);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Turno turno)
    {
        _db.Turnos.Update(turno);
        await _db.SaveChangesAsync();
    }
}
