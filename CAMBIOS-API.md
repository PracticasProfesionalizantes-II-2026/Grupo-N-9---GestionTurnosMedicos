# Cambios en la API — Mejoras, pasos 2 a 13

**Fecha:** 2026-10-08 · **Ramas:** `fix/pacientes-y-turnos` (paso 2), `fix/turnos-y-listados` (paso 3), `fix/listados-paginados` (pasos 3b y 4), `feature/reglas-de-turnos` (pasos 5 y 6) `feature/reprogramar-turnos` (paso 7), `feature/cuentas` (pasos 8 y 9) `feature/recetas-e-historia` (pasos 10 y 11) y `feature/pacientes-notificaciones-accesibilidad` (pasos 12 y 13)

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


---

## Paso 8 — Mi perfil, contraseña y límite de intentos

**No hay migración.** No cambia ninguna tabla.

### `POST /usuarios/me/contrasena` (nuevo)

Cambia la contraseña de quien está logueado. El usuario sale del token, nunca de la URL, y hay que mandar la actual:

```json
{ "contrasenaActual": "Chrono2026!", "contrasenaNueva": "OtraClave2026" }
```

| Respuesta | Cuándo |
|---|---|
| `200` `{ "mensaje": "Contraseña actualizada." }` | Salió bien |
| `400` *"La contraseña nueva debe tener al menos 8 caracteres."* | La nueva es corta |
| `400` *"La contraseña actual no es correcta."* | La actual no coincide |
| `400` *"La contraseña nueva tiene que ser distinta de la actual."* | Son iguales |
| `404` | El usuario no existe o está dado de baja |
| `429` *"Demasiados intentos. Esperá unos minutos y volvé a probar."* | Más de 5 pedidos en 15 minutos del mismo usuario |
| `401` | Sin sesión |

### `PUT /usuarios/{id}` ya no cambia la contraseña

- `UsuarioUpdateDto` queda con `nombre`, `apellido` y `telefono`.
- Si alguien manda `contrasena` (por ejemplo, desde Scalar), se ignora.
- Antes cambiaba la contraseña sin pedir la actual. Ahora el único camino es `POST /usuarios/me/contrasena`.

### Tope de intentos (`429`)

| Pedido | Tope | Se cuenta por |
|---|---|---|
| `POST /usuarios/login` | 10 cada 15 minutos | email (sin importar mayúsculas ni espacios) |
| `POST /usuarios/me/contrasena` | 5 cada 15 minutos | usuario |

- **Por qué por email y no por IP:** a la API los pedidos le llegan desde el servidor de la Web, así que la IP es la misma para todos. Un tope por IP trabaría el login de todo el mundo. El tope por IP de cada persona ya lo pone la Web (`LimiteLogin`).
- **Cuentan todos los intentos, no solo los fallidos.** El contador vive en memoria y vuelve a cero si se reinicia la API.
- Cuando se pasa el tope, la API contesta sin revisar la contraseña.

### A quién le pega

