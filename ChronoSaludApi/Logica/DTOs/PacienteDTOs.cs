namespace ChronoSaludApi.Logica.DTOs;

public record PacienteDto(
    int IdPaciente,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones,
    string? Dni,
    string? Direccion,
    string? Nacionalidad,
    string? EstadoCivil,
    string? FotoUrl,
    string? TipoDocumento,
    string? Provincia,
    string? Localidad,
    string? CodigoPostal,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono
);

public record PacienteListaDto(
    int IdPaciente,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string? Dni
);

// Un campo que no viene (o viene vacío) no cambia lo que ya estaba cargado.
// Para vaciar un dato hay que nombrarlo en Borrar, por ejemplo
// "borrar": ["alergias", "direccion"]. Ver FichaPaciente.BorrarCampos.
public record PacienteUpdateDto(
    DateTime? FechaNacimiento = null,
    string? Sexo = null,
    string? GrupoSanguineo = null,
    string? Alergias = null,
    string? Condiciones = null,
    string? Dni = null,
    string? Direccion = null,
    string? Nacionalidad = null,
    string? EstadoCivil = null,
    string? FotoUrl = null,
    string? TipoDocumento = null,
    string? Provincia = null,
    string? Localidad = null,
    string? CodigoPostal = null,
    string? ContactoEmergenciaNombre = null,
    string? ContactoEmergenciaTelefono = null,
    List<string>? Borrar = null
);
