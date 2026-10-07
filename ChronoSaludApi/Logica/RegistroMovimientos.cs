using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Escribe en el historial de movimientos. Se llama después de guardar la
/// operación principal, y nunca la hace fallar: si el registro no se puede
/// escribir queda en el log y la operación sigue dada por buena.
/// </summary>
public interface IRegistroMovimientos
{
    Task Registrar(Solicitante solicitante, string accion, string entidad, int idEntidad, int? idDoctor, string resumen);
}

public class RegistroMovimientos : IRegistroMovimientos
{
    private readonly IMovimientoRepository _repo;
    private readonly ILogger<RegistroMovimientos> _logger;

    public RegistroMovimientos(IMovimientoRepository repo, ILogger<RegistroMovimientos> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Registrar(
        Solicitante solicitante, string accion, string entidad, int idEntidad, int? idDoctor, string resumen)
    {
        try
        {
            await _repo.Agregar(new Movimiento
            {
                FechaUtc   = DateTime.UtcNow,
                IdUsuario  = solicitante.IdUsuario,
                RolUsuario = solicitante.Rol,
                Accion     = accion,
                Entidad    = entidad,
                IdEntidad  = idEntidad,
                IdDoctor   = idDoctor,
                Resumen    = Acortar(resumen)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo registrar el movimiento {Accion} sobre {Entidad} {IdEntidad}.", accion, entidad, idEntidad);
        }
    }

    private static string Acortar(string texto) =>
        texto.Length <= AccionesMovimiento.LargoMaximoDelResumen
            ? texto
            : texto[..(AccionesMovimiento.LargoMaximoDelResumen - 3)] + "...";
}
