namespace ChronoSaludWeb.Models;

/// <summary>
/// Una fila del catálogo general de coberturas. No es CoberturaFilaViewModel
/// (esa es la cobertura de un paciente puntual, con IdAfiliado incluido).
/// </summary>
public class CoberturaCatalogoFilaViewModel
{
    public required string Nombre { get; init; }
    public string? Plan { get; init; }
}

public class CoberturasIndexViewModel
{
    public string? Nombre { get; init; }

    public bool HayFiltro => !string.IsNullOrWhiteSpace(Nombre);

    public IReadOnlyList<CoberturaCatalogoFilaViewModel> Coberturas { get; init; } = Array.Empty<CoberturaCatalogoFilaViewModel>();

    public bool SinResultados => Coberturas.Count == 0 && HayFiltro;

    public string? Error { get; init; }

    public bool HuboError => Error is not null;
}
