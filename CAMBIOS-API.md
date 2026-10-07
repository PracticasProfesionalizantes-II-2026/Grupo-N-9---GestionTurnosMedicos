# Cambios en la API — Un solo rol de personal: se va "secretario"

**Fecha:** 2026-10-07 · **Rama:** `feature/rol-unico`

La API nombraba un rol `secretario` en sus chequeos de permisos, siempre al lado de
`administrador`, pero nunca se pudo crear un usuario con ese rol: el registro solo
acepta `paciente`, `doctor` y `administrador`, y el seeder tampoco lo crea. Se quitó
de todos los chequeos. El personal administrativo es `administrador`.

**No cambia ninguna respuesta.** Se verificó antes que no hay usuarios con rol
`secretario` ni en la base local ni en Azure. No hay migración.

Lo único a tener en cuenta: si alguien insertara a mano un usuario con ese rol, ya no
tendría permisos de personal. Hay que crearlo como `administrador`.

## Dónde estaba

| Archivo | Endpoints |
|---|---|
| `ChronoSaludApi/Endpoints/HorarioLaboralEndpoints.cs` | `PUT /doctores/{id}/horarios` |
| `ChronoSaludApi/Endpoints/TurnoEndpoints.cs` | `GET /turnos/{id}`, `PUT /turnos/{id}` |
| `ChronoSaludApi/Endpoints/PacienteEndpoints.cs` | Lectura y edición de la ficha del paciente |
| `ChronoSaludApi/Endpoints/CoberturaEndpoints.cs` | Coberturas de un paciente |
| `ChronoSaludApi/Endpoints/HistorialClinicoEndpoints.cs` | Lectura del historial clínico |
| `ChronoSaludApi/Endpoints/EstudioEndpoints.cs` | Estudios de un paciente |
| `ChronoSaludApi/Endpoints/RecetaEndpoints.cs` | Recetas |
| `ChronoSaludApi/Endpoints/NotificacionEndpoints.cs` | `GET /usuarios/{id}/notificaciones` |

En la Web se quitó de dos permisos (`AuthService.cs`), de dos textos
(`TurnosController.cs`) y de comentarios.

Los archivos `pruebas-*.http` de la raíz todavía mencionan el rol en sus comentarios y
en un caso de prueba; no se tocaron.

---

# Cambios en la API — Dueño del turno al reservar y al cancelar

**Fecha:** 2026-10-07 · **Rama:** `feature/turnos-dueno`

Hasta ahora `POST /turnos` y `DELETE /turnos/{id}` no miraban quién llamaba: cualquier
usuario autenticado reservaba a nombre de cualquier paciente y cancelaba cualquier
turno. Lo frenaba solo la Web. Ahora lo controla la API. **Cambian respuestas que antes
andaban** si usan la API directo por Scalar o Postman. La Web y el seeder no necesitan
cambios. No hay migración.

## 1. `DELETE /turnos/{id}`: solo el dueño, y solo turnos en pie

| Quién llama | Qué puede cancelar |
|---|---|
| `paciente` | Sus propios turnos |
| `doctor` | Los turnos de su agenda |
| `administrador` | Cualquiera |

| Caso | Antes | Ahora |
|---|---|---|
| El turno es de otro paciente o de otra agenda | `204` | `404` `"Turno no encontrado."` |
| El turno está `completado` o ya `cancelado` | `204` (y avisaba de nuevo al paciente) | `409` `"Conflicto de estado: el turno está completado y no se puede cancelar."` |
| Turno propio `pendiente` o `confirmado` | `204` | `204` (igual) |

Primero se revisa el dueño y después el estado: un turno ajeno contesta siempre `404`,
esté en el estado que esté. Un paciente o un doctor sin perfil cargado también recibe
`404`.

## 2. `POST /turnos`: el paciente reserva solo a su nombre

- Con token de `paciente`, el `idPaciente` del cuerpo **se ignora**: el turno queda a
  nombre del paciente del token. Ya no hace falta mandarlo. Si ese usuario no tiene
  perfil de paciente: `400` `"Tu usuario no tiene un perfil de paciente asociado."`.
