using ChronoSaludApi.Datos;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del comando que sugiere el aviso de migraciones pendientes.
/// </summary>
public class AvisoDeMigracionesTests
{
    [Fact]
    public void Con_la_base_local_sugiere_el_comando_con_esa_misma_base()
    {
        // Preparar: la cadena que arma levantar.ps1.
        var cadena = "Server=localhost;Database=ChronoSaludDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // Ejecutar
        var comando = AvisoDeMigraciones.ComandoParaActualizar(cadena);

        // Verificar: lleva --connection con la cadena, para no actualizar otra base.
        Assert.Equal(
            "dotnet ef database update --project ChronoSaludApi --connection \"" + cadena + "\"",
            comando);
        Assert.Equal("ChronoSaludDB en localhost", AvisoDeMigraciones.DescribirBase(cadena));
    }

    [Fact]
    public void Con_usuario_y_contrasena_no_muestra_la_cadena()
    {
        // Preparar: una cadena como la de Azure.
        var cadena = "Server=tcp:demo.database.windows.net,1433;Database=ChronoSaludDB;" +
                     "User ID=admin;Password=NoTieneQueAparecer1!;Encrypt=True;";

        // Ejecutar
        var comando = AvisoDeMigraciones.ComandoParaActualizar(cadena);
        var descripcion = AvisoDeMigraciones.DescribirBase(cadena);

        // Verificar: ni el comando ni la descripción llevan la contraseña.
        Assert.Null(comando);
        Assert.DoesNotContain("NoTieneQueAparecer1!", descripcion);
    }
}
