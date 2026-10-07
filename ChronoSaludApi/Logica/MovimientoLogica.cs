using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class MovimientoLogica : IMovimientoLogica
{
    private const int LimiteMaximo = 100;

    private readonly IMovimientoRepository _repo;
    private readonly IDoctorRepository _doctorRepo;

    public MovimientoLogica(IMovimientoRepository repo, IDoctorRepository doctorRepo)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
    }

    public async Task<(int total, IEnumerable<MovimientoDto> movimientos, string? error, bool prohibido)> Buscar(
        int? idDoctor, string? accion, DateTime? desde, DateTime? hasta, int pagina, int limite,
        Solicitante solicitante)
    {
        var sinFilas = Enumerable.Empty<MovimientoDto>();

        // El administrador ve todo. El doctor, solo lo de su agenda y su
        // horario: su IdDoctor sale del token y pisa el filtro que haya mandado.
        if (solicitante.EsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            if (doctor == null)
                return (0, sinFilas, "Tu usuario no tiene un perfil de doctor asociado.", false);

            idDoctor = doctor.Id;
        }
        else if (!solicitante.EsAdministrador)
        {
            return (0, sinFilas, null, true);
        }

        pagina = Math.Max(pagina, 1);
        limite = Math.Clamp(limite, 1, LimiteMaximo);

        // El filtro llega en días de Argentina y las filas están en UTC: el
        // día "hasta" entra completo, así que el tope es el comienzo del siguiente.
        DateTime? desdeUtc = desde.HasValue ? FechaArgentina.ComienzoDelDiaEnUtc(desde.Value) : null;
        DateTime? antesDeUtc = hasta.HasValue ? FechaArgentina.ComienzoDelDiaEnUtc(hasta.Value.AddDays(1)) : null;

        var (total, movimientos) = await _repo.Buscar(idDoctor, accion, desdeUtc, antesDeUtc, pagina, limite);

        // Quién fue (id y nombre) solo para el administrador. Al doctor le
        // llega el rol de quien actuó y nada más.
        var veQuienFue = solicitante.EsAdministrador;

        return (total, movimientos.Select(m => new MovimientoDto(
            m.Id,
            DateTime.SpecifyKind(m.FechaUtc, DateTimeKind.Utc),
            m.Accion,
            m.Entidad,
            m.IdEntidad,
            m.IdDoctor,
            m.Resumen,
            m.RolUsuario,
            veQuienFue ? m.IdUsuario : null,
            veQuienFue ? $"{m.Usuario?.Nombre} {m.Usuario?.Apellido}".Trim() : null
        )), null, false);
    }
}
