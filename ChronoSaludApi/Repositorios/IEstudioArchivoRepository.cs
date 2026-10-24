using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IEstudioArchivoRepository
{
    Task<EstudioArchivo?> Obtener(int idEstudio);

    /// <summary>De esos estudios, cuáles tienen PDF y con qué nombre. Sin traer el contenido.</summary>
    Task<Dictionary<int, string>> NombresDe(IEnumerable<int> idsEstudio);

    /// <summary>Crea el PDF o reemplaza el que ya tenía el estudio.</summary>
    Task Guardar(EstudioArchivo archivo);

    Task Eliminar(int idEstudio);
}
