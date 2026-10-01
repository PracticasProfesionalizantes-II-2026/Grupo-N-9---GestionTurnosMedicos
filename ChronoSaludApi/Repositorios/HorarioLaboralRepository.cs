using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class HorarioLaboralRepository : IHorarioLaboralRepository
{
    private readonly AppDbContext _db;

    public HorarioLaboralRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<HorarioLaboral>> ObtenerPorDoctor(int idDoctor)
        => await _db.HorariosLaborales
            .Where(h => h.IdDoctor == idDoctor)
            .OrderBy(h => h.DiaSemana)
            .ToListAsync();

    public async Task<HorarioLaboral?> ObtenerPorDoctorYDia(int idDoctor, int diaSemana)
        => await _db.HorariosLaborales
            .FirstOrDefaultAsync(h => h.IdDoctor == idDoctor && h.DiaSemana == diaSemana);

    // Borra el horario actual y carga el nuevo en un único SaveChanges,
    // así el reemplazo queda atómico.
    public async Task ReemplazarHorarios(int idDoctor, IEnumerable<HorarioLaboral> nuevos)
    {
        var actuales = await _db.HorariosLaborales.Where(h => h.IdDoctor == idDoctor).ToListAsync();
        _db.HorariosLaborales.RemoveRange(actuales);
        _db.HorariosLaborales.AddRange(nuevos);
        await _db.SaveChangesAsync();
    }
}
