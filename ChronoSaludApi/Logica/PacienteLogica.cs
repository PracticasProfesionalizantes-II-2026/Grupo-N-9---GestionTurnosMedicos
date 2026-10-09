using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class PacienteLogica : IPacienteLogica
{
    private readonly IPacienteRepository _repo;

    public PacienteLogica(IPacienteRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<PacienteListaDto> pacientes)> ObtenerTodos(
        string? buscar, string? nombre, string? dni, int? coberturaId, int pagina, int limite)
    {
        var (total, pacientes) = await _repo.Buscar(buscar, nombre, dni, coberturaId, pagina, limite);
        var resultado = pacientes
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
            p.FotoUrl,
            p.TipoDocumento,
            p.Provincia,
            p.Localidad,
            p.CodigoPostal,
            p.ContactoEmergenciaNombre,
            p.ContactoEmergenciaTelefono
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

        // 1. Los datos que vinieron con valor (todos menos el DNI).
        FichaPaciente.CopiarDatos(dto, paciente);

        // 2. El DNI. Un texto vacío lo borra; null no lo toca.
        if (dto.Dni != null)
        {
            var nuevoDni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();
            if (nuevoDni != paciente.Dni)
            {
                // El paciente carga su DNI una sola vez: después lo corrige la
                // administración, que puede verificarlo.
                if (!callerEsStaff && paciente.Dni != null)
                    return (false, "Tu DNI ya está cargado. Si hay que corregirlo, pedíselo a la administración.", false, false);

                if (nuevoDni != null && await _repo.ExisteDniEnOtroPaciente(nuevoDni, id))
                    return (false, "Ya existe un paciente registrado con ese DNI.", false, true);

                paciente.Dni = nuevoDni;
            }
        }

        // 3. Los datos que se pidieron borrar.
        var errorAlBorrar = FichaPaciente.BorrarCampos(dto.Borrar, paciente, callerEsStaff);
        if (errorAlBorrar != null) return (false, errorAlBorrar, false, false);

        await _repo.Actualizar(paciente);
        return (true, null, false, false);
    }
}
