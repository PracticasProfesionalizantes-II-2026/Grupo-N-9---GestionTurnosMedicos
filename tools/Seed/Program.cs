using System.Globalization;
using System.Text;
using System.Text.Json;
using ChronoSalud.Seed;

// ---------------------------------------------------------------------------
//  Carga datos de prueba en la API de ChronoSalud para la demo.
//
//  Crea un administrador, 4 doctores (con su perfil y especialidad), horarios
//  laborales de lunes a viernes (manana 08-14 o tarde 14-20) para los doctores
//  activos que no tengan, 6 pacientes y 15 turnos repartidos entre hoy y los
//  proximos 7 dias habiles, y despues acomoda los estados (pendiente /
//  confirmado / completado / cancelado) con el token del administrador.
//  Tambien carga los medicamentos del Vademecum Nacional que estan en data/ y
//  el marcador "Otro (ver indicaciones)" que usa la opcion "Otro..." de la receta.
//
//  Es idempotente: se apoya en lo que ya existe en la API en vez de crear a
//  ciegas, asi que se puede correr dos veces sin duplicar nada.
//    - Usuarios  -> el registro devuelve 409 si el email ya existe; ahi hace login.
//    - Administrador -> primero intenta entrar; solo si no existe lo registra.
//    - Medicamentos -> lista GET /medicamentos y compara por nombre comercial +
//                      generico + concentracion + forma farmaceutica.
//    - Doctores  -> consulta GET /doctores/me con el token del propio doctor.
//    - Horarios  -> consulta GET /doctores/{id}/horarios y solo carga si esta vacio.
//    - Pacientes -> consulta GET /pacientes/me (la API crea la ficha al registrar).
//    - Turnos    -> lista GET /turnos y compara por doctor + paciente + fecha.
//
//  Uso:
//    dotnet run --project tools/Seed
//    dotnet run --project tools/Seed -- --url http://localhost:5001 --contrasena "Chrono2026!"
//
//  La API solo deja registrar un administrador sin sesion cuando todavia no hay
//  ninguno. Si la base ya tiene administradores y el de la demo no existe, hay
//  que entrar con uno de ellos: --email <cuenta>. Con --email, --contrasena es
//  la de esa cuenta (si no va, la pide) y las cuentas de la demo se crean con
//  la contrasena de siempre.
//    dotnet run --project tools/Seed -- --email <administrador>
//
//  La contrasena de la demo es publica (esta escrita en el repo): solo se usa
//  contra la API de esta maquina. Si --url apunta a otra, el seeder pide por
//  consola la contrasena para las cuentas de la demo.
//
//  Solo los medicamentos, sin usuarios ni turnos de demo (pensado para Azure).
//  Aca la cuenta de --email puede ser administrador o doctor.
//    dotnet run --project tools/Seed -- --url <api> --solo-medicamentos --email <cuenta>
// ---------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

const string contrasenaDemo = "Chrono2026!";

var baseUrl = "http://localhost:5001";
var contrasena = contrasenaDemo;
var contrasenaIndicada = false;
var contrasenaPorConsola = false;
var contrasenaCuenta = string.Empty;
var soloMedicamentos = false;
string? cuentaPropia = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--url" when i + 1 < args.Length:
            baseUrl = args[++i];
            break;
        case "--contrasena" when i + 1 < args.Length:
            contrasena = args[++i];
            contrasenaIndicada = true;
            break;
        case "--email" when i + 1 < args.Length:
            cuentaPropia = args[++i];
            break;
        case "--solo-medicamentos":
            soloMedicamentos = true;
            break;
        case "--help":
        case "-h":
            Console.WriteLine("Uso: dotnet run --project tools/Seed -- [--url <api>] [--contrasena <clave>] " +
                              "[--email <cuenta>] [--solo-medicamentos]");
            return 0;
    }
}

if (cuentaPropia is not null)
{
    // La contrasena de la cuenta propia va aparte: no se usa para crear las
    // cuentas de la demo ni se muestra al final.
    contrasenaCuenta = contrasenaIndicada ? contrasena : LeerContrasena(cuentaPropia);
    contrasena = contrasenaDemo;
    contrasenaIndicada = false;
}

