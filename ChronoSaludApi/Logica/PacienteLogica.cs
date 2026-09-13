using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class PacienteLogica : IPacienteLogica
{
    private readonly IPacienteRepository _repo;

    public PacienteLogica(IPacienteRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<PacienteListaDto> pacientes)> ObtenerTodos(
        string? nombre, int? coberturaId, int pagina, int limite)
    {
        var todos = await _repo.ObtenerTodos(nombre, coberturaId);
        var total = todos.Count();
        var resultado = todos
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(p => new PacienteListaDto(
                p.Id,
                p.Usuario?.Nombre ?? string.Empty,
                p.Usuario?.Apellido ?? string.Empty
            ));
        return (total, resultado);
    }

    public async Task<(PacienteDto? paciente, string? error, bool prohibido)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _repo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (null, "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != id)
                return (null, null, true);
        }

        var paciente = Mapear(await _repo.ObtenerPorId(id));
        return paciente == null
            ? (null, "Paciente no encontrado.", false)
            : (paciente, null, false);
    }

    public async Task<PacienteDto?> ObtenerPorIdUsuario(int idUsuario)
        => Mapear(await _repo.ObtenerPorIdUsuario(idUsuario));

    private static PacienteDto? Mapear(Entidades.Paciente? p)
    {
        if (p == null) return null;

        return new PacienteDto(
            p.Id,
            p.Usuario?.Nombre ?? string.Empty,
            p.Usuario?.Apellido ?? string.Empty,
            p.FechaNacimiento,
            p.Sexo,
            p.GrupoSanguineo,
            p.Alergias,
            p.Condiciones
        );
    }

    public async Task<(bool ok, string? error, bool prohibido)> Actualizar(
        int id, PacienteUpdateDto dto, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _repo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (false, "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != id)
                return (false, null, true);
        }

        var paciente = await _repo.ObtenerPorId(id);
        if (paciente == null) return (false, "Paciente no encontrado.", false);

        if (dto.FechaNacimiento.HasValue) paciente.FechaNacimiento = dto.FechaNacimiento;
        if (!string.IsNullOrEmpty(dto.Sexo))           paciente.Sexo          = dto.Sexo;
        if (!string.IsNullOrEmpty(dto.GrupoSanguineo)) paciente.GrupoSanguineo = dto.GrupoSanguineo;
        if (!string.IsNullOrEmpty(dto.Alergias))       paciente.Alergias      = dto.Alergias;
        if (!string.IsNullOrEmpty(dto.Condiciones))    paciente.Condiciones   = dto.Condiciones;

        await _repo.Actualizar(paciente);
        return (true, null, false);
    }
}
