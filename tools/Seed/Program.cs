using System.Globalization;
using System.Text;
using System.Text.Json;
using ChronoSalud.Seed;

// ---------------------------------------------------------------------------
//  Carga datos de prueba en la API de ChronoSalud para la demo.
//
//  Crea un administrador, 4 doctores (con su perfil y especialidad), 6 pacientes
//  y 15 turnos repartidos entre hoy y los proximos 7 dias, y despues acomoda los
//  estados (pendiente / confirmado / completado / cancelado) con el token del
//  administrador.
//
//  Es idempotente: se apoya en lo que ya existe en la API en vez de crear a
//  ciegas, asi que se puede correr dos veces sin duplicar nada.
//    - Usuarios  -> el registro devuelve 409 si el email ya existe; ahi hace login.
//    - Doctores  -> consulta GET /doctores/me con el token del propio doctor.
//    - Pacientes -> consulta GET /pacientes/me (la API crea la ficha al registrar).
//    - Turnos    -> lista GET /turnos y compara por doctor + paciente + fecha.
//
//  Uso:
//    dotnet run --project tools/Seed
//    dotnet run --project tools/Seed -- --url http://localhost:5001 --contrasena "Chrono2026!"
// ---------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

var baseUrl = "http://localhost:5001";
var contrasena = "Chrono2026!";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--url" when i + 1 < args.Length:
            baseUrl = args[++i];
            break;
        case "--contrasena" when i + 1 < args.Length:
            contrasena = args[++i];
            break;
        case "--help":
        case "-h":
            Console.WriteLine("Uso: dotnet run --project tools/Seed -- [--url <api>] [--contrasena <clave>]");
            return 0;
    }
}

using var api = new ApiCliente(baseUrl);

Escribir("ChronoSalud - datos de prueba", ConsoleColor.White);
Escribir($"API: {baseUrl}", ConsoleColor.DarkGray);

// --- 1. Administrador ------------------------------------------------------

Paso("Administrador");
var cuentaAdmin = await ResolverUsuarioAsync(DatosDemo.Administrador, "administrador");
var tokenAdmin = cuentaAdmin.Token;

// --- 2. Doctores: usuario + perfil con especialidad ------------------------

Paso("Doctores");
var idsDoctor = new Dictionary<string, int>();

foreach (var doctor in DatosDemo.Doctores)
{
    var cuenta = await ResolverUsuarioAsync(doctor.Persona, "doctor");

    // El registro no crea la ficha de doctor (a diferencia de la de paciente),
    // asi que la pedimos con el token del propio doctor y la creamos si falta.
    var perfil = await api.GetAsync("/doctores/me", cuenta.Token);

    if (perfil.Ok)
    {
        idsDoctor[doctor.Persona.Alias] = Json.Entero(perfil.Datos, "idDoctor", "id_doctor");
        Ya($"perfil de {doctor.Especialidad} ya existia (id_doctor {idsDoctor[doctor.Persona.Alias]})");
        continue;
    }

    if (perfil.Estado != 404)
    {
        Morir($"No se pudo consultar el perfil de {doctor.Persona.Email} (HTTP {perfil.Estado}): {perfil.Error}");
    }

    var alta = await api.PostAsync("/doctores", new
    {
        IdUsuario = cuenta.IdUsuario,
        Especialidad = doctor.Especialidad,
        Matricula = doctor.Matricula,
        Consultorio = doctor.Consultorio
    }, tokenAdmin);

    if (!alta.Ok)
    {
        Morir($"No se pudo crear el perfil de doctor de {doctor.Persona.Email} (HTTP {alta.Estado}): {alta.Error}");
    }

    // Releemos /doctores/me para no depender de como se serializa id_doctor en el POST.
    perfil = await api.GetAsync("/doctores/me", cuenta.Token);
    if (!perfil.Ok)
    {
        Morir($"El perfil de {doctor.Persona.Email} se creo pero no se pudo releer (HTTP {perfil.Estado}): {perfil.Error}");
    }

    idsDoctor[doctor.Persona.Alias] = Json.Entero(perfil.Datos, "idDoctor", "id_doctor");
    Alta($"perfil de {doctor.Especialidad}, matricula {doctor.Matricula} (id_doctor {idsDoctor[doctor.Persona.Alias]})");
}

// --- 3. Pacientes: la API crea la ficha sola al registrar el usuario -------

Paso("Pacientes");
var idsPaciente = new Dictionary<string, int>();

foreach (var paciente in DatosDemo.Pacientes)
{
    var cuenta = await ResolverUsuarioAsync(paciente, "paciente");

    var perfil = await api.GetAsync("/pacientes/me", cuenta.Token);
    if (!perfil.Ok)
    {
        Morir($"No se pudo leer la ficha de paciente de {paciente.Email} (HTTP {perfil.Estado}): {perfil.Error}");
    }

    idsPaciente[paciente.Alias] = Json.Entero(perfil.Datos, "idPaciente", "id_paciente");
}

