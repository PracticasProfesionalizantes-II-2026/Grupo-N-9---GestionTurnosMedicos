namespace ChronoSaludApi.Logica.DTOs;

public record MedicamentoDto(
    int IdMedicamento,
    string Nombre,
    string? Descripcion,
    string? NombreGenerico = null,
    string? Concentracion = null,
    string? FormaFarmaceutica = null,
    string? Laboratorio = null
);

public record MedicamentoCreateDto(
    string Nombre,
    string? Descripcion,
    string? NombreGenerico = null,
    string? Concentracion = null,
    string? FormaFarmaceutica = null,
    string? Laboratorio = null
);

public record RecetaMedicamentoDto(
    int IdMedicamento,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones
);

// Un medicamento de una receta, como quedó guardado al emitirla. Los datos del
// medicamento son una copia: no cambian si después se edita el medicamento.
// RecetaMedicamentoDto es lo que se manda al emitir; esto es lo que se lee.
public record MedicamentoRecetadoDto(
    int IdMedicamento,
    string Nombre,
    string? NombreGenerico,
    string? Concentracion,
    string? FormaFarmaceutica,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones
);
