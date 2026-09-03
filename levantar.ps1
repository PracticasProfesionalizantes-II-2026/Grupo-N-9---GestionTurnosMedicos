# Levanta ChronoSalud completo: base de datos, API y frontend.
#
#   Uso:  .\levantar.ps1
#
# Abre dos ventanas nuevas (una para la API y otra para el frontend) y el
# navegador en http://localhost:5500. Para frenar todo, cerrá esas dos ventanas.
#
# Usa la instancia SQL Server de la maquina (localhost), que es la que se ve en
# SQL Server Management Studio. Si en su lugar preferis LocalDB, agregale -LocalDb.

param(
  [switch]$LocalDb,
  [int]$PuertoApi = 5001,
  [int]$PuertoFront = 5500
)

$raiz = $PSScriptRoot
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "  ChronoSalud" -ForegroundColor Cyan
Write-Host "  ==========="
Write-Host ""

# ── 1. Cerrar una API vieja que haya quedado dando vueltas ─────────────────
# Si no se cierra, bloquea el .exe al compilar y ocupa el puerto.
$vieja = Get-Process -Name "ChronoSaludApi" -ErrorAction SilentlyContinue
if ($vieja) {
  Write-Host "  Cerrando API anterior (PID $($vieja.Id -join ', '))..." -ForegroundColor Yellow
  $vieja | Stop-Process -Force
  Start-Sleep -Milliseconds 800
}

# ── 2. Base de datos ───────────────────────────────────────────────────────
if ($LocalDb) {
  # LocalDB se apaga solo tras un rato sin uso: se levanta en cada arranque.
  $estado = (sqllocaldb info MSSQLLocalDB | Select-String "State:") -replace ".*State:\s*", ""
  if ($estado -ne "Running") {
    Write-Host "  Iniciando LocalDB..."
    sqllocaldb start MSSQLLocalDB | Out-Null
  }
  $conexion = "Server=(localdb)\MSSQLLocalDB;Database=ChronoSaludDB;Trusted_Connection=True;TrustServerCertificate=True;"
  Write-Host "  Base de datos: LocalDB" -ForegroundColor Green
} else {
  # Instancia por defecto de la maquina: la misma que se ve en SSMS al
  # conectarse a "localhost". Arranca en modo manual, asi que se inicia aca.
  $servicio = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
  if (-not $servicio) {
    Write-Host "  No se encontro el servicio MSSQLSERVER. Proba con: .\levantar.ps1 -LocalDb" -ForegroundColor Red
    exit 1
  }
  if ($servicio.Status -ne "Running") {
    Write-Host "  Iniciando SQL Server..."
    Start-Service -Name "MSSQLSERVER"
  }
  $conexion = "Server=localhost;Database=ChronoSaludDB;Trusted_Connection=True;TrustServerCertificate=True;"
  Write-Host "  Base de datos: SQL Server local (se ve en SSMS como 'localhost')" -ForegroundColor Green
}

# ── 3. API en una ventana aparte ───────────────────────────────────────────
$comandoApi = @"
`$host.UI.RawUI.WindowTitle = 'ChronoSalud - API'
`$env:ConnectionStrings__DefaultConnection = '$conexion'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
Set-Location '$raiz'
Write-Host 'API de ChronoSalud - http://localhost:$PuertoApi' -ForegroundColor Green
Write-Host 'Documentacion: http://localhost:$PuertoApi/scalar'
Write-Host ''
dotnet run --project ChronoSaludApi --no-launch-profile --urls 'http://localhost:$PuertoApi'
"@
Start-Process powershell -ArgumentList "-NoExit", "-Command", $comandoApi
Write-Host "  API          -> http://localhost:$PuertoApi" -ForegroundColor Green

# ── 4. Frontend en otra ventana ────────────────────────────────────────────
$servidor = Join-Path $raiz "chronosalud-front\servir.ps1"
Start-Process powershell -ArgumentList "-NoExit", "-ExecutionPolicy", "Bypass", "-File", $servidor, "-Puerto", $PuertoFront
Write-Host "  Frontend     -> http://localhost:$PuertoFront" -ForegroundColor Green

# ── 5. Esperar a que la API responda y abrir el navegador ──────────────────
Write-Host ""
Write-Host "  Esperando a que compile la API..." -NoNewline

$listo = $false
foreach ($intento in 1..60) {
  Start-Sleep -Seconds 1
  try {
    Invoke-WebRequest -Uri "http://localhost:$PuertoApi/coberturas" -TimeoutSec 2 -UseBasicParsing | Out-Null
    $listo = $true; break
  } catch {
    # Un 401 significa que la API ya esta respondiendo (pide token).
    if ($_.Exception.Response.StatusCode.value__ -eq 401) { $listo = $true; break }
  }
  Write-Host "." -NoNewline
}

Write-Host ""
if ($listo) {
  Write-Host "  Todo listo." -ForegroundColor Green
  Start-Process "http://localhost:$PuertoFront"
} else {
  Write-Host "  La API tardo mas de lo esperado. Mira la ventana 'ChronoSalud - API'." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Para frenar todo, cerra las dos ventanas que se abrieron."
Write-Host ""