- Con token de `doctor` o `administrador` no cambia: reservan para el `idPaciente`
  que manden.
- Si ese `idPaciente` no existe, ahora contesta `400` `"El paciente indicado no existe."`.
  Antes fallaba en la base y devolvía `500`.

## 3. Token incompleto

En `POST`, `PUT` y `DELETE` de turnos, un token sin id de usuario o sin rol contesta
`401`.

## Sin cambios

`GET /turnos`, `GET /turnos/{id}` y `PUT /turnos/{id}` se comportan igual que antes.
En el `PUT` cambió solo cómo recibe la lógica al usuario que llama.

## Archivos tocados

| Archivo | Qué cambió |
|---|---|
| `ChronoSaludApi/Logica/Solicitante.cs` | Nuevo: usuario y rol del token, armados una sola vez por el endpoint |
| `ChronoSaludApi/Logica/ITurnoLogica.cs` | `Crear`, `Actualizar` y `Cancelar` reciben al solicitante |
| `ChronoSaludApi/Logica/TurnoLogica.cs` | Reglas de dueño y de estado al cancelar; paciente del token y paciente inexistente al crear |
| `ChronoSaludApi/Endpoints/TurnoEndpoints.cs` | `POST`, `PUT` y `DELETE` leen el token; el `DELETE` suma el `409` |

---

# Cambios en la API — Registro cerrado y lectura de cuentas

**Fecha:** 2026-10-05 · **Rama:** `feature-a0-registro`

Dos cambios de permisos en `/usuarios`. **Los dos rompen pedidos que antes andaban**
si usan la API directo por Scalar o Postman. La Web y el seeder ya están adaptados.
No hay migración.

## 1. `POST /usuarios/registro` ya no acepta cualquier rol

Antes cualquiera, sin token, podía crear una cuenta de doctor o de administrador.

Ahora:

| Rol que se pide | Quién puede | Si no |
|---|---|---|
| `paciente` | Cualquiera, sin token (como antes) | — |
| `doctor` o `administrador` | Solo con el token de un administrador | `401` sin token · `403` con token de otro rol |

El `401` y el `403` llegan sin cuerpo. El resto no cambió: mismo cuerpo, mismo `201`
con el token de la cuenta creada, mismo `409` por email repetido.

**Qué tienen que hacer:** para crear un doctor o un administrador, primero
`POST /usuarios/login` con una cuenta de administrador y mandar ese token en
`Authorization: Bearer <token>` (en Scalar, el botón de autenticación).

### El primer administrador

Si la base no tiene **ningún administrador activo**, el registro de un administrador se
acepta sin token; si no, no habría forma de crear el primero. Vale solo para el rol
`administrador`, no para `doctor`. En una base recién creada pueden correr el seeder
(`dotnet run --project tools/Seed`) o registrar el administrador a mano por Scalar.

Ojo: en una base nueva expuesta a internet, el primero que se registra queda de
administrador. Creen el administrador apenas levanten la API.

### Seeder

Si su base ya tiene administradores pero no el de la demo
(`admin@chronosalud.demo`), el seeder corta con "Ya hay administradores". Se corre con
la cuenta de uno de ellos: `dotnet run --project tools/Seed -- --email <administrador>`.

## 2. `GET /usuarios/{id}` solo para el dueño o un administrador

Antes cualquier usuario autenticado leía nombre, apellido, email, teléfono y rol de
cualquier cuenta. Ahora solo la propia, o cualquiera si el token es de un administrador;
al resto, `403` sin cuerpo (también si el id no existe). Es la misma regla que ya tenía
`PUT /usuarios/{id}`.

## Archivos tocados

