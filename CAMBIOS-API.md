# Cambios en la API — Mejoras, pasos 2 a 7

**Fecha:** 2026-10-08 · **Ramas:** `fix/pacientes-y-turnos` (paso 2), `fix/turnos-y-listados` (paso 3), `fix/listados-paginados` (pasos 3b y 4), `feature/reglas-de-turnos` (pasos 5 y 6) y `feature/reprogramar-turnos` (paso 7)

Un apartado por cada paso.

---

## Paso 2 — Alta de paciente completa y borrado de datos de la ficha

**Hay migración:** `AgregarDatosAltaPaciente` agrega seis columnas a `Pacientes`, todas admiten nulo y no toca datos. El script se corre en Azure **antes** de desplegar la API: la API nueva lee esas columnas.

### Campos nuevos del paciente

| Campo (JSON) | Largo máximo |
|---|---|
| `tipoDocumento` | 20 (`DNI`, `LC`, `LE`, `Pasaporte`) |
| `provincia` | 80 |
| `localidad` | 100 |
| `codigoPostal` | 10 |
| `contactoEmergenciaNombre` | 120 |
| `contactoEmergenciaTelefono` | 30 |

- Vienen en `GET /pacientes/{id}` y en `GET /pacientes/me`.
- Se pueden mandar en `PUT /pacientes/{id}`.

### `POST /usuarios/registro` acepta la ficha del paciente

```json
{
  "nombre": "Lucía", "apellido": "Gómez", "email": "lucia@demo.com",
  "contrasena": "Clave2026!", "telefono": "11-5000-0000", "rol": "paciente",
  "ficha": { "dni": "30111222", "tipoDocumento": "DNI", "fechaNacimiento": "1990-05-20",
             "provincia": "Córdoba", "localidad": "Villa María" }
}
```

- **Quién la manda:** la ficha solo se tiene en cuenta si el pedido trae el token de un **administrador**. Sin token (el registro público) se ignora, así nadie puede averiguar qué DNI ya están cargados.
- **Cómo se guarda:** la cuenta y el paciente se guardan juntos: o se crean los dos o ninguno.
- **DNI repetido:** `409` con `"Ya existe un paciente registrado con ese DNI."`, y no se crea la cuenta.
- **Respuesta:** suma `idPaciente`.

### `PUT /pacientes/{id}` puede borrar datos

- **Lo de siempre:** un campo que no viene, o que viene vacío, no cambia lo que ya estaba.
- **Nuevo, `borrar`:** la lista de campos a vaciar.
  ```json
  { "localidad": "Villa María", "borrar": ["alergias", "direccion"] }
  ```
  - Nombres válidos (sin importar mayúsculas): `fechaNacimiento`, `sexo`, `grupoSanguineo`, `alergias`, `condiciones`, `direccion`, `nacionalidad`, `estadoCivil`, `tipoDocumento`, `provincia`, `localidad`, `codigoPostal`, `contactoEmergenciaNombre`, `contactoEmergenciaTelefono` y `dni`.
  - Un nombre desconocido → `400` y no se guarda nada.
- **DNI:** un **paciente** no puede cambiar ni borrar un DNI ya cargado (`400`, con el motivo). El personal (doctor o administrador) sí.

### A quién le pega

- Todos los pedidos que andaban siguen andando: todo lo nuevo es opcional.
- Si alguien probaba con Scalar o Postman cambiar el DNI de un paciente usando el token de ese mismo paciente, ahora recibe `400`.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Entidades/Paciente.cs` | Seis campos nuevos |
| `Datos/AppDbContext.cs` | Largo máximo de los campos nuevos |
| `Logica/DTOs/PacienteDTOs.cs` | Campos nuevos en `PacienteDto` y `PacienteUpdateDto`, y `Borrar` |
| `Logica/DTOs/UsuarioDTOs.cs` | `Ficha` en el registro, `IdPaciente` en la respuesta |
| `Logica/FichaPaciente.cs` | Nuevo: copiar los datos de la ficha y borrar campos |
| `Logica/UsuarioLogica.cs` | `Registrar` guarda cuenta y paciente juntos, con la ficha |
| `Logica/PacienteLogica.cs` | `Actualizar` usa `FichaPaciente` y la regla del DNI |
| `Repositorios/UsuarioRepository.cs` | `Agregar` traduce el dato repetido de SQL Server |
| `Repositorios/ErroresDeBase.cs`, `DatoRepetidoException.cs` | Nuevos: reconocer el error de dato repetido |

---

## Paso 3 — El paciente cancela sus turnos

**No hay migración ni cambios en los endpoints.** `DELETE /turnos/{id}` ya aceptaba al paciente sobre sus propios turnos (fase E); lo que cambia es la Web, que ahora le muestra el botón.

### Aviso de migraciones pendientes (solo en Development)

- Al arrancar, la API revisa si la base local está al día con el código:
  - si el modelo cambió y falta generar la migración, avisa con `dotnet ef migrations add <Nombre> --project ChronoSaludApi`;
  - si hay migraciones sin aplicar, las nombra y avisa con `dotnet ef database update --project ChronoSaludApi`.
- El aviso sale en amarillo en la ventana "ChronoSalud - API". No frena el arranque: si la base no responde, también lo avisa y sigue.
- En Azure (Production) no corre.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Datos/AvisoDeMigraciones.cs` | Nuevo: el aviso de arriba |
| `Program.cs` | Lo llama al arrancar, solo en Development |

