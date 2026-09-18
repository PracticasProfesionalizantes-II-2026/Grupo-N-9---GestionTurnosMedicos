namespace ChronoSalud.Seed;

internal sealed record Persona(string Alias, string Nombre, string Apellido, string Email, string Telefono)
{
    public string NombreCompleto => $"{Nombre} {Apellido}";
}

internal sealed record Doctor(Persona Persona, string Especialidad, string Matricula, string Consultorio);

/// <summary>Turno de la demo. Dia es el desplazamiento en dias desde hoy.</summary>
internal sealed record TurnoDemo(
    string Doctor,
    string Paciente,
    int Dia,
    string Desde,
    string Hasta,
    string Estado,
    string Observaciones);

/// <summary>
/// Los datos que se cargan. Cambiar algo aca alcanza para cambiar la demo:
/// el resto del programa no tiene nada hardcodeado.
/// </summary>
internal static class DatosDemo
{
    public static readonly Persona Administrador =
        new("admin", "Admin", "ChronoSalud", "admin@chronosalud.demo", "11-4000-0000");

    public static readonly Doctor[] Doctores =
    [
        new(new("gomez",   "Laura",  "Gomez",   "laura.gomez@chronosalud.demo",   "11-4000-0101"), "Clínica",       "MP-10001", "Consultorio 101"),
        new(new("rivas",   "Martin", "Rivas",   "martin.rivas@chronosalud.demo",  "11-4000-0102"), "Cardiología",   "MP-10002", "Consultorio 202"),
        new(new("ferrari", "Sofia",  "Ferrari", "sofia.ferrari@chronosalud.demo", "11-4000-0103"), "Traumatología", "MP-10003", "Consultorio 303"),
        new(new("cabrera", "Diego",  "Cabrera", "diego.cabrera@chronosalud.demo", "11-4000-0104"), "Pediatría",     "MP-10004", "Consultorio 404")
    ];

    public static readonly Persona[] Pacientes =
    [
        new("duarte", "Ana",      "Duarte", "ana.duarte@chronosalud.demo",      "11-5000-0201"),
        new("salas",  "Bruno",    "Salas",  "bruno.salas@chronosalud.demo",     "11-5000-0202"),
        new("ponce",  "Carla",    "Ponce",  "carla.ponce@chronosalud.demo",     "11-5000-0203"),
        new("ruiz",   "Elena",    "Ruiz",   "elena.ruiz@chronosalud.demo",      "11-5000-0204"),
        new("molina", "Facundo",  "Molina", "facundo.molina@chronosalud.demo",  "11-5000-0205"),
        new("ortiz",  "Gabriela", "Ortiz",  "gabriela.ortiz@chronosalud.demo",  "11-5000-0206")
    ];

    /// <summary>
    /// 15 turnos de 30 minutos. Ningun doctor tiene dos turnos superpuestos el
    /// mismo dia (la API rechaza eso con 409), y la terna doctor+paciente+dia no
    /// se repite: esa es la clave con la que se detectan los que ya estaban.
    /// </summary>
    public static readonly TurnoDemo[] Turnos =
    [
        new("gomez",   "duarte", 0, "08:00", "08:30", "completado", "Control anual"),
        new("gomez",   "salas",  0, "08:30", "09:00", "completado", "Renovacion de receta"),
        new("rivas",   "ponce",  0, "09:00", "09:30", "completado", "Lectura de electrocardiograma"),
        new("ferrari", "ruiz",   0, "10:00", "10:30", "confirmado", "Dolor lumbar"),
        new("cabrera", "molina", 1, "09:00", "09:30", "confirmado", "Control de crecimiento"),
        new("gomez",   "ortiz",  1, "11:00", "11:30", "confirmado", "Chequeo general"),
        new("rivas",   "duarte", 1, "15:00", "15:30", "cancelado",  "Reprogramar a pedido del paciente"),
        new("ferrari", "salas",  2, "08:30", "09:00", "confirmado", "Post operatorio de rodilla"),
        new("cabrera", "ponce",  2, "12:00", "12:30", "pendiente",  "Primera consulta"),
        new("gomez",   "ruiz",   3, "09:30", "10:00", "pendiente",  "Resultados de laboratorio"),
        new("rivas",   "molina", 3, "16:00", "16:30", "confirmado", "Seguimiento de presion arterial"),
        new("ferrari", "ortiz",  4, "10:30", "11:00", "cancelado",  "Cancelado por el consultorio"),
        new("cabrera", "duarte", 5, "08:00", "08:30", "pendiente",  "Consulta por alergia"),
        new("gomez",   "salas",  6, "14:00", "14:30", "pendiente",  "Certificado laboral"),
        new("rivas",   "ponce",  7, "17:00", "17:30", "cancelado",  "El paciente no confirmo")
    ];
}
