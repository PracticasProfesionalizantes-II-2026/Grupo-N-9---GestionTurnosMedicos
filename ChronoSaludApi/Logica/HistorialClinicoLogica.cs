using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class HistorialClinicoLogica : IHistorialClinicoLogica
{
    private readonly IHistorialClinicoRepository _repo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IPacienteRepository _pacienteRepo;

    public HistorialClinicoLogica(IHistorialClinicoRepository repo, IDoctorRepository doctorRepo, IPacienteRepository pacienteRepo)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task<(int idPaciente, IEnumerable<HistorialClinicoDto> historiales, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, DateTime? desde, DateTime? hasta, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (pacienteId, Enumerable.Empty<HistorialClinicoDto>(), "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != pacienteId)
                return (pacienteId, Enumerable.Empty<HistorialClinicoDto>(), null, true);
        }

        var historiales = await _repo.ObtenerDePaciente(pacienteId, desde, hasta);
        var dtos = historiales.Select(h => new HistorialClinicoDto(
            h.Id,
            h.Fecha,
            h.IdDoctor,
            h.Descripcion,
            h.Diagnostico,
            h.IdTurno
        ));
        return (pacienteId, dtos, null, false);
    }

    public async Task<(bool ok, string? error)> Crear(int pacienteId, int idUsuarioDoctor, HistorialClinicoCreateDto dto)
    {
        // El autor sale del token, no del cuerpo: quien escribe la entrada es
        // el doctor autenticado y el cliente no puede decir que fue otro.
        var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioDoctor);
        if (doctor == null)
            return (false, "El usuario autenticado no tiene perfil de doctor.");

        var historial = new HistorialClinico
        {
            IdPaciente  = pacienteId,
            IdDoctor    = doctor.Id,
            Fecha       = dto.Fecha,
            Descripcion = dto.Descripcion,
            Diagnostico = dto.Diagnostico,
            IdTurno     = dto.IdTurno
        };

        await _repo.Agregar(historial);
        return (true, null);
    }

    public async Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(
        int pacienteId, int idHistorial, HistorialClinicoCreateDto dto, int idUsuarioDoctor)
    {
        var historial = await _repo.ObtenerPorId(idHistorial);

        // Si la entrada no existe, o existe pero es de otro paciente, es un 404:
        // no confirmamos que el id exista bajo otra historia clinica.
        if (historial == null || historial.IdPaciente != pacienteId)
            return (false, "Registro de historial clinico no encontrado.", false);

        // Solo el doctor autor puede modificar su propia entrada. A uno ajeno
        // se le responde lo mismo que a una entrada de otro paciente: no existe.
        var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioDoctor);
        if (doctor == null)
            return (false, "Tu usuario no tiene un perfil de doctor asociado.", true);
        if (doctor.Id != historial.IdDoctor)
            return (false, "Registro de historial clinico no encontrado.", false);

        historial.Fecha       = dto.Fecha;
        historial.Descripcion = dto.Descripcion;
        historial.Diagnostico = dto.Diagnostico;
        historial.IdTurno     = dto.IdTurno;
        // IdDoctor no se toca: es quien escribio la entrada originalmente.

        await _repo.Actualizar(historial);
        return (true, null, false);
    }
}
