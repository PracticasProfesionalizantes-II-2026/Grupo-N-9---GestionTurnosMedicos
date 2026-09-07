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