### Paso 3b — El aviso dice qué base revisó

- Cuando faltan migraciones, el aviso nombra la base y el servidor (por ejemplo, `ChronoSaludDB en localhost`). El comando que sugiere lleva `--connection` con esa misma base: sin eso, `dotnet ef` usa los user-secrets, que pueden apuntar a Azure.
- Si la base pide usuario y contraseña (Azure), el aviso no muestra la cadena: remite al script idempotente en SSMS.
- `AvisoDeMigraciones` suma dos métodos públicos, `ComandoParaActualizar` y `DescribirBase`, que se prueban en `ChronoSalud.Tests/Api/AvisoDeMigracionesTests.cs`.

---

## Paso 4 — Listados ordenados y paginados en la base

**No hay migración.** Solo cambian las consultas: el filtro, el orden, el total y la página ahora los resuelve SQL Server, en lugar de traer todo a memoria y recortar ahí.

### `GET /turnos`

| Parámetro nuevo | Qué hace |
|---|---|
| `estados` | Varios estados separados por coma: `estados=pendiente,confirmado`. Se suma a `estado`, que sigue andando igual. |
| `orden` | `fecha` (día y hora, es el de siempre), `paciente` (nombre y apellido) o `estado`. Cualquier otro valor ordena por fecha. |
| `dir` | `desc` para ir de mayor a menor; cualquier otra cosa, de menor a mayor. |

- **Antes no había un orden fijo;** ahora siempre lo hay. El último criterio es el id del turno, así ninguno se repite ni se saltea entre páginas.
- **`limite` va de 1 a 100.** Si se pide más, se devuelven 100. Si `pagina` es menor que 1, se toma la 1.
- **La respuesta suma `conteos`:** cuántos turnos hay de cada estado con los mismos filtros, sin contar el filtro de estado (son los números de las tarjetas de la Web).
  ```json
  { "total": 3, "turnos": [ ... ],
    "conteos": { "pendiente": 3, "confirmado": 5, "completado": 12, "cancelado": 1 } }
  ```
- `total` cuenta los turnos que cumplen todos los filtros, estado incluido, sumando todas las páginas.

### `GET /pacientes` y `GET /doctores`

- Paginan en la base y vienen ordenados por apellido y nombre.
- `limite` va de 1 a 200 (los desplegables de la Web piden 200).

### `GET /usuarios/{id}/notificaciones`

- Pagina en la base, de a 20, de la más nueva a la más vieja. La respuesta no cambia.

### `/reportes/turnos`, `/reportes/pacientes` y `/reportes/disponibilidad`

- Cuentan en la base en vez de traer todos los turnos. La respuesta no cambia.
- `total_turnos` y `turnos_en_periodo` son la suma de los cuatro estados.

### A quién le pega

