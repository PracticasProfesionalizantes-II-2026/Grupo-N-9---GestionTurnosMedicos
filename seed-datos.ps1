<#
.SYNOPSIS
    Carga datos de prueba en la API de ChronoSalud para la demo.

.DESCRIPTION
    Crea un administrador, 4 doctores (con su perfil y especialidad), 6 pacientes
    y 15 turnos repartidos entre hoy y los proximos 7 dias, y despues reparte los
    estados (pendiente / confirmado / completado / cancelado) con el token del
    administrador.

    Es idempotente: se apoya en lo que ya existe en la API en vez de crear a ciegas.
    Si se corre dos veces no duplica nada.
      - Usuarios  -> el registro devuelve 409 si el email ya existe; ahi hace login.
      - Doctores  -> consulta GET /doctores/me con el token del propio doctor.
      - Pacientes -> consulta GET /pacientes/me (la API crea la ficha en el registro).
      - Turnos    -> lista GET /turnos y compara por doctor + paciente + fecha.

.PARAMETER BaseUrl
    Raiz de la API. Por defecto http://localhost:5001 (perfil "http" de launchSettings).

.PARAMETER Contrasena
    Contrasena para todos los usuarios de prueba. Minimo 8 caracteres (lo valida la API).

.EXAMPLE
    .\seed-datos.ps1
    .\seed-datos.ps1 -BaseUrl http://localhost:5001 -Contrasena "Chrono2026!"
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5001',
    [string]$Contrasena = 'Chrono2026!'
)

$ProgressPreference = 'SilentlyContinue'
$BaseUrl = $BaseUrl.TrimEnd('/')

# Sin esto la consola de Windows PowerShell rompe los acentos de las especialidades.
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

# --- Helpers -----------------------------------------------------------------

function Escribir-Paso { param([string]$Texto) Write-Host "`n== $Texto" -ForegroundColor Cyan }
function Escribir-Alta { param([string]$Texto) Write-Host "   + $Texto" -ForegroundColor Green }
function Escribir-Ya   { param([string]$Texto) Write-Host "   = $Texto" -ForegroundColor DarkGray }

function Salir-ConError {
    param([string]$Texto)
    Write-Host ''
    Write-Host "ERROR: $Texto" -ForegroundColor Red
    exit 1
}

<#
    Envoltorio de Invoke-RestMethod que no tira excepciones: devuelve siempre un
    objeto con Ok / Status / Data / Error. Hace falta porque el script necesita
    distinguir un 409 esperable ("ya existe") de una falla real.
#>
function Invoke-Api {
    param(
        [Parameter(Mandatory)][ValidateSet('GET', 'POST', 'PUT', 'DELETE')][string]$Metodo,
        [Parameter(Mandatory)][string]$Ruta,
        $Cuerpo,
        [string]$Token
    )

    $headers = @{ Accept = 'application/json' }
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }

    $parametros = @{
        Method      = $Metodo
        Uri         = "$BaseUrl$Ruta"
        Headers     = $headers
        ErrorAction = 'Stop'
    }

    if ($null -ne $Cuerpo) {
        $json = $Cuerpo | ConvertTo-Json -Depth 6 -Compress
        # Como bytes UTF-8: si se manda el string pelado, PS 5.1 lo codifica en
        # ANSI y llegan las especialidades y los apellidos con acentos rotos.
        $parametros.Body        = [System.Text.Encoding]::UTF8.GetBytes($json)
        $parametros.ContentType = 'application/json; charset=utf-8'
    }

    try {
        $datos = Invoke-RestMethod @parametros
        return [pscustomobject]@{ Ok = $true; Status = 200; Data = $datos; Error = $null }
    }
    catch {
        $status    = 0
        $mensaje   = $_.Exception.Message
        $respuesta = $_.Exception.Response

        if ($respuesta) {
            try { $status = [int]$respuesta.StatusCode } catch { }

            $texto = $null
            if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
                $texto = $_.ErrorDetails.Message          # PS 7, y a veces 5.1
            }
            elseif ($respuesta.GetResponseStream) {
                # PS 5.1: hay que leer el cuerpo del WebException a mano.
                try {
                    $flujo  = $respuesta.GetResponseStream()
                    $lector = New-Object System.IO.StreamReader($flujo, [System.Text.Encoding]::UTF8)
                    $texto  = $lector.ReadToEnd()
                    $lector.Dispose()
                } catch { }
            }

            if ($texto) {
                try {
                    $json = $texto | ConvertFrom-Json
                    if ($json.error) { $mensaje = $json.error } else { $mensaje = $texto }
                } catch { $mensaje = $texto }
            }
        }

        return [pscustomobject]@{ Ok = $false; Status = $status; Data = $null; Error = $mensaje }
    }
}

