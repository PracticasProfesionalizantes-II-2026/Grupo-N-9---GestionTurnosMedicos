using System.Security.Claims;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Revisa que la cuenta del token siga activa. La usa Program.cs en cada
/// pedido con token: así una cuenta dada de baja deja de entrar en el momento,
/// sin esperar a que venza el token (8 horas).
/// </summary>
public static class CuentaActiva
{
    public static async Task<bool> EstaActivaAsync(ClaimsPrincipal usuario, IUsuarioRepository repo)
    {
        var idClaim = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var id))
            return false;

        return await repo.EstaActivo(id);
    }
}
