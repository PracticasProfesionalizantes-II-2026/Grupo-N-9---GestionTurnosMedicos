using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class CoberturaLogica : ICoberturaLogica
{
    private readonly ICoberturaRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;

    public CoberturaLogica(ICoberturaRepository repo, IPacienteRepository pacienteRepo)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task<IEnumerable<CoberturaDto>> ObtenerTodas(string? nombre)
    {
        var coberturas = await _repo.ObtenerTodas(nombre);
        return coberturas.Select(c => new CoberturaDto(c.Id, c.Nombre, c.Plan));
    }

    public async Task<(IEnumerable<PacienteCoberturaDto> coberturas, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (Enumerable.Empty<PacienteCoberturaDto>(), "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != pacienteId)
                return (Enumerable.Empty<PacienteCoberturaDto>(), null, true);
        }

        var pcs = await _repo.ObtenerCoberturasDePaciente(pacienteId);
        var coberturas = pcs.Select(pc => new PacienteCoberturaDto(
            pc.Id,
            pc.IdCobertura,
            pc.Cobertura?.Nombre ?? string.Empty,
            pc.IdAfiliado,
            pc.Plan
        ));
        return (coberturas, null, false);
    }

    public async Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> AsociarAPaciente(
        int pacienteId, AsociarCoberturaDto dto, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            // "No es staff" no implica "es el paciente": acá el doctor
            // tampoco es staff, y nunca va a tener fila en Paciente. Sin este
            // chequeo, la búsqueda de abajo le daría el 404 de "sin perfil de
            // paciente" en vez del 403 que le corresponde por no ser dueño ni staff.
            if (!callerEsPaciente)
                return (false, null, false, true);

            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (false, "Tu usuario no tiene un perfil de paciente asociado.", true, false);
            if (propio.Id != pacienteId)
                return (false, null, false, true);
        }

        var existente = await _repo.ObtenerPacienteCobertura(pacienteId, dto.IdCobertura);
        if (existente != null)
            return (false, "La cobertura ya está asociada a este paciente.", false, false);

        var cobertura = await _repo.ObtenerPorId(dto.IdCobertura);
        if (cobertura == null)
            return (false, "Cobertura no encontrada.", false, false);

        var pc = new PacienteCobertura
        {
            IdPaciente  = pacienteId,
            IdCobertura = dto.IdCobertura,
            IdAfiliado  = dto.IdAfiliado,
            Plan        = dto.Plan
        };

        await _repo.AgregarPacienteCobertura(pc);
        return (true, null, false, false);
    }

    public async Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> ActualizarDePaciente(
        int pacienteId, AsociarCoberturaDto dto, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            if (!callerEsPaciente)
                return (false, null, false, true);

            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (false, "Tu usuario no tiene un perfil de paciente asociado.", true, false);
            if (propio.Id != pacienteId)
                return (false, null, false, true);
        }

        var pc = await _repo.ObtenerPacienteCobertura(pacienteId, dto.IdCobertura);
        if (pc == null)
            return (false, "Asociación no encontrada.", false, false);

        pc.IdAfiliado = dto.IdAfiliado;
        pc.Plan = dto.Plan;

        await _repo.ActualizarPacienteCobertura(pc);
        return (true, null, false, false);
    }

    public async Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> DesvincularDePaciente(
        int pacienteId, int coberturaId, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            if (!callerEsPaciente)
                return (false, null, false, true);

            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (false, "Tu usuario no tiene un perfil de paciente asociado.", true, false);
            if (propio.Id != pacienteId)
                return (false, null, false, true);
        }

        var pc = await _repo.ObtenerPacienteCobertura(pacienteId, coberturaId);
        if (pc == null)
            return (false, "Asociación no encontrada.", false, false);

        await _repo.EliminarPacienteCobertura(pc);
        return (true, null, false, false);
    }
}
