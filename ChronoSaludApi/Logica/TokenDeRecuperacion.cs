using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ChronoSaludApi.Logica;

/// <summary>
/// El token del enlace de "Olvidé mi contraseña". No se guarda en la base:
/// lleva adentro el id del usuario y hasta cuándo vale, más una firma que solo
/// puede armar quien conoce la clave del servidor. Así nadie puede inventarse
/// uno ni cambiarle el id o la hora.
///
/// La firma incluye el hash actual de la contraseña: apenas el usuario la
/// cambia, el hash es otro y el mismo enlace deja de servir. Por eso cada
/// enlace se usa una sola vez, sin tener que anotarlo en ningún lado.
///
/// Formato: "idUsuario.vencimiento.firma", con el vencimiento en ticks y la
/// firma en Base64Url (solo letras, números, "-" y "_", seguro en una URL).
/// </summary>
public static class TokenDeRecuperacion
{
    /// <summary>Cuánto vale un enlace desde que se pide.</summary>
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(1);

    public static string Crear(int idUsuario, string hashContrasena, DateTime ahora, string clave)
    {
        var vencimiento = (ahora + Duracion).Ticks.ToString(CultureInfo.InvariantCulture);
        var firma = Firmar(idUsuario.ToString(CultureInfo.InvariantCulture), vencimiento, hashContrasena, clave);
        return $"{idUsuario}.{vencimiento}.{firma}";
    }

    /// <summary>
    /// Saca el id del usuario de un token, sin revisar todavía la firma (para
    /// eso hace falta buscar al usuario y su hash). Null si el formato no sirve.
    /// </summary>
    public static int? LeerIdUsuario(string? token)
    {
        var partes = token?.Split('.');
        if (partes is not { Length: 3 })
            return null;

        return int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    /// <summary>
    /// True si el token es de ese usuario, no venció y la firma coincide con
    /// su contraseña actual.
    /// </summary>
    public static bool EsValido(string? token, int idUsuario, string hashContrasena, DateTime ahora, string clave)
    {
        var partes = token?.Split('.');
        if (partes is not { Length: 3 })
            return false;

        if (partes[0] != idUsuario.ToString(CultureInfo.InvariantCulture))
            return false;

        if (!long.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            return false;

        if (ahora > new DateTime(ticks))
            return false;

        var esperada = Encoding.ASCII.GetBytes(Firmar(partes[0], partes[1], hashContrasena, clave));
        var recibida = Encoding.ASCII.GetBytes(partes[2]);

        // FixedTimeEquals tarda lo mismo acierte o no, así no se puede ir
        // adivinando la firma letra por letra midiendo cuánto tarda.
        return CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }

    private static string Firmar(string idUsuario, string vencimiento, string hashContrasena, string clave)
    {
        var datos = Encoding.UTF8.GetBytes($"{idUsuario}|{vencimiento}|{hashContrasena}");
        var firma = HMACSHA256.HashData(Encoding.UTF8.GetBytes(clave), datos);
        return Base64Url.EncodeToString(firma);
    }
}
