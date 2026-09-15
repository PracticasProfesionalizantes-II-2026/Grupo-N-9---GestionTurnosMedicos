# Grupo-N-9---GestionTurnosMedicos

Bujonok Francisco, Palmero Ivo, Perez Facundo

- ChronoSalud -

Sistema de gestión de turnos médicos. El repo tiene tres partes:

| Carpeta                              | Qué es                                                    |
| ------------------------------------ | --------------------------------------------------------- |
| [`ChronoSaludApi/`](ChronoSaludApi/) | API REST en .NET 10 (minimal APIs, Entity Framework, JWT) |
| [`ChronoSaludWeb/`](ChronoSaludWeb/) | Frontend en ASP.NET Core MVC (Razor + Tailwind)           |
| [`tools/Seed/`](tools/Seed/)         | Consola que carga datos de prueba en la API               |           |

## Levantar todo de una vez

```
dotnet run --project tools/Seed
```

[`dotnet run --project tools/Seed`](dotnet run --project tools/Seed) inicia la base, la API y el frontend, y abre el navegador.
Deja dos ventanas abiertas (una por servicio); para frenar todo, se cierran.

El comando va en cualquier terminal de PowerShell: sirve la de VS Code (`Ctrl + Ñ`),
parada en la carpeta del proyecto.

Usa la instancia **SQL Server** de la máquina: la misma que aparece en SQL Server
Management Studio al conectarse a `localhost`, así que los cambios que haga la
aplicación se ven ahí. Si no encuentra la instancia por defecto, usa **SQL Server Express** (`localhost\SQLEXPRESS`).

Ese servicio arranca en modo manual, así que cuando está detenido el script pide permiso
de administrador para iniciarlo y Windows muestra un cartel. Para que arranque solo junto
con Windows y no volver a ver ese cartel, alcanza con correrlo una vez así:

```
powershell -ExecutionPolicy Bypass -File levantar.ps1 -ArranqueAutomatico
```

La primera vez hay que crear la base: ver el paso 1 de abajo.

## Puesta en marcha paso a paso

Requisitos: **.NET 10 SDK** y **SQL Server** (sirve LocalDB, que viene con Visual Studio).

### 1. Base de datos

La cadena de conexión por defecto está en
[`ChronoSaludApi/appsettings.Development.json`](ChronoSaludApi/appsettings.Development.json)
y apunta a `Server=localhost`, la instancia por defecto de SQL Server.

Si en tu máquina tenés **SQL Server Express** (una instancia con nombre), no edites ese
archivo: guardá tu cadena en los user secrets, que quedan fuera del repo y no le pisan la
configuración a nadie.

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=ChronoSaludDB;Trusted_Connection=True;TrustServerCertificate=True;" --project ChronoSaludApi
```

Después, crear la base con las migraciones:

```
dotnet ef database update --project ChronoSaludApi
```

Si no tenés la herramienta: `dotnet tool install --global dotnet-ef`.

### 2. Levantar la API

```
dotnet run --project ChronoSaludApi
```

Queda en `http://localhost:5001`. La documentación interactiva de los endpoints está en
`http://localhost:5001/scalar`.

### 3. Levantar el frontend

En **otra** terminal:

```
dotnet run --project ChronoSaludWeb
```

Queda en `http://localhost:5044`.

### 4. Datos de prueba

Con la API corriendo, en otra terminal:

```
dotnet run --project tools/Seed
```

Crea un administrador, 4 doctores, 6 pacientes y 15 turnos repartidos entre hoy y los
próximos 7 días. La contraseña de todos es `Chrono2026!` y el admin es
`admin@chronosalud.demo`.

Es idempotente: se puede correr las veces que haga falta sin duplicar nada.
## Ver la base desde SQL Server Management Studio

Abrir SSMS y conectarse con:

- **Server name:** `localhost` (o `localhost\SQLEXPRESS` si tenés SQL Server Express)
- **Authentication:** Windows Authentication

La base está en *Databases → ChronoSaludDB*. Es la que usa la aplicación por defecto, así
que lo que se cargue desde el frontend aparece ahí al refrescar (`F5` sobre la tabla).

Si en algún momento levantaron el proyecto con `-LocalDb`, esos datos están en otra
instancia y para verlos hay que conectarse a `(localdb)\MSSQLLocalDB`.
## Estilos

El CSS se compila de `wwwroot/css/app.css` a `wwwroot/css/app.build.css` con el CLI
de Tailwind:

```
dotnet build ChronoSaludWeb -t:CompilarCss
dotnet build ChronoSaludWeb -t:CssWatch
```

La primera vez descarga el ejecutable de Tailwind (~112 MB), que está ignorado por git.
`app.build.css` sí está versionado, así que solo hace falta compilar si tocás los estilos.

## Problemas frecuentes

- **Error de SQL al llamar la API, usando LocalDB:** LocalDB se apaga solo tras un rato de
  inactividad. Se levanta con `sqllocaldb start MSSQLLocalDB`.
- **"El archivo se ha bloqueado por: ChronoSaludApi" al compilar:** quedó una instancia
  corriendo de una ejecución anterior. Cerrarla con `Stop-Process -Name ChronoSaludApi -Force`.

## Documentación

Word del TP: https://docs.google.com/document/d/1aFjYJ_MKdPjXtJHxsPpquCxD33VihGDwmP24AzYSO0Q/edit?usp=sharing
(Es el mismo word que el descargado de abajo. El link está por si ocurre algún error).

Clases: https://app.diagrams.net/#G1U2z9lki67BpNLmjgS-AQfg_hLP1Sniiz#%7B%22pageId%22%3A%22FsB0bT2FR8Ba7luLeEG5%22%7D

Casos de uso: https://app.diagrams.net/#G1YW4e_ZWlW5Z6w7kvaXftI60h9eKUpvRK#%7B%22pageId%22%3A%225O-YL7p3ES3qIqL0S2sI%22%7D

Documentación API: https://docs.google.com/document/d/1bl_0oJ8eveKUI9GjHCkyexxb0NF7L5K0wd8yWQ62T9w/edit?tab=t.0