// La contrasena de la demo esta escrita en el repo, asi que solo sirve para la
// API de esta maquina. Contra cualquier otra, las cuentas de la demo llevan
// una que se pide por consola o que viene en --contrasena. Con
// --solo-medicamentos y --email no se pide: no se toca ninguna cuenta de la demo.
var tocaCuentasDemo = !soloMedicamentos || cuentaPropia is null;

if (!EsApiLocal(baseUrl) && tocaCuentasDemo)
{
    if (!contrasenaIndicada)
    {
        contrasena = LeerContrasena("las cuentas de la demo");
        contrasenaPorConsola = true;
    }

    if (contrasena.Length < 8 || contrasena == contrasenaDemo)
    {
        Morir("Contra una API que no es la de esta maquina, las cuentas de la demo necesitan una " +
              "contrasena propia de al menos 8 caracteres: la de la demo es publica.");
    }
}

using var api = new ApiCliente(baseUrl);

Escribir("ChronoSalud - datos de prueba", ConsoleColor.White);
Escribir($"API: {baseUrl}", ConsoleColor.DarkGray);

// --- 1. Administrador ------------------------------------------------------

Paso("Administrador");
string tokenAdmin;

if (cuentaPropia is null)
{
    tokenAdmin = await ResolverAdministradorDemoAsync();
}
else
{
    tokenAdmin = await IniciarSesionAsync(cuentaPropia, contrasenaCuenta);
}

// --- 1b. Medicamentos: el vademecum de los CSV de data/ --------------------

Paso("Medicamentos");

List<Seed.MedicamentoCsv> vademecum;
try
{
    vademecum = Seed.MedicamentoCsvReader.LeerCarpeta(Path.Combine(AppContext.BaseDirectory, "data"));
}
catch (Exception ex) when (ex is IOException or InvalidDataException)
{
    Morir($"No se pudieron leer los CSV de medicamentos: {ex.Message}");
    return 1;
}

var listadoMedicamentos = await api.GetAsync("/medicamentos", tokenAdmin);
if (!listadoMedicamentos.Ok)
{
    Morir($"No se pudieron listar los medicamentos existentes (HTTP {listadoMedicamentos.Estado}): " +
          $"{listadoMedicamentos.Error}");
}

// Lo que ya hay en la API, con la misma clave que usa el lector de CSV.
var medicamentosCargados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

if (Json.Buscar(listadoMedicamentos.Datos, out var loteMedicamentos, "medicamentos")
    && loteMedicamentos.ValueKind == JsonValueKind.Array)
{
    foreach (var item in loteMedicamentos.EnumerateArray())
    {
        medicamentosCargados.Add(ClaveMedicamento(
            Json.Texto(item, "nombre"),
            Json.Texto(item, "nombreGenerico", "nombre_generico"),
            Json.Texto(item, "concentracion"),
            Json.Texto(item, "formaFarmaceutica", "forma_farmaceutica")));
    }
}

var medicamentosNuevos = 0;
var medicamentosSaltados = 0;

foreach (var med in vademecum)
{
    var clave = ClaveMedicamento(med.NombreComercial, med.NombreGenerico, med.Concentracion, med.FormaFarmaceutica);

    // Add devuelve false si la clave ya estaba, asi que tampoco se repite
    // dentro de la misma corrida.
    if (!medicamentosCargados.Add(clave))
    {
        medicamentosSaltados++;
        continue;
    }

    var alta = await api.PostAsync("/medicamentos", new
    {
        Nombre = med.NombreComercial,
        med.NombreGenerico,
        med.Concentracion,
        med.FormaFarmaceutica,
        med.Laboratorio
    }, tokenAdmin);

    if (!alta.Ok)
    {
        Morir($"No se pudo cargar el medicamento {med.NombreComercial} {med.Concentracion} " +
              $"(HTTP {alta.Estado}): {alta.Error}" +
              (alta.Estado == 403 ? ". La cuenta tiene que ser administrador o doctor." : ""));
    }

    // Resguardo, solo con el primero: una API anterior a los campos del
    // vademecum acepta el POST pero guarda nada mas que el nombre. Esas filas
    // no coincidirian con la clave en la proxima corrida y se duplicarian.
    if (medicamentosNuevos == 0)
    {
        var idNuevo = Json.Entero(alta.Datos, "id_medicamento", "idMedicamento");
        var releido = await api.GetAsync($"/medicamentos/{idNuevo}", tokenAdmin);

        if (!releido.Ok || !string.Equals(
                Json.Texto(releido.Datos, "nombreGenerico", "nombre_generico"),
                med.NombreGenerico,
                StringComparison.OrdinalIgnoreCase))
        {
            Morir($"La API en {baseUrl} todavia no guarda los datos del vademecum (falta desplegar " +
                  $"la version nueva o aplicar la migracion). Quedo cargado el medicamento {idNuevo} " +
                  "sin esos datos: borralo antes de volver a correr el seeder.");
        }
    }

    medicamentosNuevos++;
}

