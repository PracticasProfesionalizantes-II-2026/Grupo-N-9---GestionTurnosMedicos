using System.Security.Claims;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Quién hace el pedido: el usuario y el rol que vienen en el token. Los
/// endpoints lo arman y la lógica decide con él qué puede hacer cada uno;
/// nunca sale del cuerpo ni de la ruta.
/// </summary>
public record Solicitante(int IdUsuario, string Rol)
{
    public bool EsPaciente      => Rol == "paciente";
    public bool EsDoctor        => Rol == "doctor";
    public bool EsAdministrador => Rol == "administrador";

    /// <summary>
    /// Null si al token le falta el id de usuario o el rol: el endpoint
    /// contesta 401 en vez de seguir con un solicitante a medias.
    /// </summary>
    public static Solicitante? Desde(ClaimsPrincipal usuario)
    {
        var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;

        return int.TryParse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idUsuario)
               && !string.IsNullOrWhiteSpace(rol)
            ? new Solicitante(idUsuario, rol)
            : null;
    }
}