| Archivo | Qué cambió |
|---|---|
| `ChronoSaludApi/Endpoints/UsuarioEndpoints.cs` | Permisos del registro y del GET por id |
| `ChronoSaludApi/Logica/UsuarioLogica.cs`, `IUsuarioLogica.cs` | `Registrar` recibe si quien llama es administrador |
| `ChronoSaludApi/Repositorios/UsuarioRepository.cs`, `IUsuarioRepository.cs` | `HayAdministradorActivo` |
| `ChronoSaludWeb/Services/AuthService.cs` | El alta de doctor y de administrador manda el token de la sesión |
| `ChronoSaludWeb/Controllers/AdminController.cs` | Mensaje para el `403` |
| `tools/Seed/Program.cs` | Entra como administrador antes de crear doctores; `--email` en la carga completa |

---

# Cambios en la API — Historial Clínico

**Fecha:** 2026-09-07 · **Rama:** `main`

Dos cambios en el historial clínico. **Uno rompe el contrato HTTP y el otro cambia
el esquema de la base**, así que los dos necesitan acción de su lado.

---

## 1. El PUT ahora pide el id de la entrada (rompe compatibilidad)

### Antes

```
PUT /pacientes/{id}/historiales-clinicos
```

No recibía qué entrada editar. Buscaba la **primera entrada con la misma fecha**
del cuerpo y modificaba esa. Si no encontraba ninguna, **creaba una nueva en
silencio** y contestaba `200 OK` como si hubiera editado.

Dos problemas: con dos entradas el mismo día editaba una cualquiera, y un PUT
sobre algo inexistente terminaba dando de alta un registro clínico sin que nadie
lo pidiera.

### Ahora

```
PUT /pacientes/{id}/historiales-clinicos/{idHistorial}
```

- Opera sobre el `idHistorial` real de la ruta.
- Si esa entrada no existe → **`404`**.
- Si existe pero es de **otro paciente** → **`404`** (no confirmamos que el id
  exista bajo otra historia clínica).
- **Un PUT nunca crea.** Para dar de alta está el `POST`, que no cambió.

El cuerpo es el mismo de antes (`fecha`, `descripcion`, `diagnostico`, `idTurno`).

### A quién le pega

| Consumidor | Estado |
|---|---|
| `chronosalud-front/js/paginas/paciente.js` | **Ya adaptado** (ver abajo). |
| `ChronoSaludWeb/Services/HistorialService.cs` | OK, sin cambios. Solo usa GET y POST. |

### Qué se cambió en `chronosalud-front`

El `idHistorial` ya venía en la respuesta del GET, solo había que llevarlo hasta
el PUT:

- El botón **Editar** de cada fila ahora lleva `data-editar-historial="<id>"` en
  vez de la fecha; la fecha pasó a `data-fecha`.
- `paciente.html`: campo oculto nuevo `<input name="idHistorial" id="id-historial">`
  en el formulario del modal, al lado del de `modo`.
- `abrirModalHistorial()` guarda el id en ese campo y `guardarHistorial()` lo
  usa para armar la URL del PUT.
- **La fecha ya no queda de solo lectura al editar.** Antes se fijaba para no
  terminar creando una entrada nueva por accidente; ahora que se edita por id ese
  riesgo no existe y la fecha es un campo más, que la API actualiza como al resto.
- Se sacó el cartelito "Se modifica la entrada de esta fecha": ya no se edita
  "la entrada de una fecha" sino la que el usuario eligió de la lista.

---

## 2. `HistorialClinico` ahora guarda qué doctor escribió la entrada (cambia el esquema)

Faltaba el autor, que en una historia clínica es dato obligatorio.

- Nueva columna **`IdDoctor`** en `HistorialesClinicos`: `int NOT NULL`, con FK a
  `Doctores(Id)` (`ON DELETE NO ACTION`) e índice.
- **Sale del claim del token**, no del cuerpo del request. El cliente no puede
  decir que la escribió otro. El endpoint ya pedía rol `doctor`; ahora además
  resuelve el perfil de doctor del usuario autenticado.
- `HistorialClinicoDto` (respuesta del GET) suma **`idDoctor`**. Es aditivo: los
  clientes que no lo miran siguen andando.
