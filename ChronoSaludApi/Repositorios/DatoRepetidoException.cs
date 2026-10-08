namespace ChronoSaludApi.Repositorios;

/// <summary>
/// La base no guardó porque un dato que tiene que ser único ya estaba
/// (por ejemplo, el email de una cuenta o el DNI de un paciente).
/// </summary>
public class DatoRepetidoException : Exception
{
    public DatoRepetidoException(Exception interna)
        : base("La base de datos rechazó un dato repetido.", interna) { }
}