<#
    Registra el usuario o, si el email ya estaba, hace login.
    Devuelve @{ IdUsuario; Token; Creado }.
#>
function Resolver-Usuario {
    param(
        [Parameter(Mandatory)][hashtable]$Persona,
        [Parameter(Mandatory)][string]$Rol
    )

    $alta = Invoke-Api -Metodo POST -Ruta '/usuarios/registro' -Cuerpo @{
        nombre     = $Persona.Nombre
        apellido   = $Persona.Apellido
        email      = $Persona.Email
        contrasena = $Contrasena
        telefono   = $Persona.Telefono
        rol        = $Rol
    }

    if ($alta.Ok) {
        Escribir-Alta "$Rol $($Persona.Nombre) $($Persona.Apellido) <$($Persona.Email)>"
        return @{ IdUsuario = [int]$alta.Data.idUsuario; Token = $alta.Data.token; Creado = $true }
    }

    if ($alta.Status -ne 409) {
        if ($alta.Status -eq 0) {
            Salir-ConError "No se pudo contactar la API en $BaseUrl. Verifica que este corriendo (dotnet run). Detalle: $($alta.Error)"
        }
        Salir-ConError "No se pudo registrar $($Persona.Email) (HTTP $($alta.Status)): $($alta.Error)"
    }

    # 409: el email ya existe, entramos con la contrasena del script.
    $login = Invoke-Api -Metodo POST -Ruta '/usuarios/login' -Cuerpo @{
        email      = $Persona.Email
        contrasena = $Contrasena
    }

    if (-not $login.Ok) {
        Salir-ConError "El usuario $($Persona.Email) ya existe pero su contrasena no es '$Contrasena'. Borra ese usuario o corre el script con -Contrasena."
    }

    Escribir-Ya "$Rol $($Persona.Nombre) $($Persona.Apellido) <$($Persona.Email)> ya existia"
    return @{ IdUsuario = [int]$login.Data.idUsuario; Token = $login.Data.token; Creado = $false }
}

# --- Datos de la demo --------------------------------------------------------

$administrador = @{
    Nombre = 'Admin'; Apellido = 'ChronoSalud'
    Email  = 'admin@chronosalud.demo'; Telefono = '11-4000-0000'
}

$doctores = @(
    @{ Alias = 'gomez';   Nombre = 'Laura';  Apellido = 'Gomez';   Email = 'laura.gomez@chronosalud.demo';   Telefono = '11-4000-0101'; Especialidad = 'Clínica';       Matricula = 'MP-10001'; Consultorio = 'Consultorio 101' }
    @{ Alias = 'rivas';   Nombre = 'Martin'; Apellido = 'Rivas';   Email = 'martin.rivas@chronosalud.demo';  Telefono = '11-4000-0102'; Especialidad = 'Cardiología';   Matricula = 'MP-10002'; Consultorio = 'Consultorio 202' }
    @{ Alias = 'ferrari'; Nombre = 'Sofia';  Apellido = 'Ferrari'; Email = 'sofia.ferrari@chronosalud.demo'; Telefono = '11-4000-0103'; Especialidad = 'Traumatología'; Matricula = 'MP-10003'; Consultorio = 'Consultorio 303' }
    @{ Alias = 'cabrera'; Nombre = 'Diego';  Apellido = 'Cabrera'; Email = 'diego.cabrera@chronosalud.demo'; Telefono = '11-4000-0104'; Especialidad = 'Pediatría';     Matricula = 'MP-10004'; Consultorio = 'Consultorio 404' }
)

$pacientes = @(
    @{ Alias = 'duarte'; Nombre = 'Ana';      Apellido = 'Duarte'; Email = 'ana.duarte@chronosalud.demo';     Telefono = '11-5000-0201' }
    @{ Alias = 'salas';  Nombre = 'Bruno';    Apellido = 'Salas';  Email = 'bruno.salas@chronosalud.demo';    Telefono = '11-5000-0202' }
    @{ Alias = 'ponce';  Nombre = 'Carla';    Apellido = 'Ponce';  Email = 'carla.ponce@chronosalud.demo';    Telefono = '11-5000-0203' }
    @{ Alias = 'ruiz';   Nombre = 'Elena';    Apellido = 'Ruiz';   Email = 'elena.ruiz@chronosalud.demo';     Telefono = '11-5000-0204' }
    @{ Alias = 'molina'; Nombre = 'Facundo';  Apellido = 'Molina'; Email = 'facundo.molina@chronosalud.demo'; Telefono = '11-5000-0205' }
    @{ Alias = 'ortiz';  Nombre = 'Gabriela'; Apellido = 'Ortiz';  Email = 'gabriela.ortiz@chronosalud.demo'; Telefono = '11-5000-0206' }
)

