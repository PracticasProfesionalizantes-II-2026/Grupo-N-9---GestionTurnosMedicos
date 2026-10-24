namespace ChronoSaludApi.Logica.DTOs;

public record EstudioDto(
    int IdEstudio,
    string Tipo,
    string Estado,
    DateTime FechaSolicitud,
    int? IdTurno,
    string? Resultado,
    string? ArchivoUrl,
    DateTime? FechaResultado,
    // Paso 16: qué estudio es y quién lo pidió (el doctor del turno
    // vinculado; null si el estudio no tiene turno).
    string Descripcion = "",
    ProfesionalDto? Doctor = null,
    // Paso 23: si el resultado tiene un PDF subido a la app y con qué nombre.
    bool TienePdf = false,
    string? NombrePdf = null
);

public record EstudioCreateDto(
    int IdPaciente,
    int? IdTurno,
    string Tipo,
    string Descripcion,
    DateTime FechaSolicitud
);

public record EstudioResultadoDto(
    string Resultado,
    string? ArchivoUrl,
    string Estado,
    DateTime FechaResultado
);