// --- 4. Turnos -------------------------------------------------------------

Paso("Turnos");

// Nombres tal como los arma la API en el listado, para poder comparar.
var nombreDoctor = DatosDemo.Doctores.ToDictionary(d => d.Persona.Alias, d => d.Persona.NombreCompleto);
var nombrePaciente = DatosDemo.Pacientes.ToDictionary(p => p.Alias, p => p.NombreCompleto);

// Traemos los turnos que ya hay (incluidos los cancelados: la baja es logica y
// siguen apareciendo en el listado) y los indexamos por doctor+paciente+dia.
var existentes = new Dictionary<string, JsonElement>();
var pagina = 1;
int cantidadLote;

do
{
    var listado = await api.GetAsync($"/turnos?pagina={pagina}&limite=200", tokenAdmin);
    if (!listado.Ok)
    {
        Morir($"No se pudieron listar los turnos existentes (HTTP {listado.Estado}): {listado.Error}");
    }

    var total = Json.Entero(listado.Datos, "total");
    cantidadLote = 0;

    if (Json.Buscar(listado.Datos, out var lote, "turnos") && lote.ValueKind == JsonValueKind.Array)
    {
        foreach (var turno in lote.EnumerateArray())
        {
            cantidadLote++;

            var fechaTexto = Json.Texto(turno, "fechaInicio", "fecha_inicio");
            if (!DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var fechaTurno))
            {
                continue;
            }

            var clave = Clave(
                Json.Texto(turno, "doctor"),
                Json.Texto(turno, "paciente"),
                fechaTurno);

            existentes.TryAdd(clave, turno);
        }
    }

    if (pagina * 200 >= total)
    {
        break;
    }

    pagina++;
}
while (cantidadLote > 0);

var hoy = DateTime.Today;
var creados = 0;
var saltados = 0;
var aRevisar = new List<(int Id, string EstadoActual, string EstadoDeseado)>();

foreach (var turno in DatosDemo.Turnos)
{
    var fecha = hoy.AddDays(turno.Dia);
    var clave = Clave(nombreDoctor[turno.Doctor], nombrePaciente[turno.Paciente], fecha);

    if (existentes.TryGetValue(clave, out var ya))
    {
        aRevisar.Add((
            Json.Entero(ya, "idTurno", "id_turno"),
            Json.Texto(ya, "estado"),
            turno.Estado));
        saltados++;
        continue;
    }

    var alta = await api.PostAsync("/turnos", new
    {
        IdPaciente = idsPaciente[turno.Paciente],
        IdDoctor = idsDoctor[turno.Doctor],
        // Sin zona horaria y a las 00:00: la hora del turno viaja aparte, en horaInicio.
        FechaInicio = fecha.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        HoraInicio = turno.Desde,
        HoraFin = turno.Hasta,
        Observaciones = turno.Observaciones
    }, tokenAdmin);

    if (!alta.Ok)
    {
        Morir($"No se pudo crear el turno de {nombrePaciente[turno.Paciente]} con " +
              $"{nombreDoctor[turno.Doctor]} el {fecha:dd/MM} (HTTP {alta.Estado}): {alta.Error}");
    }

    aRevisar.Add((Json.Entero(alta.Datos, "id_turno", "idTurno"), "pendiente", turno.Estado));
    creados++;
    Alta($"{fecha:dd/MM} {turno.Desde} - {nombrePaciente[turno.Paciente]} con {nombreDoctor[turno.Doctor]}");
}

if (saltados > 0)
{
    Ya($"{saltados} turno(s) ya estaban cargados");
}

// --- 5. Estados, con el token del administrador ----------------------------

Paso("Estados");
var actualizados = 0;

