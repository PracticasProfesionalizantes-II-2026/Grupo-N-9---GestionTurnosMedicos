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

        try
        {
            // 1. El modelo (entidades y AppDbContext) cambió y nadie generó la migración.
            if (db.Database.HasPendingModelChanges())
            {
                app.Logger.LogWarning(
                    "El modelo de datos cambió y falta generar la migración. Corré: " +
                    "dotnet ef migrations add <Nombre> --project ChronoSaludApi");
            }

            // 2. Hay migraciones en el código que la base local todavía no tiene.
            var pendientes = db.Database.GetPendingMigrations().ToList();
            if (pendientes.Count > 0)
            {
                app.Logger.LogWarning(
                    "La base de datos no tiene estas migraciones: {Migraciones}. Corré: " +
                    "dotnet ef database update --project ChronoSaludApi",
                    string.Join(", ", pendientes));
            }
        }
        catch (Exception error)
        {
            // Por ejemplo, SQL Server apagado: el aviso no frena el arranque.
            app.Logger.LogWarning(error, "No se pudo revisar si la base de datos está al día con las migraciones.");
        }
    }
}
