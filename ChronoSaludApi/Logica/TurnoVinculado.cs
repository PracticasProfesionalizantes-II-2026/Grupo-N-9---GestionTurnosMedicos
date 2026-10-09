using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Revisa el turno al que se vincula una receta o una consulta de la
/// historia clínica: tiene que ser de ese paciente y de ese doctor.
/// </summary>
public static class TurnoVinculado
{
    public const string NoEncontrado =
        "Turno no encontrado: tiene que ser un turno de este paciente con este doctor.";

    /// <summary>
    /// Devuelve null si está bien (o si no viene ningún turno); si no, el error.
    /// </summary>
    public static async Task<string?> Revisar(ITurnoRepository turnos, int? idTurno, int idPaciente, int idDoctor)
    {
        if (idTurno == null)
            return null;

        var turno = await turnos.ObtenerPorId(idTurno.Value);

        // Un turno de otro paciente o de otro doctor se responde como
        // inexistente: no confirmamos que ese id exista.
        if (turno == null || turno.IdPaciente != idPaciente || turno.IdDoctor != idDoctor)
            return NoEncontrado;

        return null;
    }
}