- **La Web:** ya usa el pedido nuevo para "Cambiar contraseña" y nunca mandaba `contrasena` en el `PUT`.
- **El seeder** (`tools/Seed`): entra una vez con cada cuenta que ya existe. Si se corre más de 10 veces en 15 minutos contra la misma API, el login del administrador de la demo queda frenado hasta que pase el rato (o hasta reiniciar la API).
- **Quien pruebe desde Scalar:** para cambiar una contraseña hay que usar el pedido nuevo.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/LimiteIntentos.cs` *(nuevo)* | Los dos topes de intentos |
| `Logica/UsuarioLogica.cs`, `Logica/IUsuarioLogica.cs` | `CambiarContrasena`. `Actualizar` ya no toca la contraseña |
| `Logica/DTOs/UsuarioDTOs.cs` | `CambioContrasenaDto`. `UsuarioUpdateDto` sin `Contrasena` |
| `Endpoints/UsuarioEndpoints.cs` | El pedido nuevo y el `429` en el login |
| `Program.cs` | Registra `LimiteIntentos` como singleton (uno para toda la API) |

---

## Paso 9 — Baja y reactivación de cuentas

**No hay migración.** `Usuarios.Activo` y `Doctores.Activo` ya existían.

### `DELETE /usuarios/{id}` (solo administrador)

Baja lógica: no se borra nada. La cuenta deja de poder entrar y, si tiene perfil de doctor, el doctor también queda inactivo (deja de aparecer en `GET /doctores` y no se le pueden dar turnos). Las dos cosas se guardan juntas.

| Respuesta | Cuándo |
|---|---|
| `204` | Salió bien |
| `404` *"Usuario no encontrado."* | No existe |
| `409` *"La cuenta ya está dada de baja."* | Ya estaba de baja |
| `409` *"No podés dar de baja tu propia cuenta."* | Es la cuenta de quien lo pide (sale del token) |
| `409` *"Es el único administrador activo…"* | Dejaría el sistema sin administradores |
| `409` *"La cuenta tiene N turnos pendientes o confirmados desde hoy…"* | Tiene turnos en pie desde hoy, como paciente o como doctor. No se cancela nada solo: hay que cancelarlos o reprogramarlos primero |

**Antes**, el único error posible era `404` (el resto salía como `403`), y no había ningún freno.

### `POST /usuarios/{id}/reactivar` (nuevo, solo administrador)

Vuelve a activar la cuenta y, si tiene, su perfil de doctor, con el mismo horario de atención que tenía.

- `200` `{ "mensaje": "Cuenta reactivada." }`;
- `404` si no existe;
- `409` *"La cuenta ya está activa."*

### `GET /usuarios?bajas=true`

Lista solo las cuentas dadas de baja. Sin `bajas` (o con `false`), solo las activas, como hasta ahora.

### `GET /usuarios/{id}` suma `activo`

Al final del objeto: `"activo": true` o `false`.

### Un token de una cuenta dada de baja deja de valer

- El token dura 8 horas. Antes, quien ya estaba adentro seguía trabajando hasta que venciera.
- Ahora, en cada pedido con token, la API revisa que la cuenta siga activa (una consulta chica). Si no, contesta `401`.
- La Web ya cierra la sesión ante un `401` y manda al login.
- El login de una cuenta dada de baja sigue dando el mismo `401` que una contraseña mal, para no revelar que el email existe.

### `GET /pacientes` sin las bajas

- El listado trae solo pacientes con la cuenta activa.
- `GET /pacientes/{id}` sigue trayendo la ficha de un paciente dado de baja, para no perder su historia clínica.

### A quién le pega

- **La Web:** usa todo esto en Usuarios (pestañas, Dar de baja y Reactivar).
- **El seeder** (`tools/Seed`): entra con cada cuenta de la demo. **No des de baja una cuenta de la demo**: el seeder no la puede usar y avisa *"ya existe pero su contrasena no es…"*, que confunde. Si pasa, reactivala.
- **Quien pruebe desde Scalar:** `DELETE /usuarios/{id}` ahora puede contestar `409` con el motivo.
- **Límite conocido:** si dos administradores se dan de baja uno al otro en el mismo segundo, el sistema podría quedar sin administradores. Darse de baja a uno mismo ya está frenado, así que hacen falta dos personas a la vez.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/UsuarioLogica.cs`, `Logica/IUsuarioLogica.cs` | `DarDeBaja` (con los frenos) y `Reactivar` reemplazan a `EliminarLogico`. `Buscar` con `bajas`. El constructor suma `ITurnoRepository` e `IReloj` |
| `Logica/CuentaActiva.cs` *(nuevo)* | Revisa que la cuenta del token siga activa |
| `Program.cs` | `OnTokenValidated` usa `CuentaActiva` |
| `Endpoints/UsuarioEndpoints.cs` | `DELETE` con `409`, `POST /{id}/reactivar` y `bajas` en `GET /usuarios` |
| `Repositorios/UsuarioRepository.cs`, `IUsuarioRepository.cs` | `ObtenerConDoctor`, `ContarAdministradoresActivos`, `EstaActivo`, `GuardarActivo` (reemplaza a `Eliminar`) y `Buscar` con `bajas` |
| `Repositorios/PacienteRepository.cs` | El listado, solo con cuentas activas |
| `Logica/DTOs/UsuarioDTOs.cs` | `UsuarioDto` suma `Activo` |

