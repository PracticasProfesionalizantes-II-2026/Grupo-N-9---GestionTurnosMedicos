using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChronoSaludApi.Repositorios;

/// <summary>
/// Reconoce errores de SQL Server por su número, para poder contestar con un
/// mensaje claro en vez de un error 500.
/// </summary>
public static class ErroresDeBase
{
    // Números de error de SQL Server cuando una fila repite un valor que tiene
    // que ser único: 2601 = índice único, 2627 = clave primaria o restricción única.
    private const int IndiceUnicoRepetido = 2601;
    private const int ClaveUnicaRepetida = 2627;

    /// <summary>True si la base rechazó el guardado por un dato repetido.</summary>
    public static bool EsDatoRepetido(DbUpdateException error)
    {
        // Entity Framework envuelve el error original de SQL Server: viene
        // como InnerException.
        if (error.InnerException is SqlException errorSql)
        {
            return errorSql.Number == IndiceUnicoRepetido || errorSql.Number == ClaveUnicaRepetida;
        }

        return false;
    }
}