// Marcador de la opcion "Otro..." de la receta: la unica fila de catalogo a la
// que apuntan los medicamentos escritos a mano (el nombre real va en las
// indicaciones). El front lo reconoce por este nombre exacto. Va despues del
// vademecum para no ser nunca el primer alta, que es la que usa el resguardo
// de arriba.
const string nombreMarcadorOtro = "Otro (ver indicaciones)";

if (medicamentosCargados.Add(ClaveMedicamento(nombreMarcadorOtro, "", "", "")))
{
    var altaMarcador = await api.PostAsync("/medicamentos", new
    {
        Nombre = nombreMarcadorOtro,
        Descripcion = "Marcador de la opcion \"Otro...\" de las recetas. No borrar ni renombrar."
    }, tokenAdmin);

    if (!altaMarcador.Ok)
    {
        Morir($"No se pudo cargar el marcador \"{nombreMarcadorOtro}\" (HTTP {altaMarcador.Estado}): " +
              $"{altaMarcador.Error}");
    }

    medicamentosNuevos++;
}
else
{
    medicamentosSaltados++;
}

if (medicamentosNuevos > 0)
{
    Alta($"{medicamentosNuevos} medicamento(s) cargado(s)");
}

if (medicamentosSaltados > 0)
{
    Ya($"{medicamentosSaltados} medicamento(s) ya estaban cargados");
}

if (soloMedicamentos)
{
    Console.WriteLine();
    Escribir($"Medicamentos: {medicamentosNuevos} nuevo(s), {medicamentosSaltados} ya existente(s).",
        ConsoleColor.White);
    Escribir("El seeder es idempotente: podes volver a correrlo sin duplicar nada.\n", ConsoleColor.DarkGray);
    return 0;
}

// --- 2. Doctores: usuario + perfil con especialidad ------------------------

Paso("Doctores");
var idsDoctor = new Dictionary<string, int>();

foreach (var doctor in DatosDemo.Doctores)
{
    // El alta de un doctor la tiene que pedir un administrador.
    var cuenta = await ResolverUsuarioAsync(doctor.Persona, "doctor", tokenAdmin);

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

// --- 2b. Horarios laborales: lunes a viernes, manana o tarde ---------------

Paso("Horarios");

// Los doctores demo alternan manana/tarde segun su orden en DatosDemo (los
// turnos demo estan armados para eso). El resto alterna en orden de listado.
var turnoManiana = new Dictionary<int, bool>();
for (var i = 0; i < DatosDemo.Doctores.Length; i++)
{
    turnoManiana[idsDoctor[DatosDemo.Doctores[i].Persona.Alias]] = i % 2 == 0;
}

var doctoresActivos = new List<(int Id, string Nombre)>();
var paginaDoctores = 1;

while (true)
{
    var listado = await api.GetAsync($"/doctores?pagina={paginaDoctores}&limite=100", tokenAdmin);
    if (!listado.Ok)
    {
        Morir($"No se pudieron listar los doctores (HTTP {listado.Estado}): {listado.Error}");
    }

    var total = Json.Entero(listado.Datos, "total");
    var cantidad = 0;

    if (Json.Buscar(listado.Datos, out var lote, "doctores") && lote.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in lote.EnumerateArray())
        {
            cantidad++;
            doctoresActivos.Add((Json.Entero(item, "idDoctor", "id_doctor"), Json.Texto(item, "nombre")));
        }
    }

    if (cantidad == 0 || paginaDoctores * 100 >= total)
    {
        break;
    }

    paginaDoctores++;
}