foreach (var item in aRevisar)
{
    if (string.Equals(item.EstadoActual, item.EstadoDeseado, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    // Mandamos solo el estado: si no viaja fecha ni hora, la API no revalida
    // conflictos de agenda, que es justo lo que queremos aca.
    var cambio = await api.PutAsync($"/turnos/{item.Id}", new { Estado = item.EstadoDeseado }, tokenAdmin);

    if (!cambio.Ok)
    {
        Morir($"No se pudo pasar el turno {item.Id} a '{item.EstadoDeseado}' (HTTP {cambio.Estado}): {cambio.Error}");
    }

    actualizados++;
}

Escribir($"   {actualizados} turno(s) cambiaron de estado, {aRevisar.Count - actualizados} ya estaban bien",
    ConsoleColor.DarkGray);

foreach (var grupo in DatosDemo.Turnos.GroupBy(t => t.Estado).OrderBy(g => g.Key, StringComparer.Ordinal))
{
    Escribir($"   {grupo.Key,-11} {grupo.Count()}", ConsoleColor.DarkGray);
}

// --- 6. Credenciales -------------------------------------------------------

var credenciales = new List<(string Rol, string Nombre, string Email, string Detalle)>
{
    ("administrador", DatosDemo.Administrador.NombreCompleto, DatosDemo.Administrador.Email, "")
};

credenciales.AddRange(DatosDemo.Doctores.Select(d =>
    ("doctor", d.Persona.NombreCompleto, d.Persona.Email, $"{d.Especialidad} - {d.Matricula}")));

credenciales.AddRange(DatosDemo.Pacientes.Select(p =>
    ("paciente", p.NombreCompleto, p.Email, "")));

Paso("Credenciales");

var anchoRol = Math.Max(3, credenciales.Max(c => c.Rol.Length));
var anchoNombre = Math.Max(6, credenciales.Max(c => c.Nombre.Length));
var anchoEmail = Math.Max(5, credenciales.Max(c => c.Email.Length));

Console.WriteLine($"   {"Rol".PadRight(anchoRol)}  {"Nombre".PadRight(anchoNombre)}  " +
                  $"{"Email".PadRight(anchoEmail)}  Detalle");

foreach (var (rol, nombre, email, detalle) in credenciales)
{
    Console.WriteLine($"   {rol.PadRight(anchoRol)}  {nombre.PadRight(anchoNombre)}  " +
                      $"{email.PadRight(anchoEmail)}  {detalle}");
}

Console.WriteLine();
Escribir($"Contrasena de todos los usuarios: {contrasena}", ConsoleColor.White);
Escribir($"Turnos: {creados} nuevo(s), {saltados} ya existente(s), " +
         $"{DatosDemo.Turnos.Length} en total para la demo.", ConsoleColor.White);
Escribir("El seeder es idempotente: podes volver a correrlo sin duplicar nada.\n", ConsoleColor.DarkGray);

return 0;

// --- Helpers ---------------------------------------------------------------

// Clave de comparacion de un turno: quien lo atiende, quien lo recibe y que dia.
static string Clave(string doctor, string paciente, DateTime fecha)
    => $"{doctor}|{paciente}|{fecha:yyyy-MM-dd}";

void Escribir(string texto, ConsoleColor color)
{
    var anterior = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(texto);
    Console.ForegroundColor = anterior;
}

void Paso(string texto)
{
    Console.WriteLine();
    Escribir($"== {texto}", ConsoleColor.Cyan);
}

void Alta(string texto) => Escribir($"   + {texto}", ConsoleColor.Green);

void Ya(string texto) => Escribir($"   = {texto}", ConsoleColor.DarkGray);

void Morir(string texto)
{
    Console.WriteLine();
    Escribir($"ERROR: {texto}", ConsoleColor.Red);
    Environment.Exit(1);
}

// Registra el usuario o, si el email ya estaba, hace login.
async Task<(int IdUsuario, string Token, bool Creado)> ResolverUsuarioAsync(Persona persona, string rol)
{
    var alta = await api.PostAsync("/usuarios/registro", new
    {
        persona.Nombre,
        persona.Apellido,
        persona.Email,
        Contrasena = contrasena,
        persona.Telefono,
        Rol = rol
    });

    if (alta.Ok)
    {
        Alta($"{rol} {persona.NombreCompleto} <{persona.Email}>");
        return (Json.Entero(alta.Datos, "idUsuario", "id_usuario"), Json.Texto(alta.Datos, "token"), true);
    }

    if (alta.Estado != 409)
    {
        if (alta.Estado == 0)
        {
            Morir($"No se pudo contactar la API en {baseUrl}. Verifica que este corriendo " +
                  $"(dotnet run --project ChronoSaludApi). Detalle: {alta.Error}");
        }

        Morir($"No se pudo registrar {persona.Email} (HTTP {alta.Estado}): {alta.Error}");
    }

    // 409: el email ya existe, entramos con la contrasena del seeder.
    var login = await api.PostAsync("/usuarios/login", new
    {
        persona.Email,
        Contrasena = contrasena
    });

    if (!login.Ok)
    {
        Morir($"El usuario {persona.Email} ya existe pero su contrasena no es '{contrasena}'. " +
              "Borra ese usuario o corre el seeder con --contrasena.");
    }

    Ya($"{rol} {persona.NombreCompleto} <{persona.Email}> ya existia");
    return (Json.Entero(login.Datos, "idUsuario", "id_usuario"), Json.Texto(login.Datos, "token"), false);
}
