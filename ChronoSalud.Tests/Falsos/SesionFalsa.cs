using Microsoft.AspNetCore.Http;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza a la sesión de ASP.NET en las pruebas de la Web: guarda los
/// valores en un diccionario en memoria.
/// </summary>
public class SesionFalsa : ISession
{
    private readonly Dictionary<string, byte[]> _valores = new Dictionary<string, byte[]>();

    public bool IsAvailable => true;
    public string Id => "sesion-de-prueba";
    public IEnumerable<string> Keys => _valores.Keys;

    public void Clear() => _valores.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _valores.Remove(key);
    public void Set(string key, byte[] value) => _valores[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_valores.TryGetValue(key, out var guardado))
        {
            value = guardado;
            return true;
        }

        value = Array.Empty<byte>();
        return false;
    }
}
