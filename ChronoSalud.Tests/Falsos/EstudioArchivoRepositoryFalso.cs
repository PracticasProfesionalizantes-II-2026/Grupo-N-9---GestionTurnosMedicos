using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a EstudioArchivoRepository: los PDF viven en un diccionario.</summary>
public class EstudioArchivoRepositoryFalso : IEstudioArchivoRepository
{
    public Dictionary<int, EstudioArchivo> Archivos { get; } = new Dictionary<int, EstudioArchivo>();

    public Task<EstudioArchivo?> Obtener(int idEstudio)
        => Task.FromResult(Archivos.GetValueOrDefault(idEstudio));

    public Task<Dictionary<int, string>> NombresDe(IEnumerable<int> idsEstudio)
    {
        var nombres = idsEstudio.Distinct()
            .Where(Archivos.ContainsKey)
            .ToDictionary(id => id, id => Archivos[id].Nombre);
        return Task.FromResult(nombres);
    }

    public Task Guardar(EstudioArchivo archivo)
    {
        Archivos[archivo.IdEstudio] = archivo;
        return Task.CompletedTask;
    }

    public Task Eliminar(int idEstudio)
    {
        Archivos.Remove(idEstudio);
        return Task.CompletedTask;
    }
}
