using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza al historial de movimientos: anota las acciones en una lista.</summary>
public class RegistroMovimientosFalso : IRegistroMovimientos
{
    public List<string> Acciones { get; } = new List<string>();

    public Task Registrar(Solicitante solicitante, string accion, string entidad, int idEntidad, int? idDoctor, string resumen)
    {
        Acciones.Add(accion);
        return Task.CompletedTask;
    }
}
