using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Lo que comparten el alta de un paciente y la edición de su ficha: copiar
/// los datos que llegan y vaciar los que se piden borrar.
/// </summary>
public static class FichaPaciente
{
    /// <summary>
    /// Copia al paciente cada dato que vino con valor. Un dato que no vino, o
    /// que vino vacío, no cambia lo que ya estaba cargado. El DNI queda afuera
    /// porque tiene reglas propias (ver PacienteLogica.Actualizar).
    /// </summary>
    public static void CopiarDatos(PacienteUpdateDto datos, Paciente paciente)
    {
        if (datos.FechaNacimiento.HasValue) paciente.FechaNacimiento = datos.FechaNacimiento;

        if (!string.IsNullOrWhiteSpace(datos.Sexo))           paciente.Sexo           = datos.Sexo.Trim();
        if (!string.IsNullOrWhiteSpace(datos.GrupoSanguineo)) paciente.GrupoSanguineo = datos.GrupoSanguineo.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Alergias))       paciente.Alergias       = datos.Alergias.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Condiciones))    paciente.Condiciones    = datos.Condiciones.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Direccion))      paciente.Direccion      = datos.Direccion.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Nacionalidad))   paciente.Nacionalidad   = datos.Nacionalidad.Trim();
        if (!string.IsNullOrWhiteSpace(datos.EstadoCivil))    paciente.EstadoCivil    = datos.EstadoCivil.Trim();
        if (!string.IsNullOrWhiteSpace(datos.FotoUrl))        paciente.FotoUrl        = datos.FotoUrl.Trim();
        if (!string.IsNullOrWhiteSpace(datos.TipoDocumento))  paciente.TipoDocumento  = datos.TipoDocumento.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Provincia))      paciente.Provincia      = datos.Provincia.Trim();
        if (!string.IsNullOrWhiteSpace(datos.Localidad))      paciente.Localidad      = datos.Localidad.Trim();
        if (!string.IsNullOrWhiteSpace(datos.CodigoPostal))   paciente.CodigoPostal   = datos.CodigoPostal.Trim();

        if (!string.IsNullOrWhiteSpace(datos.ContactoEmergenciaNombre))
            paciente.ContactoEmergenciaNombre = datos.ContactoEmergenciaNombre.Trim();

        if (!string.IsNullOrWhiteSpace(datos.ContactoEmergenciaTelefono))
            paciente.ContactoEmergenciaTelefono = datos.ContactoEmergenciaTelefono.Trim();
    }

    /// <summary>
    /// Vacía los campos nombrados en <paramref name="campos"/>, por ejemplo
    /// ["alergias", "direccion"]. Los nombres son los mismos del JSON y no
    /// importan las mayúsculas. Devuelve null si salió bien, o el motivo si un
    /// nombre no se puede borrar. El DNI solo lo borra el personal.
    /// </summary>
    public static string? BorrarCampos(List<string>? campos, Paciente paciente, bool esPersonal)
    {
        if (campos == null) return null;

        foreach (var campo in campos)
        {
            switch (campo.Trim().ToLowerInvariant())
            {
                case "fechanacimiento":            paciente.FechaNacimiento            = null; break;
                case "sexo":                       paciente.Sexo                       = null; break;
                case "gruposanguineo":             paciente.GrupoSanguineo             = null; break;
                case "alergias":                   paciente.Alergias                   = null; break;
                case "condiciones":                paciente.Condiciones                = null; break;
                case "direccion":                  paciente.Direccion                  = null; break;
                case "nacionalidad":               paciente.Nacionalidad               = null; break;
                case "estadocivil":                paciente.EstadoCivil                = null; break;
                case "tipodocumento":              paciente.TipoDocumento              = null; break;
                case "provincia":                  paciente.Provincia                  = null; break;
                case "localidad":                  paciente.Localidad                  = null; break;
                case "codigopostal":               paciente.CodigoPostal               = null; break;
                case "contactoemergencianombre":   paciente.ContactoEmergenciaNombre   = null; break;
                case "contactoemergenciatelefono": paciente.ContactoEmergenciaTelefono = null; break;

                case "dni":
                    if (!esPersonal)
                        return "El DNI solo lo puede borrar la administración.";
                    paciente.Dni = null;
                    break;

                default:
                    // Si un nombre no existe se corta acá. Lo que ya se vació
                    // en este pedido no llega a guardarse: quien llama no
                    // guarda nada cuando recibe un error.
                    return $"No se puede borrar el campo \"{campo}\".";
            }
        }

        return null;
    }
}
