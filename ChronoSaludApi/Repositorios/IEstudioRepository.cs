using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IEstudioRepository
{
    Task<IEnumerable<Estudio>> ObtenerDePaciente(int pacienteId, string? tipo, string? estado, DateTime? desde);
    Task<Estudio?> ObtenerPorId(int id);

    /// <summary>Para leer: trae también el turno con su doctor, para saber quién lo pidió.</summary>
    Task<Estudio?> ObtenerConDoctor(int id);
    Task Agregar(Estudio estudio);
    Task Actualizar(Estudio estudio);
}
