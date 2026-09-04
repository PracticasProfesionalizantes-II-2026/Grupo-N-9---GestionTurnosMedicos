# Compila wwwroot/css/app.css -> wwwroot/css/app.build.css usando el CLI
# standalone de Tailwind (un .exe suelto, no hace falta Node ni npm).
#
#   .\tools\css.ps1           compila una vez, minificado
#   .\tools\css.ps1 -Watch    queda escuchando y recompila al guardar
#
# La primera vez baja el .exe (~112 MB), que esta ignorado por git.

param([switch]$Watch)

$ErrorActionPreference = "Stop"

$version = "v4.3.3"
$raiz    = Split-Path $PSScriptRoot -Parent
$exe     = Join-Path $PSScriptRoot "tailwindcss.exe"

if (-not (Test-Path $exe)) {
    Write-Host "Descargando el CLI de Tailwind $version..."
    $url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$version/tailwindcss-windows-x64.exe"
    Invoke-WebRequest -Uri $url -OutFile $exe
}

$entrada = Join-Path $raiz "wwwroot\css\app.css"
$salida  = Join-Path $raiz "wwwroot\css\app.build.css"

if ($Watch) {
    & $exe -i $entrada -o $salida --watch
} else {
    & $exe -i $entrada -o $salida --minify
}