<#
    15 turnos de 30 minutos. Ningun doctor tiene dos turnos superpuestos el mismo
    dia (la API rechaza eso con 409), y la terna doctor+paciente+dia no se repite:
    esa es la clave con la que despues detectamos los que ya estaban cargados.
#>
$turnos = @(
    @{ Doctor = 'gomez';   Paciente = 'duarte'; Dia = 0; Desde = '08:00'; Hasta = '08:30'; Estado = 'completado'; Obs = 'Control anual' }
    @{ Doctor = 'gomez';   Paciente = 'salas';  Dia = 0; Desde = '08:30'; Hasta = '09:00'; Estado = 'completado'; Obs = 'Renovacion de receta' }
    @{ Doctor = 'rivas';   Paciente = 'ponce';  Dia = 0; Desde = '09:00'; Hasta = '09:30'; Estado = 'completado'; Obs = 'Lectura de electrocardiograma' }
    @{ Doctor = 'ferrari'; Paciente = 'ruiz';   Dia = 0; Desde = '10:00'; Hasta = '10:30'; Estado = 'confirmado'; Obs = 'Dolor lumbar' }
    @{ Doctor = 'cabrera'; Paciente = 'molina'; Dia = 1; Desde = '09:00'; Hasta = '09:30'; Estado = 'confirmado'; Obs = 'Control de crecimiento' }
    @{ Doctor = 'gomez';   Paciente = 'ortiz';  Dia = 1; Desde = '11:00'; Hasta = '11:30'; Estado = 'confirmado'; Obs = 'Chequeo general' }
    @{ Doctor = 'rivas';   Paciente = 'duarte'; Dia = 1; Desde = '15:00'; Hasta = '15:30'; Estado = 'cancelado';  Obs = 'Reprogramar a pedido del paciente' }
    @{ Doctor = 'ferrari'; Paciente = 'salas';  Dia = 2; Desde = '08:30'; Hasta = '09:00'; Estado = 'confirmado'; Obs = 'Post operatorio de rodilla' }
    @{ Doctor = 'cabrera'; Paciente = 'ponce';  Dia = 2; Desde = '12:00'; Hasta = '12:30'; Estado = 'pendiente';  Obs = 'Primera consulta' }
    @{ Doctor = 'gomez';   Paciente = 'ruiz';   Dia = 3; Desde = '09:30'; Hasta = '10:00'; Estado = 'pendiente';  Obs = 'Resultados de laboratorio' }
    @{ Doctor = 'rivas';   Paciente = 'molina'; Dia = 3; Desde = '16:00'; Hasta = '16:30'; Estado = 'confirmado'; Obs = 'Seguimiento de presion arterial' }
    @{ Doctor = 'ferrari'; Paciente = 'ortiz';  Dia = 4; Desde = '10:30'; Hasta = '11:00'; Estado = 'cancelado';  Obs = 'Cancelado por el consultorio' }
    @{ Doctor = 'cabrera'; Paciente = 'duarte'; Dia = 5; Desde = '08:00'; Hasta = '08:30'; Estado = 'pendiente';  Obs = 'Consulta por alergia' }
    @{ Doctor = 'gomez';   Paciente = 'salas';  Dia = 6; Desde = '14:00'; Hasta = '14:30'; Estado = 'pendiente';  Obs = 'Certificado laboral' }
    @{ Doctor = 'rivas';   Paciente = 'ponce';  Dia = 7; Desde = '17:00'; Hasta = '17:30'; Estado = 'cancelado';  Obs = 'El paciente no confirmo' }
)

# --- 1. Administrador --------------------------------------------------------

Write-Host 'ChronoSalud - datos de prueba' -ForegroundColor White
Write-Host "API: $BaseUrl" -ForegroundColor DarkGray

Escribir-Paso 'Administrador'
$cuentaAdmin = Resolver-Usuario -Persona $administrador -Rol 'administrador'
$tokenAdmin  = $cuentaAdmin.Token

# --- 2 y 3. Doctores: usuario + perfil con especialidad ----------------------

