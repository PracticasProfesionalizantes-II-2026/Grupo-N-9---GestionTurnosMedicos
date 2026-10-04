using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IUsuarioFotoRepository
{
    Task<UsuarioFoto?> ObtenerPorIdUsuario(int idUsuario);
    Task<DateTime?> ObtenerFechaActualizacion(int idUsuario);
    Task Guardar(UsuarioFoto foto);
    Task Eliminar(int idUsuario);
    Task<FotoAcceso?> ObtenerAcceso(int idUsuario);
    Task<IEnumerable<FotoDueno>> ObtenerDePacientes(IEnumerable<int> idsPaciente);
    Task<IEnumerable<FotoDueno>> ObtenerDeDoctores(IEnumerable<int> idsDoctor);
    Task<IEnumerable<FotoDueno>> ObtenerDeTurnos(IEnumerable<int> idsTurno);
}

// Lo que hace falta para decidir quién puede ver una foto, sin traer su contenido.
public record FotoAcceso(string RolDueno, DateTime ActualizadaEn);

// Una foto existente vista desde un paciente, un doctor o un turno: Id es el
// id por el que se preguntó e IdUsuario el dueño de la foto.
public record FotoDueno(int Id, int IdUsuario, string RolDueno);
