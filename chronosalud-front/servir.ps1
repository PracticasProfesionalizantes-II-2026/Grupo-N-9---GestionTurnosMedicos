# Servidor web estático para el frontend de ChronoSalud.
# No requiere instalar nada: usa el HttpListener que ya trae Windows.
#
#   Uso:  .\servir.ps1          (queda escuchando en http://localhost:5500)
#         .\servir.ps1 -Puerto 8080
#
# Cortar con Ctrl+C.

param([int]$Puerto = 5500)

$raiz = $PSScriptRoot
$url = "http://localhost:$Puerto/"

$tipos = @{
  ".html" = "text/html; charset=utf-8"
  ".css"  = "text/css; charset=utf-8"
  ".js"   = "text/javascript; charset=utf-8"
  ".json" = "application/json; charset=utf-8"
  ".svg"  = "image/svg+xml"
  ".png"  = "image/png"
  ".jpg"  = "image/jpeg"
  ".ico"  = "image/x-icon"
}

$escucha = New-Object System.Net.HttpListener
$escucha.Prefixes.Add($url)

try {
  $escucha.Start()
} catch {
  Write-Host "No se pudo abrir el puerto $Puerto. Probá con otro: .\servir.ps1 -Puerto 5501" -ForegroundColor Red
  exit 1
}

Write-Host ""
Write-Host "  ChronoSalud frontend corriendo en $url" -ForegroundColor Green
Write-Host "  Sirviendo: $raiz"
Write-Host "  Cortar con Ctrl+C"
Write-Host ""

try {
  while ($escucha.IsListening) {
    $contexto = $escucha.GetContext()
    $pedido = $contexto.Request
    $respuesta = $contexto.Response

    # "/" sirve index.html
    $ruta = $pedido.Url.LocalPath.TrimStart("/")
    if ([string]::IsNullOrEmpty($ruta)) { $ruta = "index.html" }

    $archivo = Join-Path $raiz ($ruta -replace "/", "\")

    # No permitir salir de la carpeta del frontend
    $completa = [System.IO.Path]::GetFullPath($archivo)
    if (-not $completa.StartsWith([System.IO.Path]::GetFullPath($raiz))) {
      $respuesta.StatusCode = 403
      $respuesta.Close()
      continue
    }

    if (Test-Path $completa -PathType Leaf) {
      $bytes = [System.IO.File]::ReadAllBytes($completa)
      $extension = [System.IO.Path]::GetExtension($completa).ToLower()
      $respuesta.ContentType = if ($tipos.ContainsKey($extension)) { $tipos[$extension] } else { "application/octet-stream" }
      $respuesta.ContentLength64 = $bytes.Length
      $respuesta.OutputStream.Write($bytes, 0, $bytes.Length)
      Write-Host "200  $ruta" -ForegroundColor DarkGray
    } else {
      $mensaje = [System.Text.Encoding]::UTF8.GetBytes("404 - No se encontro $ruta")
      $respuesta.StatusCode = 404
      $respuesta.ContentType = "text/plain; charset=utf-8"
      $respuesta.OutputStream.Write($mensaje, 0, $mensaje.Length)
      Write-Host "404  $ruta" -ForegroundColor Yellow
    }

    $respuesta.Close()
  }
} finally {
  $escucha.Stop()
  $escucha.Close()
}