Escribir-Paso 'Doctores'
$idsDoctor = @{}

foreach ($doctor in $doctores) {
    $cuenta = Resolver-Usuario -Persona $doctor -Rol 'doctor'

    # El registro no crea la ficha de doctor (a diferencia de la de paciente),
    # asi que la pedimos con el token del propio doctor y la creamos si falta.
    $perfil = Invoke-Api -Metodo GET -Ruta '/doctores/me' -Token $cuenta.Token

    if ($perfil.Ok) {
        $idsDoctor[$doctor.Alias] = [int]$perfil.Data.idDoctor
        Escribir-Ya "perfil de $($doctor.Especialidad) ya existia (id_doctor $($idsDoctor[$doctor.Alias]))"
        continue
    }

    if ($perfil.Status -ne 404) {
        Salir-ConError "No se pudo consultar el perfil de $($doctor.Email) (HTTP $($perfil.Status)): $($perfil.Error)"
    }

    $alta = Invoke-Api -Metodo POST -Ruta '/doctores' -Token $tokenAdmin -Cuerpo @{
        idUsuario    = $cuenta.IdUsuario
        especialidad = $doctor.Especialidad
        matricula    = $doctor.Matricula
        consultorio  = $doctor.Consultorio
    }

    if (-not $alta.Ok) {
        Salir-ConError "No se pudo crear el perfil de doctor de $($doctor.Email) (HTTP $($alta.Status)): $($alta.Error)"
    }

    # Releemos /doctores/me para no depender de como se serializa id_doctor en el POST.
    $perfil = Invoke-Api -Metodo GET -Ruta '/doctores/me' -Token $cuenta.Token
    if (-not $perfil.Ok) {
        Salir-ConError "El perfil de $($doctor.Email) se creo pero no se pudo releer (HTTP $($perfil.Status)): $($perfil.Error)"
    }

    $idsDoctor[$doctor.Alias] = [int]$perfil.Data.idDoctor
    Escribir-Alta "perfil de $($doctor.Especialidad), matricula $($doctor.Matricula) (id_doctor $($idsDoctor[$doctor.Alias]))"
}

# --- 2. Pacientes: la API crea la ficha sola al registrar el usuario ---------

Escribir-Paso 'Pacientes'
$idsPaciente = @{}

foreach ($paciente in $pacientes) {
    $cuenta = Resolver-Usuario -Persona $paciente -Rol 'paciente'

    $perfil = Invoke-Api -Metodo GET -Ruta '/pacientes/me' -Token $cuenta.Token
    if (-not $perfil.Ok) {
        Salir-ConError "No se pudo leer la ficha de paciente de $($paciente.Email) (HTTP $($perfil.Status)): $($perfil.Error)"
    }

    $idsPaciente[$paciente.Alias] = [int]$perfil.Data.idPaciente
}

# --- 4. Turnos ---------------------------------------------------------------

Escribir-Paso 'Turnos'

# Nombres tal como los arma la API en el listado, para poder comparar.
$nombreDoctor = @{}
foreach ($d in $doctores) { $nombreDoctor[$d.Alias] = "$($d.Nombre) $($d.Apellido)" }

$nombrePaciente = @{}
foreach ($p in $pacientes) { $nombrePaciente[$p.Alias] = "$($p.Nombre) $($p.Apellido)" }

# Traemos los turnos que ya hay (incluidos los cancelados: la baja es logica y
# siguen apareciendo en el listado) y los indexamos por doctor+paciente+dia.
$existentes = @{}
$pagina = 1
do {
    $listado = Invoke-Api -Metodo GET -Ruta "/turnos?pagina=$pagina&limite=200" -Token $tokenAdmin
    if (-not $listado.Ok) {
        Salir-ConError "No se pudieron listar los turnos existentes (HTTP $($listado.Status)): $($listado.Error)"
    }

    $total = [int]$listado.Data.total
    $lote  = @($listado.Data.turnos)

    foreach ($t in $lote) {
        $fecha = ([datetime]$t.fechaInicio).ToString('yyyy-MM-dd')
        $clave = "$($t.doctor)|$($t.paciente)|$fecha"
        if (-not $existentes.ContainsKey($clave)) { $existentes[$clave] = $t }
    }

    $vistos = $pagina * 200
    $pagina++
} while ($lote.Count -gt 0 -and $vistos -lt $total)

$hoy      = (Get-Date).Date
$creados  = 0
$saltados = 0
$aRevisar = @()   # turnos cuyo estado hay que acomodar en el paso 5

