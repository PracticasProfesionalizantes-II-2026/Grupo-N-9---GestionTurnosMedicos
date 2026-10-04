namespace ChronoSaludApi.Logica.DTOs;

public record UsuarioFotoDto(
    byte[] Contenido,
    string TipoContenido,
    DateTime ActualizadaEn
);
