using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Quién firmó una receta o escribió una entrada de la historia clínica,
/// listo para mostrar.
/// </summary>
public class FirmaViewModel
{
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Especialidad { get; init; }
    public string? Matricula { get; init; }

    /// <summary>"Matrícula MN 1234", o null si no hay matrícula.</summary>
    public string? MatriculaMostrada =>
        string.IsNullOrWhiteSpace(Matricula) ? null : $"Matrícula {Matricula.Trim()}";

    /// <summary>
    /// Especialidad y matrícula en una línea: "Clínica médica · Matrícula MN 1234".
    /// Lo que falte se omite.
    /// </summary>
    public string? Detalle
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(Especialidad)) partes.Add(Especialidad.Trim());
            if (MatriculaMostrada is not null) partes.Add(MatriculaMostrada);

            return partes.Count == 0 ? null : string.Join(" · ", partes);
        }
    }

    /// <summary>Todo en una línea: "Laura Méndez · Clínica médica · Matrícula MN 1234".</summary>
    public string Linea => Detalle is null ? NombreCompleto : $"{NombreCompleto} · {Detalle}";

    /// <summary>Null si la API no mandó el doctor (por ejemplo, una versión anterior).</summary>
    public static FirmaViewModel? Desde(Profesional? doctor)
    {
        if (doctor is null) return null;

        var nombre = $"{doctor.Nombre} {doctor.Apellido}".Trim();
        if (nombre.Length == 0) return null;

        return new FirmaViewModel
        {
            NombreCompleto = nombre,
            Especialidad = doctor.Especialidad,
            Matricula = doctor.Matricula
        };
    }
}
