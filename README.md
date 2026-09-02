# Grupo-N-9---GestionTurnosMedicos

Bujonok Francisco, Palmero Ivo, Perez Facundo

- ChronoSalud -

Sistema de gestión de turnos médicos. El repo tiene dos partes:

| Carpeta                                    | Qué es                                                    |
| ------------------------------------------ | --------------------------------------------------------- |
| [`ChronoSaludApi/`](ChronoSaludApi/)       | API REST en .NET 10 (minimal APIs, Entity Framework, JWT) |
| [`chronosalud-front/`](chronosalud-front/) | Frontend web en HTML/CSS/JS puro, sin build                |

## Puesta en marcha

Requisitos: **.NET 10 SDK** y **SQL Server** (sirve LocalDB, que viene con Visual Studio).

### 1. Base de datos

La cadena de conexión está en
[`ChronoSaludApi/appsettings.Development.json`](ChronoSaludApi/appsettings.Development.json)
y apunta a `Server=localhost`. Si usás **LocalDB** en vez de una instancia completa,
cambiala por:

```
Server=(localdb)\\MSSQLLocalDB;Database=ChronoSaludDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Después, crear la base con las migraciones:

```
dotnet ef database update --project ChronoSaludApi
```

Si no tenés la herramienta: `dotnet tool install --global dotnet-ef`.

La base arranca vacía: creá el primer usuario administrador desde la pantalla de registro.

### 2. Levantar la API

```
dotnet run --project ChronoSaludApi
```

Queda en `http://localhost:5001`. La documentación interactiva de los endpoints está en
`http://localhost:5001/scalar`.

### 3. Levantar el frontend

En **otra** terminal:

```
powershell -ExecutionPolicy Bypass -File chronosalud-front\servir.ps1
```

Y abrir `http://localhost:5500`. No sirve abrir el HTML con doble clic: los módulos JS
se bloquean con `file://`.

Más detalle en el [README del frontend](chronosalud-front/README.md).

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
