namespace ChronoSaludApi.Logica.DTOs;

public record EstudioDto(
    int IdEstudio,
    string Tipo,
    string Estado,
    DateTime FechaSolicitud,
    int? IdTurno,
    string? Resultado,
    string? ArchivoUrl,
    DateTime? FechaResultado
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