---

## Paso 10 — Recetas que no cambian solas y quién firmó

**Hay migración:** `CopiarMedicamentoEnReceta`:
- agrega cuatro columnas a `RecetaMedicamentos`, todas admiten nulo;
- llena esas columnas en las recetas que ya existen;
- cambia la regla de borrado de la clave foránea hacia `Medicamentos` de `CASCADE` a `NO ACTION`;
- no borra nada.

El script se corre en Azure **antes** de desplegar la API: la API nueva lee esas columnas.

### La receta guarda una copia del medicamento

- **Al emitir una receta**, cada renglón guarda el nombre comercial, el genérico, la concentración y la forma farmacéutica del medicamento. Si después alguien edita el medicamento con `PUT /medicamentos/{id}`, la receta sigue diciendo lo que se recetó.
- **Al modificar una receta** con `PUT /recetas/{id}`, los renglones se reemplazan y toman los datos del momento: el doctor la vuelve a firmar.
- **`GET /pacientes/{id}/recetas`:** cada medicamento suma `nombre`, `nombreGenerico`, `concentracion` y `formaFarmaceutica`. Los campos que ya venían no cambian.
  ```json
  { "idMedicamento": 1, "nombre": "GEFINOVA", "nombreGenerico": "GEFITINIB",
    "concentracion": "250 MG", "formaFarmaceutica": "COMPRIMIDO RECUBIERTO",
    "dosis": "1 comprimido", "frecuencia": "cada 8 horas", "duracion": "5 días", "indicaciones": null }
  ```
- **El cuerpo de `POST /recetas` y `PUT /recetas/{id}` no cambia:** se manda el `idMedicamento` y la API copia los datos.
- **Un renglón sin copia**, de una receta emitida con la API vieja después de correr el script, se muestra con los datos actuales del medicamento.

### `DELETE /medicamentos/{id}` no borra uno que está en una receta

**Antes**, borrar un medicamento borraba en cascada ese renglón de todas las recetas que lo tenían.

| Respuesta | Cuándo |
|---|---|
| `204` | Salió bien: no figura en ninguna receta |
| `404` *"Medicamento no encontrado."* | No existe |
| `409` *"No se puede borrar: el medicamento figura en al menos una receta emitida."* | Está en alguna receta |

Además, la base misma frena el borrado (clave foránea `NO ACTION`).

### Quién firmó y quién escribió

`GET /pacientes/{id}/recetas` y `GET /pacientes/{id}/historiales-clinicos` suman, al final de cada receta o entrada, el bloque `doctor`:

```json
"doctor": { "idDoctor": 1, "nombre": "Laura", "apellido": "Gomez",
            "especialidad": "Clínica", "matricula": "MP-10001" }
```

En el historial, el `idDoctor` que ya venía sigue igual.

### A quién le pega

- **La Web:**
  - muestra "Firmada por" en la receta y "Escrita por" en la historia clínica;
  - tiene la hoja nueva para imprimir;
  - ya no baja el vademécum completo para mostrar el nombre de los medicamentos.
