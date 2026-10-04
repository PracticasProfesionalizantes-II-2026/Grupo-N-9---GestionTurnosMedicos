namespace ChronoSaludApi.Entidades;

// Foto de perfil de un usuario. Va en una tabla aparte y no como columna de
// Usuario para que el contenido no viaje cada vez que se carga un usuario
// (login, búsquedas, etc.).
public class UsuarioFoto
{
    // Clave primaria y, a la vez, clave foránea a Usuario (una foto por usuario)
    public int IdUsuario { get; set; }

    public byte[] Contenido { get; set; } = Array.Empty<byte>();

    // "image/jpeg" | "image/png" | "image/webp", según la firma del archivo
    public string TipoContenido { get; set; } = string.Empty;

    public DateTime ActualizadaEn { get; set; }
}
