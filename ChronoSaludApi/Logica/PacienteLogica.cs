using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class PacienteLogica : IPacienteLogica
{
    private readonly IPacienteRepository _repo;

    public PacienteLogica(IPacienteRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<PacienteListaDto> pacientes)> ObtenerTodos(
        string? nombre, string? dni, int? coberturaId, int pagina, int limite)
    {
        var todos = await _repo.ObtenerTodos(nombre, dni, coberturaId);
        var total = todos.Count();
        var resultado = todos
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(p => new PacienteListaDto(
                p.Id,
                p.IdUsuario,
                p.Usuario?.Nombre ?? string.Empty,
                p.Usuario?.Apellido ?? string.Empty,
                p.Usuario?.Email ?? string.Empty,
                p.Usuario?.Telefono,
                p.Dni
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
            p.IdUsuario,
            p.Usuario?.Nombre ?? string.Empty,
            p.Usuario?.Apellido ?? string.Empty,
            p.Usuario?.Email ?? string.Empty,
            p.Usuario?.Telefono,
            p.FechaNacimiento,
            p.Sexo,
            p.GrupoSanguineo,
            p.Alergias,
            p.Condiciones,
            p.Dni,
            p.Direccion,
            p.Nacionalidad,
            p.EstadoCivil,
            p.FotoUrl
        );
    }

    public async Task<(bool ok, string? error, bool prohibido, bool conflictoDni)> Actualizar(
        int id, PacienteUpdateDto dto, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _repo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (false, "Tu usuario no tiene un perfil de paciente asociado.", false, false);
            if (propio.Id != id)
                return (false, null, true, false);
        }

        var paciente = await _repo.ObtenerPorId(id);
        if (paciente == null) return (false, "Paciente no encontrado.", false, false);

        if (dto.FechaNacimiento.HasValue) paciente.FechaNacimiento = dto.FechaNacimiento;
        if (!string.IsNullOrEmpty(dto.Sexo))           paciente.Sexo          = dto.Sexo;
        if (!string.IsNullOrEmpty(dto.GrupoSanguineo)) paciente.GrupoSanguineo = dto.GrupoSanguineo;
        if (!string.IsNullOrEmpty(dto.Alergias))       paciente.Alergias      = dto.Alergias;
        if (!string.IsNullOrEmpty(dto.Condiciones))    paciente.Condiciones   = dto.Condiciones;

        if (dto.Dni != null)
        {
            var nuevoDni = string.IsNullOrEmpty(dto.Dni) ? null : dto.Dni;
            if (nuevoDni != paciente.Dni)
            {
                if (nuevoDni != null && await _repo.ExisteDniEnOtroPaciente(nuevoDni, id))
                    return (false, "Ya existe un paciente registrado con ese DNI.", false, true);
                paciente.Dni = nuevoDni;
            }
        }
        if (!string.IsNullOrEmpty(dto.Direccion))    paciente.Direccion    = dto.Direccion;
        if (!string.IsNullOrEmpty(dto.Nacionalidad)) paciente.Nacionalidad = dto.Nacionalidad;
        if (!string.IsNullOrEmpty(dto.EstadoCivil))  paciente.EstadoCivil  = dto.EstadoCivil;
        if (!string.IsNullOrEmpty(dto.FotoUrl))      paciente.FotoUrl      = dto.FotoUrl;

        await _repo.Actualizar(paciente);
        return (true, null, false, false);
    }
}
