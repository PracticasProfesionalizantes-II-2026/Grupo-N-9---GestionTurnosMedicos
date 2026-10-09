using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// El buscador de pacientes (partial _ElegirPaciente). Reemplaza a los
/// desplegables que cargaban hasta 200 pacientes. Funciona sin JavaScript:
/// buscar es un formulario GET y cada resultado es un enlace que vuelve a la
/// misma pantalla con ?paciente=N.
/// </summary>
public class ElegirPacienteViewModel
{
    /// <summary>El paciente ya elegido. Null mientras se busca.</summary>
    public PacienteLista? Elegido { get; init; }

    public IReadOnlyList<PacienteLista> Resultados { get; init; } = Array.Empty<PacienteLista>();

    /// <summary>Cuántos pacientes coinciden en total (se muestran hasta 20).</summary>
    public int Total { get; init; }

    /// <summary>Lo que se escribió en el buscador.</summary>
    public string? Buscar { get; init; }

    // La pantalla a la que vuelven el buscador y los resultados.
    public string Controlador { get; init; } = string.Empty;
    public string Accion { get; init; } = string.Empty;

    /// <summary>
    /// Los otros datos de esa pantalla, para no perderlos al buscar (por
    /// ejemplo, el horario elegido en Turnos/Confirmar). Sin "paciente" ni
    /// "buscar".
    /// </summary>
    public IReadOnlyDictionary<string, string> Ruta { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Se muestra "Cambiar paciente". Desde un turno o al editar, el paciente
    /// no se cambia.
    /// </summary>
    public bool PuedeCambiar { get; init; } = true;

    public bool HayMas => Total > Resultados.Count;

    /// <summary>La ruta de un resultado: la de la pantalla más ese paciente.</summary>
    public IDictionary<string, string> RutaCon(int idPaciente)
    {
        var ruta = new Dictionary<string, string>(Ruta);
        ruta["paciente"] = idPaciente.ToString();
        return ruta;
    }

    /// <summary>La ruta de "Cambiar paciente": la de la pantalla, sin paciente.</summary>
    public IDictionary<string, string> RutaSinPaciente => new Dictionary<string, string>(Ruta);

    /// <summary>"Ana Duarte".</summary>
    public static string NombreDe(PacienteLista paciente)
    {
        var nombre = $"{paciente.Nombre} {paciente.Apellido}".Trim();
        return nombre.Length == 0 ? $"Paciente #{paciente.IdPaciente}" : nombre;
    }

    /// <summary>
    /// "DNI 30111222 · ana@mail.com", para distinguir a dos pacientes con el
    /// mismo nombre. Lo que falte se omite; null si no hay nada.
    /// </summary>
    public static string? DatosDe(PacienteLista paciente)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(paciente.Dni)) partes.Add($"DNI {paciente.Dni.Trim()}");
        if (!string.IsNullOrWhiteSpace(paciente.Email)) partes.Add(paciente.Email.Trim());

        return partes.Count == 0 ? null : string.Join(" · ", partes);
    }

    /// <summary>
    /// Arma el buscador para una pantalla. <paramref name="ruta"/> son los
    /// otros datos de la pantalla; "paciente" y "buscar" se descartan.
    /// </summary>
    public static ElegirPacienteViewModel Desde(
        SeleccionDePaciente seleccion,
        string controlador,
        string accion,
        string? buscar = null,
        IDictionary<string, string>? ruta = null,
        bool puedeCambiar = true)
    {
        var otros = new Dictionary<string, string>();
        if (ruta is not null)
        {
            foreach (var (clave, valor) in ruta)
            {
                if (clave == "paciente" || clave == "buscar") continue;
                otros[clave] = valor;
            }
        }

        return new ElegirPacienteViewModel
        {
            Elegido = seleccion.Elegido,
            Resultados = seleccion.Resultados,
            Total = seleccion.Total,
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim(),
            Controlador = controlador,
            Accion = accion,
            Ruta = otros,
            PuedeCambiar = puedeCambiar
        };
    }
}
