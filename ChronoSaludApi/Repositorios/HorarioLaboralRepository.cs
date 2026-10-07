using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class HorarioLaboralRepository : IHorarioLaboralRepository
{
    // Códigos de SQL Server: 1205 = elegido como víctima de un deadlock,
    // 1222 = se venció la espera de un bloqueo.
    private static readonly int[] ErroresDeBloqueo = [1205, 1222];

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

    // Serializable: mientras dura la transacción nadie puede sumarle un turno
    // a este doctor ni tocarle el horario, así que lo que se controló es lo
    // que se guarda. El borrado y el alta van en un único SaveChanges.
    public async Task<IReadOnlyList<Turno>> ReemplazarHorarios(
        int idDoctor,
        IReadOnlyList<HorarioLaboral> nuevos,
        DateTime desde,
        Func<IReadOnlyList<HorarioLaboral>, IReadOnlyList<Turno>, IReadOnlyList<Turno>> turnosQueFrenan)
    {
        try
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var actuales = await _db.HorariosLaborales
                .Where(h => h.IdDoctor == idDoctor)
                .ToListAsync();

            var turnos = await _db.Turnos
                .Include(t => t.Paciente).ThenInclude(p => p!.Usuario)
                .Where(t => t.IdDoctor == idDoctor && t.FechaInicio >= desde)
                .ToListAsync();

            var frenan = turnosQueFrenan(actuales, turnos);
            if (frenan.Count > 0)
            {
                await transaccion.RollbackAsync();
                return frenan;
            }

            _db.HorariosLaborales.RemoveRange(actuales);
            _db.HorariosLaborales.AddRange(nuevos);
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Array.Empty<Turno>();
        }
        catch (Exception error) when (EsBloqueo(error))
        {
            // La transacción ya se deshizo sola. Lo que quedó a medias en el
            // contexto se descarta para que no viaje en un guardado posterior.
            _db.ChangeTracker.Clear();
            throw new BaseOcupadaException(error);
        }
    }

    private static bool EsBloqueo(Exception? error)
    {
        for (; error is not null; error = error.InnerException)
        {
            if (error is SqlException sql && ErroresDeBloqueo.Contains(sql.Number))
                return true;
        }

        return false;
    }
}
