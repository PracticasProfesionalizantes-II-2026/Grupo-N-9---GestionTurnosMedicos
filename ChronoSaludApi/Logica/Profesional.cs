using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Arma los datos de quién firmó una receta o escribió una entrada de la
/// historia clínica: nombre, especialidad y matrícula del doctor.
/// </summary>
public static class Profesional
{
    public static ProfesionalDto? Armar(Doctor? doctor)
    {
        // Sin el doctor y su cuenta cargados no hay nombre para mostrar.
        if (doctor == null || doctor.Usuario == null)
            return null;

        return new ProfesionalDto(
            doctor.Id,
            doctor.Usuario.Nombre,
            doctor.Usuario.Apellido,
            doctor.Especialidad,
            doctor.Matricula);
    }
}