- **La Web vieja con la API nueva anda igual:** no manda los parámetros nuevos e ignora `conteos`.
- **La Web nueva con la API vieja** (los minutos del despliegue): las tarjetas de Turnos muestran "—" y el orden no está garantizado hasta que termina de desplegarse la API.
- **El seeder** ahora recorre de a 100. **Ojo:** un seeder viejo (de a 200) contra la API nueva puede creer que ya leyó todos los turnos y duplicar los de la demo. Usá el de esta versión.
- Quien probaba con Scalar o Postman `limite=500` ahora recibe 100 turnos (o 200 pacientes o doctores) por página.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Repositorios/FiltroTurnos.cs` | Nuevo: los filtros del listado de turnos en una clase |
| `Repositorios/TurnoRepository.cs` | `Buscar` (filtro, orden y página en SQL), `ContarPorEstado`, `ContarPacientesAtendidos` y `ContarOcupadosPorDoctor` |
| `Repositorios/PacienteRepository.cs`, `DoctorRepository.cs`, `NotificacionRepository.cs` | Paginan en SQL y devuelven `(total, página)` |
| `Logica/TurnoLogica.cs`, `PacienteLogica.cs`, `DoctorLogica.cs`, `NotificacionLogica.cs` | Usan esos métodos; ya no recortan en memoria |
| `Endpoints/TurnoEndpoints.cs` | Parámetros `estados`, `orden` y `dir`; `conteos` en la respuesta; tope de 100 |
| `Endpoints/PacienteEndpoints.cs`, `DoctorEndpoints.cs` | Tope de 200 |
| `Endpoints/ReporteEndpoints.cs` | Cuentan en la base |

---

## Paso 5 — Reglas de estado, doble reserva y hora de Argentina

**Hay migración:** `EvitarTurnosDuplicados`. Crea un índice único en `Turnos`:

```sql
CREATE UNIQUE INDEX [IX_Turnos_Doctor_Dia_Hora] ON [Turnos] ([IdDoctor], [FechaInicio], [HoraInicio]) WHERE [Estado] <> 'cancelado';
```

- No toca datos, pero **falla si ya hay dos turnos en pie del mismo doctor, el mismo día y a la misma hora**. Antes de correrla en Azure, hay que revisar que no haya ninguno (la consulta está en el resumen del paso).
- Se corre en Azure **antes** del merge, como siempre. Si llegara a quedar para después, no pasa nada grave: la API nueva funciona sin el índice. Lo que se pierde es la protección contra dos reservas del mismo horario al mismo tiempo.

### Estados del turno

Hay un estado nuevo, `ausente`: el paciente no vino. Los cambios permitidos son:

| De | A | Cuándo |
|---|---|---|
| pendiente | confirmado | siempre |
| pendiente o confirmado | completado o ausente | cuando ya llegó la hora del turno |
| pendiente o confirmado | cancelado | siempre (el paciente, solo si el turno no empezó) |
| completado, ausente o cancelado | — | nunca: son finales |

- **`PUT /turnos/{id}` con `estado`:**
  - Se aceptan mayúsculas y espacios: `" Ausente "` se guarda como `ausente`.
  - Un estado que no existe → `400`.
  - Un cambio que no está en la tabla → `409`, con el motivo (por ejemplo, *"Conflicto de estado: todavía no es la hora del turno..."*).
  - Antes se guardaba cualquier texto.
- **Reprogramar** (`PUT` con otra fecha u hora):
  - El turno tiene que estar pendiente o confirmado; si no → `409`.
  - El horario nuevo pasa por las mismas reglas que un turno nuevo: horario del doctor y fecha no pasada → `400` con el motivo. Si choca con otro turno → `409`.
  - Si se manda la misma fecha y hora que ya tenía, no cuenta como reprogramar.

### Horas que ya pasaron (hora de Argentina)

- **`POST /turnos` de un paciente** a una hora de hoy que ya pasó → `400` *"Ese horario ya pasó. Elegí uno más adelante."*. El personal sí puede cargarlo (por ejemplo, alguien que se atendió sin turno).
- **`DELETE /turnos/{id}` de un paciente** sobre un turno que ya empezó → `400`. El personal sí puede.
- **`GET /doctores/{id}/disponibilidad`** usa la hora de Argentina. Antes usaba la del servidor: en Azure (UTC), desde las 21:00 de acá ya era "mañana", y de 9:00 a 12:00 se ofrecían franjas que ya habían pasado.
- **Las notificaciones** guardan la fecha en hora de Argentina.

### Doble reserva

- Si dos pedidos reservan el mismo horario a la vez, el índice único frena el segundo. Responde `409` *"Conflicto de horario: ese horario se acaba de ocupar. Elegí otro."*, en lugar de un `500`.
- Lo mismo al reprogramar.

### Listado e historial

- `GET /turnos` suma `ausente` en `conteos`, y acepta `estado=ausente`.
- Historial de movimientos: acción nueva `turno.ausente`.

### A quién le pega

- **La Web vieja con la API nueva:** si alguien toca "Marcar como completado" en un turno que todavía no empezó, ve el mensaje de la API en vez de completarlo.
- **El seeder:** un turno de la demo que tendría que quedar completado pero todavía no empezó (por ejemplo, porque se carga temprano o un fin de semana) queda confirmado. Si la API no deja cambiar el estado de un turno (por ejemplo, uno de la demo que alguien ya canceló), avisa en amarillo y sigue.
- **Scalar o Postman:** un `PUT` con un estado inventado ahora recibe `400`.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/Reloj.cs` | Nuevo: `IReloj` (la hora actual) y `RelojArgentina` |
| `Logica/EstadosTurno.cs` | Nuevo: los estados y `PuedeCambiar` |
| `Logica/FechaArgentina.cs` | `Ahora()` |
| `Logica/TurnoLogica.cs` | Reglas de estado, reprogramación, horas pasadas, `409` por dato repetido, notificación con hora de Argentina |
| `Logica/HorarioLaboralLogica.cs` | Usa `IReloj` en lugar de `DateTime.Today` y `DateTime.Now` |
| `Repositorios/TurnoRepository.cs` | `Agregar` y `Actualizar` traducen el dato repetido |
| `Repositorios/FiltroTurnos.cs` | `ausente` entre los estados que se cuentan |
| `Datos/AppDbContext.cs` | El índice único (y el de `IdDoctor`, declarado a mano para que no se borre) |
| `Entidades/Movimiento.cs` | `TurnoAusente` |
| `Entidades/Notificacion.cs` | La fecha por defecto en hora de Argentina |
| `Program.cs` | Registra `IReloj` |