- `HistorialClinicoCreateDto` (cuerpo de POST/PUT) **no cambia** — a propósito.

### Efecto secundario a tener en cuenta

Si un usuario con rol `doctor` no tiene fila en `Doctores`, el `POST` ahora
contesta **`400`** con `"El usuario autenticado no tiene perfil de doctor."`.
Antes andaba igual. Si les falla, chequeen que el doctor tenga perfil creado.

### Migración

```
20260907201107_AgregarIdDoctorAHistorialClinico
```

Correr en cada base local:

```bash
cd ChronoSaludApi
dotnet ef database update
```

**Cerrá la API antes**, si no el build falla porque el `.exe` está bloqueado.

#### Qué hace con las entradas que ya existían

Una columna `NOT NULL` con FK no puede quedar en 0, así que la migración rellena
`IdDoctor` en tres pasos, de más confiable a menos:

1. El doctor del **turno asociado** a la entrada (`IdTurno`). Este es el autor real.
2. Si no tiene turno asociado: el doctor del **turno más reciente de ese paciente**.
3. Último recurso: el **doctor activo de menor Id**.

> ⚠️ **Solo el paso 1 recupera el autor de verdad.** Los pasos 2 y 3 son una
> aproximación para no perder las filas de prueba que ya estaban cargadas. En la
> base de desarrollo de acá quedaron 3 entradas: una por el paso 1 y dos por el
> paso 2. Si en su base hay entradas que les importan, **revisen el `IdDoctor`
> después de migrar**. Sobre datos reales habría que resolver la autoría a mano
> antes de correr esto.

Si la base no tiene ningún doctor cargado, la migración corta con un mensaje
explicando eso en vez de reventar con un error de FK.

---

## Decisión abierta: quién queda como autor al editar

Hoy el `PUT` **no toca `IdDoctor`**: queda quien escribió la entrada
originalmente, aunque la edite otro doctor. Es lo más fiel a "qué doctor escribió
la entrada", pero significa que si el doctor B edita una entrada del doctor A, el
registro sigue diciendo A.

La alternativa sería registrar las dos cosas (autor original + último editor), que
es lo que haría un sistema real. **Si el grupo quiere eso, avisen: es otra
migración.**

---

## Archivos tocados

| Archivo | Qué cambió |
|---|---|
| `ChronoSaludApi/Endpoints/HistorialClinicoEndpoints.cs` | Ruta del PUT con `{idHistorial}`; lee el claim en el POST |
| `ChronoSaludApi/Logica/HistorialClinicoLogica.cs` | Edita por id, 404 si no existe, nunca crea; resuelve el doctor del token |
| `ChronoSaludApi/Logica/IHistorialClinicoLogica.cs` | Firmas de `Crear` y `Actualizar` |
| `ChronoSaludApi/Logica/DTOs/HistorialClinicoDTOs.cs` | `idDoctor` en el DTO de lectura |
| `ChronoSaludApi/Entidades/HistorialClinico.cs` | Propiedad `IdDoctor` + navegación `Doctor` |
| `ChronoSaludApi/Datos/AppDbContext.cs` | Relación `HistorialClinico → Doctor` |
| `ChronoSaludApi/Migrations/20260907201107_*` | Migración con el relleno |
| `chronosalud-front/js/paginas/paciente.js` | Edita por id: botón, modal y PUT |
| `chronosalud-front/paciente.html` | Campo oculto `idHistorial` en el modal |

## Verificado

Contra la API corriendo, con el token de un doctor:

| Caso | Resultado |
|---|---|
| `POST` crea la entrada | `idDoctor` sale del token, no del cuerpo |
| `PUT` con id existente | `200` |
| `PUT` con id inexistente | `404` |
| `PUT` con id de otro paciente | `404` |
| `PUT` a la ruta vieja sin id | `405` |
| Cantidad de entradas tras los PUT fallidos | No crece — el PUT nunca crea |
| `idDoctor` después de editar | No cambia |
