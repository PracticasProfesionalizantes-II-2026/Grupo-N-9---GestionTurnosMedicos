namespace ChronoSaludApi.Logica.DTOs;

public record UsuarioFotoDto(
    byte[] Contenido,
    string TipoContenido,
    DateTime ActualizadaEn
);

// Una foto que existe y que quien consulta puede ver. Id es el id por el que
// se preguntó (de paciente, de doctor o de turno) e IdUsuario el dueño.
public record FotoDisponibleDto(
    int Id,
    int IdUsuario
);

public record FotosDisponiblesDto(
    IEnumerable<FotoDisponibleDto> Pacientes,
    IEnumerable<FotoDisponibleDto> Doctores,
    IEnumerable<FotoDisponibleDto> Turnos
);