---

## Paso 6 — Días con lugar para pedir turno

**No hay migración.** Es un endpoint nuevo, de solo lectura.

### `GET /doctores/{id}/dias-disponibles?desde=YYYY-MM-DD&dias=14`

Por cada día, cuántas franjas libres de 30 minutos tiene el doctor:

```json
[ { "fecha": "2026-10-09T00:00:00", "libres": 6 },
  { "fecha": "2026-10-10T00:00:00", "libres": 0 } ]
```

- **`desde`** es opcional. Si no viene, o si es un día pasado, arranca hoy (hora de Argentina).
- **`dias`** va de 1 a 31; si no viene, son 14.
- **Las cuentas son las mismas que en `GET /doctores/{id}/disponibilidad`:**
  - un día que el doctor no atiende da 0;
  - los turnos cancelados no ocupan lugar;
  - hoy no se cuentan las franjas que ya empezaron.

  Las dos usan el mismo método (`FranjasLibres`).
- **Hace dos consultas a la base en total:** el horario semanal y los turnos del período. No hace una por día.
- **Errores:** doctor inexistente → `404`; inactivo → `400`.
- Cualquier usuario con sesión lo puede pedir, igual que la disponibilidad.

### A quién le pega

- **Nadie:** es un pedido nuevo.
- **La Web nueva con la API vieja** (los minutos del despliegue): la tira de días se muestra sin los números, y los días se pueden elegir igual.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/HorarioLaboralLogica.cs` | `ObtenerDiasDisponibles`. El cálculo de franjas pasa a `FranjasLibres`, que comparten los dos pedidos |
| `Logica/IHorarioLaboralLogica.cs`, `Logica/DTOs/HorarioLaboralDTOs.cs` | La firma y `DiaDisponibleDto` |
| `Endpoints/HorarioLaboralEndpoints.cs` | El endpoint |

---

## Paso 7 — Reprogramar

**No hay migración.** Las reglas para reprogramar ya estaban desde el paso 5. Lo único nuevo en la API es el aviso.

### `PUT /turnos/{id}` con otra fecha u hora avisa al paciente

- Cuando cambia el día o la hora del turno, el paciente recibe una notificación: *"Tu turno del 09/10/2026 a las 10:00 se pasó al 12/10/2026 a las 11:00."*
- **El turno conserva su estado:** si estaba confirmado, sigue confirmado.
- **Lo demás no cambia:**
  - solo se reprograma un turno pendiente o confirmado (`409` si no);
  - el horario nuevo tiene que estar dentro del horario del doctor y no puede ser una fecha pasada (`400`);
  - si choca con otro turno → `409`;
  - el historial anota `turno.reprogramado`.

### A quién le pega

- **A nadie:** es un aviso más.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/TurnoLogica.cs` | `Actualizar` avisa al paciente cuando cambia el horario |

