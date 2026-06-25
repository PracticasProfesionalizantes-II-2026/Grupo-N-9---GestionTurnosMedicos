using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class HistorialClinicoLogica : IHistorialClinicoLogica
{
    private readonly IHistorialClinicoRepository _repo;

    public HistorialClinicoLogica(IHistorialClinicoRepository repo) => _repo = repo;

    public async Task<(int idPaciente, IEnumerable<HistorialClinicoDto> historiales)?> ObtenerDePaciente(
        int pacienteId, DateTime? desde, DateTime? hasta)
    {
        var historiales = await _repo.ObtenerDePaciente(pacienteId, desde, hasta);
        var dtos = historiales.Select(h => new HistorialClinicoDto(
            h.Id,
            h.Fecha,
            h.Descripcion,
            h.Diagnostico,
            h.IdTurno
        ));
        return (pacienteId, dtos);
    }

    public async Task<(bool ok, string? error)> Crear(int pacienteId, HistorialClinicoCreateDto dto)
    {
        var historial = new HistorialClinico
        {
            IdPaciente  = pacienteId,
            Fecha       = dto.Fecha,
            Descripcion = dto.Descripcion,
            Diagnostico = dto.Diagnostico,
            IdTurno     = dto.IdTurno
        };

        await _repo.Agregar(historial);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> Actualizar(int pacienteId, HistorialClinicoCreateDto dto)
    {
        var historiales = await _repo.ObtenerDePaciente(pacienteId, dto.Fecha, dto.Fecha);
        var historial = historiales.FirstOrDefault();

        if (historial == null)
        {
            // Si no existe entrada para esa fecha, crear una nueva
            return await Crear(pacienteId, dto);
        }

        historial.Descripcion = dto.Descripcion;
        historial.Diagnostico = dto.Diagnostico;
        historial.IdTurno     = dto.IdTurno;

        await _repo.Actualizar(historial);
        return (true, null);
    }
}
