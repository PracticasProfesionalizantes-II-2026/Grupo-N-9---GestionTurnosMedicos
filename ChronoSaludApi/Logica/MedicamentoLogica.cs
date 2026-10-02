using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class MedicamentoLogica : IMedicamentoLogica
{
    private readonly IMedicamentoRepository _repo;

    public MedicamentoLogica(IMedicamentoRepository repo) => _repo = repo;

    public async Task<IEnumerable<MedicamentoDto>> ObtenerTodos()
    {
        var meds = await _repo.ObtenerTodos();
        return meds.Select(MapearDto);
    }

    public async Task<MedicamentoDto?> ObtenerPorId(int id)
    {
        var m = await _repo.ObtenerPorId(id);
        return m == null ? null : MapearDto(m);
    }

    public async Task<int> Crear(MedicamentoCreateDto dto)
    {
        var med = new Medicamento
        {
            Nombre            = dto.Nombre,
            Descripcion       = dto.Descripcion,
            NombreGenerico    = dto.NombreGenerico,
            Concentracion     = dto.Concentracion,
            FormaFarmaceutica = dto.FormaFarmaceutica,
            Laboratorio       = dto.Laboratorio
        };
        await _repo.Agregar(med);
        return med.Id;
    }

    public async Task<bool> Actualizar(int id, MedicamentoCreateDto dto)
    {
        var med = await _repo.ObtenerPorId(id);
        if (med == null) return false;

        med.Nombre      = dto.Nombre;
        med.Descripcion = dto.Descripcion;
        med.NombreGenerico    = dto.NombreGenerico;
        med.Concentracion     = dto.Concentracion;
        med.FormaFarmaceutica = dto.FormaFarmaceutica;
        med.Laboratorio       = dto.Laboratorio;
        await _repo.Actualizar(med);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var med = await _repo.ObtenerPorId(id);
        if (med == null) return false;
        await _repo.Eliminar(med);
        return true;
    }

    private static MedicamentoDto MapearDto(Medicamento m) => new MedicamentoDto(
        m.Id,
        m.Nombre,
        m.Descripcion,
        m.NombreGenerico,
        m.Concentracion,
        m.FormaFarmaceutica,
        m.Laboratorio
    );
}