foreach ($turno in $turnos) {
    $fecha = $hoy.AddDays($turno.Dia)
    $clave = "$($nombreDoctor[$turno.Doctor])|$($nombrePaciente[$turno.Paciente])|$($fecha.ToString('yyyy-MM-dd'))"

    if ($existentes.ContainsKey($clave)) {
        $ya = $existentes[$clave]
        $aRevisar += @{ Id = [int]$ya.idTurno; EstadoActual = [string]$ya.estado; EstadoDeseado = $turno.Estado }
        $saltados++
        continue
    }

    $alta = Invoke-Api -Metodo POST -Ruta '/turnos' -Token $tokenAdmin -Cuerpo @{
        idPaciente    = $idsPaciente[$turno.Paciente]
        idDoctor      = $idsDoctor[$turno.Doctor]
        # Sin zona horaria y a las 00:00: la hora del turno viaja aparte, en horaInicio.
        fechaInicio   = $fecha.ToString('yyyy-MM-ddTHH:mm:ss')
        horaInicio    = $turno.Desde
        horaFin       = $turno.Hasta
        observaciones = $turno.Obs
    }

    if (-not $alta.Ok) {
        Salir-ConError "No se pudo crear el turno de $($nombrePaciente[$turno.Paciente]) con $($nombreDoctor[$turno.Doctor]) el $($fecha.ToString('dd/MM')) (HTTP $($alta.Status)): $($alta.Error)"
    }

    $aRevisar += @{ Id = [int]$alta.Data.id_turno; EstadoActual = 'pendiente'; EstadoDeseado = $turno.Estado }
    $creados++
    Escribir-Alta "$($fecha.ToString('dd/MM')) $($turno.Desde) - $($nombrePaciente[$turno.Paciente]) con $($nombreDoctor[$turno.Doctor])"
}

if ($saltados -gt 0) { Escribir-Ya "$saltados turno(s) ya estaban cargados" }

# --- 5. Estados, con el token del administrador ------------------------------

Escribir-Paso 'Estados'
$actualizados = 0

foreach ($item in $aRevisar) {
    if ($item.EstadoActual -eq $item.EstadoDeseado) { continue }

    # Mandamos solo el estado: si no viaja fecha ni hora, la API no revalida
    # conflictos de agenda, que es justo lo que queremos aca.
    $cambio = Invoke-Api -Metodo PUT -Ruta "/turnos/$($item.Id)" -Token $tokenAdmin -Cuerpo @{
        estado = $item.EstadoDeseado
    }

    if (-not $cambio.Ok) {
        Salir-ConError "No se pudo pasar el turno $($item.Id) a '$($item.EstadoDeseado)' (HTTP $($cambio.Status)): $($cambio.Error)"
    }

    $actualizados++
}

Write-Host "   $actualizados turno(s) cambiaron de estado, $($aRevisar.Count - $actualizados) ya estaban bien" -ForegroundColor DarkGray

foreach ($grupo in ($turnos | Group-Object { $_.Estado } | Sort-Object Name)) {
    Write-Host ("   {0,-11} {1}" -f $grupo.Name, $grupo.Count) -ForegroundColor DarkGray
}

# --- 6. Credenciales ---------------------------------------------------------

$credenciales = @()

$credenciales += [pscustomobject]@{
    Rol        = 'administrador'
    Nombre     = "$($administrador.Nombre) $($administrador.Apellido)"
    Email      = $administrador.Email
    Contrasena = $Contrasena
    Detalle    = ''
}

foreach ($d in $doctores) {
    $credenciales += [pscustomobject]@{
        Rol        = 'doctor'
        Nombre     = "$($d.Nombre) $($d.Apellido)"
        Email      = $d.Email
        Contrasena = $Contrasena
        Detalle    = "$($d.Especialidad) - $($d.Matricula)"
    }
}

foreach ($p in $pacientes) {
    $credenciales += [pscustomobject]@{
        Rol        = 'paciente'
        Nombre     = "$($p.Nombre) $($p.Apellido)"
        Email      = $p.Email
        Contrasena = $Contrasena
        Detalle    = ''
    }
}

Write-Host "`n== Credenciales" -ForegroundColor Cyan
$credenciales | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

Write-Host "Turnos: $creados nuevo(s), $saltados ya existente(s), $($turnos.Count) en total para la demo." -ForegroundColor White
Write-Host "El script es idempotente: podes volver a correrlo sin duplicar nada.`n" -ForegroundColor DarkGray
