using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza a UsuarioRepository. Como en la base real, si el usuario trae su
/// Paciente cargado se guardan los dos: el paciente va a la lista del
/// PacienteRepositoryFalso que recibe.
/// </summary>
public class UsuarioRepositoryFalso : IUsuarioRepository
{
    private readonly PacienteRepositoryFalso _pacientes;

    public UsuarioRepositoryFalso(PacienteRepositoryFalso pacientes)
    {
        _pacientes = pacientes;
    }

    public List<Usuario> Usuarios { get; } = new List<Usuario>();

    /// <summary>Si es true, Agregar falla como cuando la base encuentra un dato repetido.</summary>
    public bool SimularDatoRepetido { get; set; }

    public Task<IEnumerable<Usuario>> ObtenerTodos()
    {
        return Task.FromResult<IEnumerable<Usuario>>(Usuarios);
    }

    public Task<Usuario?> ObtenerPorId(int id)
    {
        var usuario = Usuarios.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(usuario);
    }

    // En la prueba, el Doctor ya viene cargado en el usuario si hace falta.
    public Task<Usuario?> ObtenerConDoctor(int id)
    {
        var usuario = Usuarios.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(usuario);
    }

    public Task<Usuario?> ObtenerPorEmail(string email)
    {
        var usuario = Usuarios.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(usuario);
    }

    public Task<bool> HayAdministradorActivo()
    {
        var hay = Usuarios.Any(u => u.Rol == "administrador" && u.Activo);
        return Task.FromResult(hay);
    }

    public Task<int> ContarAdministradoresActivos()
    {
        var cuantos = Usuarios.Count(u => u.Rol == "administrador" && u.Activo);
        return Task.FromResult(cuantos);
    }

    public Task<bool> EstaActivo(int id)
    {
        var activo = Usuarios.Any(u => u.Id == id && u.Activo);
        return Task.FromResult(activo);
    }

    public Task Agregar(Usuario usuario)
    {
        if (SimularDatoRepetido)
            throw new DatoRepetidoException(new Exception("Simulado en la prueba."));

        usuario.Id = Usuarios.Count + 1;
        Usuarios.Add(usuario);

        if (usuario.Paciente != null)
        {
            usuario.Paciente.IdUsuario = usuario.Id;
            usuario.Paciente.Usuario = usuario;
            _pacientes.Agregar(usuario.Paciente);
        }

        return Task.CompletedTask;
    }

    public Task Actualizar(Usuario usuario) => Task.CompletedTask;

    // Los cambios ya quedaron en el objeto de la lista: no hay nada que guardar.
    public Task GuardarActivo(Usuario usuario) => Task.CompletedTask;

    public Task<(int total, IEnumerable<Usuario> usuarios)> Buscar(
        string? buscar, string? rol, int pagina, int limite, bool bajas = false)
    {
        IEnumerable<Usuario> todos = Usuarios;
        return Task.FromResult((Usuarios.Count, todos));
    }

    public Task<IEnumerable<int>> ObtenerIdsConFoto(IEnumerable<int> idsUsuario)
    {
        return Task.FromResult<IEnumerable<int>>(new List<int>());
    }
}
