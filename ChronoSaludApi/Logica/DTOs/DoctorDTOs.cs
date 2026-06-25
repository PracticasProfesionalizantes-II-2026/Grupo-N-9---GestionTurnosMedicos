namespace ChronoSaludApi.Logica.DTOs;

public record DoctorDto(
    int IdDoctor,
    string Nombre,
    string Apellido,
    string Especialidad,
    string Matricula,
    string? Consultorio
);

public record DoctorListaDto(
    int IdDoctor,
    string Nombre,
    string Especialidad,
    string Matricula
);

public record DoctorCreateDto(
    int IdUsuario,
    string Especialidad,
    string Matricula,
    string? Consultorio
);

public record DoctorUpdateDto(
    string? Especialidad,
    string? Consultorio
);