- **Quien pruebe desde Scalar:** `DELETE /medicamentos/{id}` ahora puede contestar `409`.
- **El seeder:** no cambia. Carga medicamentos, no borra ninguno.
- **Límite conocido:** si se emite una receta con un medicamento justo mientras otro lo borra, la base frena el borrado y la respuesta es `500` en vez de `409`. No se pierde ningún dato.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Entidades/RecetaMedicamento.cs` | Cuatro campos con la copia del medicamento |
| `Datos/AppDbContext.cs` | Largos de la copia; la relación con `Medicamento` pasa a `Restrict` |
| `Logica/RecetaLogica.cs` | Copia los datos al emitir y al modificar; el DTO usa la copia y suma el doctor |
| `Logica/Profesional.cs` *(nuevo)* | Arma el bloque `doctor` |
| `Logica/HistorialClinicoLogica.cs` | El DTO suma el doctor |
| `Logica/MedicamentoLogica.cs`, `IMedicamentoLogica.cs` | `Eliminar` devuelve `(ok, error)` y frena si está en una receta |
| `Endpoints/MedicamentoEndpoints.cs` | `DELETE` con `404` y `409` |
| `Logica/DTOs/MedicamentoDTOs.cs` | `MedicamentoRecetadoDto` *(nuevo)*: lo que se lee de una receta |
| `Logica/DTOs/RecetaDTOs.cs`, `HistorialClinicoDTOs.cs` | Suman `Doctor` |
| `Logica/DTOs/DoctorDTOs.cs` | `ProfesionalDto` *(nuevo)* |
| `Repositorios/RecetaRepository.cs`, `HistorialClinicoRepository.cs` | Cargan el doctor y su cuenta |
| `Repositorios/MedicamentoRepository.cs`, `IMedicamentoRepository.cs` | `EstaEnAlgunaReceta` |
| `Migrations/…_CopiarMedicamentoEnReceta.cs` | La migración, con el `UPDATE` que llena las recetas existentes |

---

## Paso 11 — El doctor atiende desde el turno

**No hay migración.** `Recetas.IdTurno` e `HistorialesClinicos.IdTurno` ya existían.

### El turno vinculado tiene que ser de ese paciente con ese doctor

**Dónde se revisa:** si una receta o una entrada de la historia clínica viene con `idTurno`, ese turno tiene que ser del mismo paciente y del mismo doctor. Vale en estos cuatro:
- `POST /recetas`;
- `PUT /recetas/{id}`;
- `POST /pacientes/{id}/historiales-clinicos`;
- `PUT /pacientes/{id}/historiales-clinicos/{idHistorial}`.

**Antes**, se aceptaba cualquier número.

**Contra qué se compara:**
- en un alta, contra el paciente del pedido y el doctor que firma;
- al modificar, contra el paciente y el doctor que ya tenía.

**Si no cumple**, la respuesta es `404` *"Turno no encontrado: tiene que ser un turno de este paciente con este doctor."* Un turno de otro paciente o de otro doctor se contesta como inexistente, igual que en el resto de la API.

**Sin `idTurno`**, todo sigue como antes.

### `RecetaDto` suma `idTurno`

Al final del objeto, en `GET /pacientes/{id}/recetas`: `"idTurno": 6`, o `null` si la receta no está vinculada a un turno. Sirve para editar una receta sin perder el vínculo.

### `PUT /recetas/{id}`: "no encontrado" es `404`

Un medicamento que no existe, o un turno que no corresponde, contestaba `400`, porque el endpoint solo buscaba "no encontrada". Ahora contesta `404`, igual que `POST /recetas`.

### A quién le pega

- **La Web:**
  - "Registrar consulta" y "Emitir receta" desde el detalle del turno, ya vinculadas;
  - la casilla "Marcar el turno como completado". Usa el `PUT /turnos/{id}` de siempre, después de guardar la consulta;
  - editar recetas y entradas de la historia clínica.
- **Quien pruebe desde Scalar:** mandar un `idTurno` que no es de ese paciente con ese doctor ahora da `404`.
- **Sin cambios:** el administrador puede seguir modificando cualquier receta con `PUT /recetas/{id}`. La Web muestra "Editar" solo al doctor que la firmó.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Logica/TurnoVinculado.cs` *(nuevo)* | El chequeo del turno vinculado |
| `Logica/RecetaLogica.cs` | Suma `ITurnoRepository`. Revisa el turno al emitir y al modificar. El DTO suma `IdTurno` |
| `Logica/HistorialClinicoLogica.cs` | Suma `ITurnoRepository`. Revisa el turno al crear y al modificar |
| `Logica/DTOs/RecetaDTOs.cs` | `RecetaDto` suma `IdTurno` |
| `Endpoints/RecetaEndpoints.cs` | `PUT`: "no encontrado" también da `404` |

---

## Paso 12 — Buscar pacientes

**No hay migración.**

### `GET /pacientes?buscar=texto`

