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

    /// <summary>
    /// Una página del listado de turnos. El filtro, el orden, el total y la
    /// página los resuelve SQL Server: de la base viajan solo las filas que
    /// se muestran.
    /// <paramref name="orden"/> es "paciente", "estado" o, cualquier otro
    /// valor, "fecha" (día y hora).
    /// </summary>
    public async Task<(int total, List<Turno> turnos)> Buscar(
        FiltroTurnos filtro, string orden, bool descendente, int pagina, int limite)
    {
        // El listado muestra los nombres del paciente y del doctor: se traen
        // sus usuarios en la misma consulta.
        IQueryable<Turno> query = Filtrar(filtro, usarEstados: true)
            .Include(t => t.Paciente).ThenInclude(p => p!.Usuario)
            .Include(t => t.Doctor).ThenInclude(d => d!.Usuario);

        var total = await query.CountAsync();

        IOrderedQueryable<Turno> ordenada;
        switch (orden)
        {
            case "paciente":
                // Nombre y después apellido: es como se muestra ("Ana Duarte").
                ordenada = descendente
                    ? query.OrderByDescending(t => t.Paciente!.Usuario!.Nombre).ThenByDescending(t => t.Paciente!.Usuario!.Apellido)
                    : query.OrderBy(t => t.Paciente!.Usuario!.Nombre).ThenBy(t => t.Paciente!.Usuario!.Apellido);
                // Si dos turnos son del mismo paciente, primero el más temprano.
                ordenada = ordenada.ThenBy(t => t.FechaInicio).ThenBy(t => t.HoraInicio);
                break;

            case "estado":
                ordenada = descendente
                    ? query.OrderByDescending(t => t.Estado)
                    : query.OrderBy(t => t.Estado);
                ordenada = ordenada.ThenBy(t => t.FechaInicio).ThenBy(t => t.HoraInicio);
                break;

            default:
                // "fecha": por día y, dentro del día, por hora.
                ordenada = descendente
                    ? query.OrderByDescending(t => t.FechaInicio).ThenByDescending(t => t.HoraInicio)
                    : query.OrderBy(t => t.FechaInicio).ThenBy(t => t.HoraInicio);
                break;
        }

        // El último criterio es siempre el Id: así dos turnos empatados salen
        // en el mismo orden en cada carga y ninguno se repite entre páginas.
        var turnos = await ordenada
            .ThenBy(t => t.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (total, turnos);
    }

    /// <summary>
    /// Cuántos turnos hay de cada estado con ese filtro. Ignora
    /// filtro.Estados a propósito: las tarjetas del listado muestran los
    /// cuatro números aunque la tabla esté filtrada por uno.
    /// </summary>
    public async Task<Dictionary<string, int>> ContarPorEstado(FiltroTurnos filtro)
    {
        var query = Filtrar(filtro, usarEstados: false);
        var conteos = new Dictionary<string, int>();

        // Un COUNT por estado: cuatro consultas cortas.
        foreach (var estado in FiltroTurnos.EstadosConocidos)
        {
            conteos[estado] = await query.CountAsync(t => t.Estado == estado);
        }

        return conteos;
    }

    /// <summary>
    /// Cuántos pacientes distintos tuvieron al menos un turno completado en
    /// el período. Cuenta SQL Server (COUNT DISTINCT).
    /// </summary>
    public async Task<int> ContarPacientesAtendidos(DateTime desde, DateTime hasta)
        => await _db.Turnos
            .Where(t => t.Estado == "completado" && t.FechaInicio >= desde && t.FechaInicio <= hasta)
            .Select(t => t.IdPaciente)
            .Distinct()
            .CountAsync();

    /// <summary>
    /// Por doctor, cuántos turnos no cancelados tiene en el período. La clave
    /// del diccionario es el IdDoctor. Un doctor sin turnos no aparece.
    /// </summary>
    public async Task<Dictionary<int, int>> ContarOcupadosPorDoctor(int? doctorId, DateTime desde, DateTime hasta)
    {
        var filtro = new FiltroTurnos { DoctorId = doctorId, Desde = desde, Hasta = hasta };

        // GroupBy agrupa los turnos por doctor y Count los cuenta: SQL Server
        // devuelve una fila por doctor, no los turnos.
        var filas = await Filtrar(filtro, usarEstados: false)
            .Where(t => t.Estado != "cancelado")
            .GroupBy(t => t.IdDoctor)
            .Select(grupo => new { IdDoctor = grupo.Key, Cantidad = grupo.Count() })
            .ToListAsync();

        var resultado = new Dictionary<int, int>();
        foreach (var fila in filas)
        {
            resultado[fila.IdDoctor] = fila.Cantidad;
        }

        return resultado;
    }

    /// <summary>
    /// La consulta con los filtros aplicados, todavía sin ejecutar: la usan
    /// el listado y los conteos. AsNoTracking porque es solo para leer.
    /// </summary>
    private IQueryable<Turno> Filtrar(FiltroTurnos filtro, bool usarEstados)
    {
        var query = _db.Turnos.AsNoTracking();

        if (filtro.PacienteId.HasValue) query = query.Where(t => t.IdPaciente == filtro.PacienteId);
        if (filtro.DoctorId.HasValue)   query = query.Where(t => t.IdDoctor == filtro.DoctorId);
        if (filtro.Desde.HasValue)      query = query.Where(t => t.FechaInicio >= filtro.Desde);
        if (filtro.Hasta.HasValue)      query = query.Where(t => t.FechaInicio <= filtro.Hasta);

        if (usarEstados && filtro.Estados.Count > 0)
        {
            var estados = filtro.Estados;
            query = query.Where(t => estados.Contains(t.Estado));
        }

        return query;
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
