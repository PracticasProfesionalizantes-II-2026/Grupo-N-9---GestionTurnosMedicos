using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class CoberturaLogica : ICoberturaLogica
{
    private readonly ICoberturaRepository _repo;

    public CoberturaLogica(ICoberturaRepository repo) => _repo = repo;

    public async Task<IEnumerable<CoberturaDto>> ObtenerTodas(string? nombre)
    {
        var coberturas = await _repo.ObtenerTodas(nombre);
        return coberturas.Select(c => new CoberturaDto(c.Id, c.Nombre, c.Plan));
    }

    public async Task<IEnumerable<PacienteCoberturaDto>> ObtenerDePaciente(int pacienteId)
    {
        var pcs = await _repo.ObtenerCoberturasDePaciente(pacienteId);
        return pcs.Select(pc => new PacienteCoberturaDto(
            pc.Id,
            pc.IdCobertura,
            pc.Cobertura?.Nombre ?? string.Empty,
            pc.IdAfiliado,
            pc.Plan
        ));
    }

    public async Task<(bool ok, string? error)> AsociarAPaciente(int pacienteId, AsociarCoberturaDto dto)
    {
        var existente = await _repo.ObtenerPacienteCobertura(pacienteId, dto.IdCobertura);
        if (existente != null)
            return (false, "La cobertura ya está asociada a este paciente.");

        var cobertura = await _repo.ObtenerPorId(dto.IdCobertura);
        if (cobertura == null)
            return (false, "Cobertura no encontrada.");

        var pc = new PacienteCobertura
        {
            IdPaciente  = pacienteId,
            IdCobertura = dto.IdCobertura,
            IdAfiliado  = dto.IdAfiliado,
            Plan        = dto.Plan
        };

        await _repo.AgregarPacienteCobertura(pc);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> ActualizarDePaciente(int pacienteId, AsociarCoberturaDto dto)
    {
        var pc = await _repo.ObtenerPacienteCobertura(pacienteId, dto.IdCobertura);
        if (pc == null)
            return (false, "Asociación no encontrada.");

        pc.IdAfiliado = dto.IdAfiliado;
        pc.Plan = dto.Plan;

        await _repo.ActualizarPacienteCobertura(pc);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DesvincularDePaciente(int pacienteId, int coberturaId)
    {
        var pc = await _repo.ObtenerPacienteCobertura(pacienteId, coberturaId);
        if (pc == null)
            return (false, "Asociación no encontrada.");

        await _repo.EliminarPacienteCobertura(pc);
        return (true, null);
    }
}
