using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChronoSaludApi.Datos;

/// <summary>
/// Solo para desarrollo. Al arrancar, revisa que la base local esté al día con
/// el código y, si no, lo avisa en la consola de la API con el comando a
/// correr. Sin este aviso, una migración olvidada se ve recién como un
/// "Error 500" en la Web (por ejemplo, "El nombre de columna ... no es válido").
/// </summary>
public static class AvisoDeMigraciones
{
    public static void Revisar(WebApplication app)
    {
        // AppDbContext se registra por pedido (Scoped): fuera de un pedido hay
        // que crear un "scope" a mano para poder pedirlo.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // La base a la que está conectada la API. Con levantar.ps1 es la local;
        // con "dotnet run" o "dotnet ef" sueltos, la de los user-secrets.
        var cadena = db.Database.GetConnectionString() ?? "";
        var baseUsada = DescribirBase(cadena);

        try
        {
            // 1. El modelo (entidades y AppDbContext) cambió y nadie generó la migración.
            if (db.Database.HasPendingModelChanges())
            {
                app.Logger.LogWarning(
                    "El modelo de datos cambió y falta generar la migración. Corré: " +
                    "dotnet ef migrations add <Nombre> --project ChronoSaludApi");
            }

            // 2. Hay migraciones en el código que la base todavía no tiene.
            var pendientes = string.Join(", ", db.Database.GetPendingMigrations());
            if (pendientes != "")
            {
                var comando = ComandoParaActualizar(cadena);

                if (comando != null)
                {
                    app.Logger.LogWarning(
                        "A la base {Base} le faltan estas migraciones: {Migraciones}. Corré: {Comando}",
                        baseUsada, pendientes, comando);
                }
                else
                {
                    app.Logger.LogWarning(
                        "A la base {Base} le faltan estas migraciones: {Migraciones}. Es una base con " +
                        "usuario y contraseña (por ejemplo, Azure): aplicalas con el script idempotente " +
                        "en SSMS (ver README).",
                        baseUsada, pendientes);
                }
            }
        }
        catch (Exception error)
        {
            // Por ejemplo, SQL Server apagado: el aviso no frena el arranque.
            app.Logger.LogWarning(error,
                "No se pudo revisar si la base {Base} está al día con las migraciones.", baseUsada);
        }
    }

    /// <summary>
    /// Arma el "dotnet ef database update" para la misma base que usa la API.
    /// Lleva --connection a propósito: sin eso, dotnet ef usa la base de los
    /// user-secrets, que puede ser otra (por ejemplo, Azure).
    /// Devuelve null si la base pide usuario y contraseña: esa cadena tiene
    /// una contraseña y no se escribe en la consola.
    /// </summary>
    public static string? ComandoParaActualizar(string cadena)
    {
        var datos = new SqlConnectionStringBuilder(cadena);

        // IntegratedSecurity es el "Trusted_Connection=True" de la cadena: entra
        // con el usuario de Windows, así que la cadena no tiene contraseña.
        if (!datos.IntegratedSecurity)
            return null;

        return $"dotnet ef database update --project ChronoSaludApi --connection \"{cadena}\"";
    }

    /// <summary>
    /// "ChronoSaludDB en localhost": el nombre de la base y el servidor, sin
    /// usuario ni contraseña.
    /// </summary>
    public static string DescribirBase(string cadena)
    {
        try
        {
            var datos = new SqlConnectionStringBuilder(cadena);
            return $"{datos.InitialCatalog} en {datos.DataSource}";
        }
        catch (ArgumentException)
        {
            // La cadena está vacía o mal escrita.
            return "(sin cadena de conexión válida)";
        }
    }
}
