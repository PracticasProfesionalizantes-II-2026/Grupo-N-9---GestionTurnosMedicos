using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class RecetaLogica : IRecetaLogica
{
    private readonly IRecetaRepository    _repo;
    private readonly IMedicamentoRepository _medRepo;

    public RecetaLogica(IRecetaRepository repo, IMedicamentoRepository medRepo)
    {
        _repo    = repo;
        _medRepo = medRepo;
    }

    public async Task<IEnumerable<RecetaDto>> ObtenerDePaciente(int pacienteId)
    {
        var recetas = await _repo.ObtenerDePaciente(pacienteId);
        return recetas.Select(MapearDto);
    }

    public async Task<RecetaDto?> ObtenerPorId(int id)
    {
        var r = await _repo.ObtenerPorId(id);
        return r == null ? null : MapearDto(r);
    }

    public async Task<(int? id, string? error)> Crear(RecetaCreateDto dto)
    {
        if (dto.Medicamentos == null || !dto.Medicamentos.Any())
            return (null, "Debe especificar al menos un medicamento.");

        // Validar que existan todos los medicamentos
        foreach (var m in dto.Medicamentos)
        {
            var med = await _medRepo.ObtenerPorId(m.IdMedicamento);
            if (med == null)
                return (null, $"Medicamento con id {m.IdMedicamento} no encontrado.");
        }

        var receta = new Receta
        {
            IdPaciente = dto.IdPaciente,
            IdDoctor   = dto.IdDoctor,
            IdTurno    = dto.IdTurno,
            Fecha      = dto.Fecha,
            Vigencia   = dto.Vigencia,
            Detalles   = dto.Detalles,
            RecetaMedicamentos = dto.Medicamentos.Select(m => new RecetaMedicamento
            {
                IdMedicamento = m.IdMedicamento,
                Dosis         = m.Dosis,
                Frecuencia    = m.Frecuencia,
                Duracion      = m.Duracion,
                Indicaciones  = m.Indicaciones
            }).ToList()
        };

        await _repo.Agregar(receta);
        return (receta.Id, null);
    }

    public async Task<(bool ok, string? error)> Actualizar(int id, RecetaCreateDto dto)
    {
        var receta = await _repo.ObtenerPorId(id);
        if (receta == null) return (false, "Receta no encontrada.");

        if (dto.Medicamentos == null || !dto.Medicamentos.Any())
            return (false, "Debe especificar al menos un medicamento.");

        foreach (var m in dto.Medicamentos)
        {
            var med = await _medRepo.ObtenerPorId(m.IdMedicamento);
            if (med == null)
                return (false, $"Medicamento con id {m.IdMedicamento} no encontrado.");
        }

        receta.IdTurno   = dto.IdTurno;
        receta.Fecha     = dto.Fecha;
        receta.Vigencia  = dto.Vigencia;
        receta.Detalles  = dto.Detalles;
        receta.RecetaMedicamentos = dto.Medicamentos.Select(m => new RecetaMedicamento
        {
            IdReceta      = id,
            IdMedicamento = m.IdMedicamento,
            Dosis         = m.Dosis,
            Frecuencia    = m.Frecuencia,
            Duracion      = m.Duracion,
            Indicaciones  = m.Indicaciones
        }).ToList();

        await _repo.Actualizar(receta);
        return (true, null);
    }

    private static RecetaDto MapearDto(Receta r) => new RecetaDto(
        r.Id,
        r.Fecha,
        r.Vigencia,
        r.Detalles,
        r.RecetaMedicamentos.Select(rm => new RecetaMedicamentoDto(
            rm.IdMedicamento,
            rm.Dosis,
            rm.Frecuencia,
            rm.Duracion,
            rm.Indicaciones
        )).ToList()
    );
}