- **Cada palabra tiene que aparecer en algún campo:** nombre, apellido, email o DNI. Por ejemplo:
  - "ana dua" encuentra a Ana Duarte;
  - "30111" la encuentra por el DNI;
  - "duarte@" la encuentra por el email.
- **Mayúsculas y acentos no importan:** "gomez" encuentra a "Gómez" y "pena" a "Peña". La consulta compara con la collation `Latin1_General_CI_AI`, porque la de la base distingue acentos.
- **Como mucho 5 palabras;** las que sobran se ignoran.
- **Los comodines no rompen nada:** `%` y `_` se buscan como texto.
- **Sin `buscar`,** el listado es como siempre: por apellido, solo cuentas activas y paginado.
- **Los parámetros de antes** (`nombre`, `dni`, `cobertura_id`, `pagina` y `limite`) siguen andando igual.

```
GET /pacientes?buscar=lucia%20gomez&limite=20
→ { "total": 2, "pagina": 1, "pacientes": [ { "idPaciente": 7, "nombre": "Lucía", "apellido": "Gómez",
     "email": "lucia.gomez@mail.com", "dni": "30111222", ... }, ... ] }
```

### A quién le pega

- **La Web:** los cinco desplegables de hasta 200 pacientes se reemplazan por un buscador:
  - Recetas y Historia clínica, en el filtro y al cargar una nueva;
  - `Turnos/Confirmar`, cuando el personal elige el paciente.

  La pantalla de Pacientes también busca por DNI y email.
- **Quien use la API desde Scalar o Postman:** sin cambios. `buscar` es opcional.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Endpoints/PacienteEndpoints.cs` | Parámetro `buscar` |
| `Logica/PacienteLogica.cs`, `IPacienteLogica.cs` | `ObtenerTodos` recibe `buscar` |
| `Repositorios/PacienteRepository.cs`, `IPacienteRepository.cs` | `Buscar` filtra palabra por palabra, sin distinguir mayúsculas ni acentos |
| `Repositorios/PalabrasDeBusqueda.cs` *(nuevo)* | Separa el texto en palabras (hasta 5) |
| `Endpoints/ReporteEndpoints.cs` | La llamada a `Buscar` suma el parámetro nuevo |

---

## Paso 13 — Notificaciones visibles

**No hay migración.**

### `GET /usuarios/{id}/notificaciones/no-leidas`

- Devuelve cuántas notificaciones tiene el usuario sin leer: `{ "noLeidas": 3 }`.
- **Permiso:** el mismo que el listado. Las ve el propio usuario o un administrador; cualquier otro recibe **403**.

```
GET /usuarios/6/notificaciones/no-leidas
→ { "noLeidas": 3 }
```

### `PATCH /notificaciones/leer-todas`

- Marca como leídas **todas las del usuario del token** y devuelve cuántas marcó: `{ "marcadas": 3 }`.
- No recibe id: nadie marca las de otro, igual que `PATCH /notificaciones/{id}/leer`.
- Si no había ninguna sin leer, devuelve `{ "marcadas": 0 }`.
- Es un solo `UPDATE` en la base, no una consulta por notificación.

### A quién le pega

- **La Web:** los pacientes ven una **campana** en el encabezado, con la cantidad sin leer, y la pantalla **Notificaciones**:
  - pestañas "Todas" y "Sin leer";
  - "Marcar como leída" en cada una y "Marcar todas como leídas";
  - "Ver mis turnos" en las de turnos.

  La Web guarda la cantidad en la sesión por un minuto, así no le pregunta a la API en cada página.
- **Quien use la API desde Scalar o Postman:** los dos endpoints son nuevos; lo de antes no cambia.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Endpoints/NotificacionEndpoints.cs` | Los dos endpoints nuevos |
| `Logica/NotificacionLogica.cs`, `INotificacionLogica.cs` | `ContarNoLeidas` y `MarcarTodasLeidas` |
| `Repositorios/NotificacionRepository.cs`, `INotificacionRepository.cs` | Contar las sin leer y marcarlas todas con `ExecuteUpdateAsync` |
