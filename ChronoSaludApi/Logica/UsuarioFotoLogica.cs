using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class UsuarioFotoLogica : IUsuarioFotoLogica
{
    // 2 MB
    public const int TamanoMaximo = 2 * 1024 * 1024;

    private readonly IUsuarioFotoRepository _repo;
    private readonly IUsuarioRepository _usuarioRepo;

    public UsuarioFotoLogica(IUsuarioFotoRepository repo, IUsuarioRepository usuarioRepo)
    {
        _repo = repo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<(bool ok, string? error)> Guardar(int idUsuario, byte[] contenido)
    {
        if (contenido.Length == 0)
            return (false, "El archivo está vacío.");

        if (contenido.Length > TamanoMaximo)
            return (false, "La foto no puede superar los 2 MB.");

        // El tipo sale de la firma del archivo, nunca de lo que diga el cliente.
        var tipo = DetectarTipo(contenido);
        if (tipo == null)
            return (false, "El archivo no es una imagen válida. Formatos aceptados: JPG, PNG o WebP.");

        var usuario = await _usuarioRepo.ObtenerPorId(idUsuario);
        if (usuario == null) return (false, "Usuario no encontrado.");

        await _repo.Guardar(new UsuarioFoto
        {
            IdUsuario     = idUsuario,
            Contenido     = contenido,
            TipoContenido = tipo,
            ActualizadaEn = DateTime.UtcNow
        });

        return (true, null);
    }

    public async Task<UsuarioFotoDto?> Obtener(int idUsuario)
    {
        var foto = await _repo.ObtenerPorIdUsuario(idUsuario);
        if (foto == null) return null;

        return new UsuarioFotoDto(foto.Contenido, foto.TipoContenido, foto.ActualizadaEn);
    }

    public async Task<DateTime?> ObtenerFechaActualizacion(int idUsuario)
        => await _repo.ObtenerFechaActualizacion(idUsuario);

    public async Task<(bool ok, string? error)> Eliminar(int idUsuario)
    {
        var usuario = await _usuarioRepo.ObtenerPorId(idUsuario);
        if (usuario == null) return (false, "Usuario no encontrado.");

        await _repo.Eliminar(idUsuario);
        return (true, null);
    }

    // Reconoce el formato por los primeros bytes. Todo lo que no sea JPEG, PNG
    // o WebP (SVG, GIF, PDF, texto, etc.) devuelve null.
    private static string? DetectarTipo(byte[] b)
    {
        // JPEG: FF D8 FF
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (b.Length >= 8 &&
            b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
            b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return "image/png";

        // WebP: "RIFF" + 4 bytes de tamaño + "WEBP"
        if (b.Length >= 12 &&
            b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' &&
            b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
            return "image/webp";

        return null;
    }
}