var otrosDoctores = 0;

foreach (var (idDoctor, nombre) in doctoresActivos)
{
    var actual = await api.GetAsync($"/doctores/{idDoctor}/horarios", tokenAdmin);
    if (!actual.Ok)
    {
        Morir($"No se pudo consultar el horario de {nombre} (HTTP {actual.Estado}): {actual.Error}");
    }

    if (actual.Datos.ValueKind == JsonValueKind.Array && actual.Datos.GetArrayLength() > 0)
    {
        Ya($"{nombre} ya tenia horario cargado");
        continue;
    }

    if (!turnoManiana.TryGetValue(idDoctor, out var maniana))
    {
        maniana = otrosDoctores % 2 == 0;
        otrosDoctores++;
    }

    var horaDesde = maniana ? "08:00" : "14:00";
    var horaHasta = maniana ? "14:00" : "20:00";

    // DiaSemana: 1 = lunes ... 5 = viernes.
    var semana = Enumerable.Range(1, 5)
        .Select(dia => new { DiaSemana = dia, HoraInicio = horaDesde, HoraFin = horaHasta })
        .ToArray();

    var carga = await api.PutAsync($"/doctores/{idDoctor}/horarios", new { Horarios = semana }, tokenAdmin);
    if (!carga.Ok)
    {
        Morir($"No se pudo cargar el horario de {nombre} (HTTP {carga.Estado}): {carga.Error}");
    }

    Alta($"{nombre}: lunes a viernes de {horaDesde} a {horaHasta}");
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
    var listado = await api.GetAsync($"/turnos?pagina={pagina}&limite=100", tokenAdmin);
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

    if (pagina * 100 >= total)
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
    var fecha = SumarDiasHabiles(hoy, turno.Dia);
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

var credenciales = new List<(string Rol, string Nombre, string Email, string Detalle)>();

// Con --email no se crea el administrador de la demo.
if (cuentaPropia is null)
{
    credenciales.Add(("administrador", DatosDemo.Administrador.NombreCompleto, DatosDemo.Administrador.Email, ""));
}

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
Escribir(contrasenaPorConsola
    ? "Contrasena de los usuarios de la demo: la que ingresaste por consola."
    : $"Contrasena de los usuarios de la demo: {contrasena}", ConsoleColor.White);
Escribir($"Turnos: {creados} nuevo(s), {saltados} ya existente(s), " +
         $"{DatosDemo.Turnos.Length} en total para la demo.", ConsoleColor.White);
Escribir($"Medicamentos: {medicamentosNuevos} nuevo(s), {medicamentosSaltados} ya existente(s).",
    ConsoleColor.White);
Escribir("El seeder es idempotente: podes volver a correrlo sin duplicar nada.\n", ConsoleColor.DarkGray);

return 0;

// --- Helpers ---------------------------------------------------------------

// Clave de comparacion de un turno: quien lo atiende, quien lo recibe y que dia.
static string Clave(string doctor, string paciente, DateTime fecha)
    => $"{doctor}|{paciente}|{fecha:yyyy-MM-dd}";

// Clave de comparacion de un medicamento, la misma con la que MedicamentoCsvReader
// saca los repetidos. El nombre comercial solo no alcanza: se repite entre
// concentraciones (LENALINOVA 5 MG y LENALINOVA 15 MG son dos medicamentos).
static string ClaveMedicamento(string nombre, string generico, string concentracion, string forma)
    => $"{nombre.Trim()}|{generico.Trim()}|{concentracion.Trim()}|{forma.Trim()}";

// La API es local si la URL apunta a esta misma maquina (localhost, 127.0.0.1
// o ::1). Una URL que no se puede leer se trata como no local.
static bool EsApiLocal(string url)
    => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsLoopback;

// Pide la contrasena sin mostrarla, para que no quede escrita en la terminal.
static string LeerContrasena(string cuenta)
{
    Console.Write($"Contrasena de {cuenta}: ");

    if (Console.IsInputRedirected)
    {
        return Console.ReadLine() ?? string.Empty;
    }

    var texto = new StringBuilder();

    while (true)
    {
        var tecla = Console.ReadKey(intercept: true);

        if (tecla.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (tecla.Key == ConsoleKey.Backspace)
        {
            if (texto.Length > 0)
            {
                texto.Length--;
            }

            continue;
        }

        if (!char.IsControl(tecla.KeyChar))
        {
            texto.Append(tecla.KeyChar);
        }
    }

    Console.WriteLine();
    return texto.ToString();
}

// Los doctores atienden de lunes a viernes: el dia 0 es hoy (o el lunes
// siguiente si hoy es fin de semana) y cada dia extra saltea sabado y domingo.
static DateTime SumarDiasHabiles(DateTime desde, int dias)
{
    var fecha = desde;
    while (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
    {
        fecha = fecha.AddDays(1);
    }

    for (var i = 0; i < dias; i++)
    {
        do
        {
            fecha = fecha.AddDays(1);
        }
        while (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
    }

    return fecha;
}

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

// Entra con una cuenta que ya existe, sin registrar nada.
async Task<string> IniciarSesionAsync(string cuenta, string clave)
{
    var login = await api.PostAsync("/usuarios/login", new
    {
        Email = cuenta,
        Contrasena = clave
    });

    if (!login.Ok)
    {
        if (login.Estado == 0)
        {
            Morir($"No se pudo contactar la API en {baseUrl}. Detalle: {login.Error}");
        }

        Morir($"No se pudo iniciar sesion con {cuenta} (HTTP {login.Estado}). Revisa el email y la contrasena.");
    }

    Ya($"sesion iniciada como {cuenta}");
    return Json.Texto(login.Datos, "token");
}

// Entra como el administrador de la demo o, si no existe, lo registra sin
// sesion: la API solo lo acepta cuando todavia no hay ningun administrador.
async Task<string> ResolverAdministradorDemoAsync()
{
    var admin = DatosDemo.Administrador;

    var login = await api.PostAsync("/usuarios/login", new
    {
        admin.Email,
        Contrasena = contrasena
    });

    if (login.Ok)
    {
        Ya($"administrador {admin.NombreCompleto} <{admin.Email}> ya existia");
        return Json.Texto(login.Datos, "token");
    }

    return (await ResolverUsuarioAsync(admin, "administrador")).Token;
}

// Registra el usuario o, si el email ya estaba, hace login. Para un rol
// distinto de paciente la API pide el token de un administrador.
async Task<(int IdUsuario, string Token, bool Creado)> ResolverUsuarioAsync(
    Persona persona, string rol, string? token = null)
{
    var alta = await api.PostAsync("/usuarios/registro", new
    {
        persona.Nombre,
        persona.Apellido,
        persona.Email,
        Contrasena = contrasena,
        persona.Telefono,
        Rol = rol
    }, token);

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

        // La API no deja crear esa cuenta: sin sesion, porque ya hay algun
        // administrador; con sesion, porque la cuenta no es de un administrador.
        if (alta.Estado is 401 or 403)
        {
            Morir(token is null
                ? "Ya hay administradores: corre el seeder con --email <cuenta de un administrador>."
                : $"La API no acepto el alta de {persona.Email} (HTTP {alta.Estado}): " +
                  "la cuenta de --email tiene que ser de un administrador.");
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
        Morir($"El usuario {persona.Email} ya existe pero su contrasena no es " +
              (contrasenaPorConsola ? "la que ingresaste" : $"'{contrasena}'") +
              ". Borra ese usuario o corre el seeder con --contrasena.");
    }

    Ya($"{rol} {persona.NombreCompleto} <{persona.Email}> ya existia");
    return (Json.Entero(login.Datos, "idUsuario", "id_usuario"), Json.Texto(login.Datos, "token"), false);
}
