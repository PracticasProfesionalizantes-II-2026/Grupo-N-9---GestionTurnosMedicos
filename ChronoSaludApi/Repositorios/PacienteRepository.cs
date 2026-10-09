using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class PacienteRepository : IPacienteRepository
{
    private readonly AppDbContext _db;

    public PacienteRepository(AppDbContext db) => _db = db;

    // Una página del listado. El total, el orden y la página los resuelve
    // SQL Server: de la base viajan solo los pacientes que se muestran.
    // Collation de SQL Server que no distingue mayúsculas ni acentos: así
    // "gomez" encuentra a "Gómez". La de la base sí distingue acentos.
    private const string SinAcentos = "Latin1_General_CI_AI";

    public async Task<(int total, List<Paciente> pacientes)> Buscar(
        string? buscar, string? nombre, string? dni, int? coberturaId, int pagina, int limite)
    {
        // Solo pacientes con la cuenta activa. La ficha por id sigue
        // accesible, para no perder la historia clínica de una baja.
        var query = _db.Pacientes
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Where(p => p.Usuario!.Activo);

        // Cada palabra tiene que aparecer en el nombre, el apellido, el email
        // o el DNI: así "ana duarte" encuentra a Ana Duarte.
        foreach (var palabra in PalabrasDeBusqueda.Separar(buscar))
        {
            query = query.Where(p =>
                EF.Functions.Collate(p.Usuario!.Nombre, SinAcentos).Contains(palabra) ||
                EF.Functions.Collate(p.Usuario!.Apellido, SinAcentos).Contains(palabra) ||
                EF.Functions.Collate(p.Usuario!.Email, SinAcentos).Contains(palabra) ||
                (p.Dni != null && p.Dni.Contains(palabra)));
        }

        if (!string.IsNullOrEmpty(nombre))
            query = query.Where(p =>
                p.Usuario!.Nombre.Contains(nombre) ||
                p.Usuario!.Apellido.Contains(nombre));

        if (!string.IsNullOrEmpty(dni))
            query = query.Where(p => p.Dni != null && p.Dni.Contains(dni));

        if (coberturaId.HasValue)
            query = query.Where(p =>
                p.PacienteCoberturas.Any(pc => pc.IdCobertura == coberturaId));

        var total = await query.CountAsync();

        var pacientes = await query
            .OrderBy(p => p.Usuario!.Apellido)
            .ThenBy(p => p.Usuario!.Nombre)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (total, pacientes);
    }

    public async Task<Paciente?> ObtenerPorId(int id)
        => await _db.Pacientes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Paciente?> ObtenerPorIdUsuario(int idUsuario)
        => await _db.Pacientes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);

    public async Task Agregar(Paciente paciente)
    {
        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Paciente paciente)
    {
        _db.Pacientes.Update(paciente);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExisteDniEnOtroPaciente(string dni, int idPacienteExcluir)
        => await _db.Pacientes.AnyAsync(p => p.Dni == dni && p.Id != idPacienteExcluir);
}
