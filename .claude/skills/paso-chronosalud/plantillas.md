# Plantillas del método de pasos

Las tres plantillas que usa la skill `paso-chronosalud`. Copian el formato que ya
tienen `docs/plan-funcionalidades.md`, `docs/bitacora.md` y `CAMBIOS-API.md`.

## Plan

```markdown
## Paso <N> — <título corto>

**Objetivo.** <una o dos frases: qué puede hacer quién, que antes no podía>

**Estado actual.** <qué hay hoy, con archivo:líneas>

**API.**
- `Ruta/Archivo.cs:40-62` — modificación: <qué cambia>
- `Ruta/ArchivoNuevo.cs` — nuevo: <qué hace>

**Web.**
- `Controllers/...` — agregado: <acción, GET/POST>
- `Views/...` — <qué se ve>

**Pruebas.** <archivos de prueba nuevos y qué casos cubren>

**Esquema y despliegue.** <sin migración / con migración y comandos>. Orden: <base → API → Web>.

**Permisos.** <tabla o lista por rol: paciente, doctor, administrador>

**Riesgos.** <qué se puede romper y a quién le pega>

**Cómo probarlo.** <pasos exactos: pantalla o endpoint, datos, resultado esperado>

**Esfuerzo.** <bajo / medio / alto>
```

## Bitácora

Va al final de `docs/bitacora.md`.

```markdown
## <AAAA-MM-DD> — mejoras, paso <N>: <título>
**Hecho:**
- <qué quedó andando, en lenguaje de usuario>
**Probado:**
- <N> pruebas nuevas (<total> en total): <qué cubren>.
- <prueba manual, si la hubo>
**A medias:** <lo que quedó sin terminar, o —>
**Ojo:**
- <migración, ajustes de Azure, orden de despliegue, cosas que pueden sorprender>
- **Quedan para más adelante:** <ideas que salieron y no entraron>
```

## CAMBIOS-API

Un apartado por paso, al final de `CAMBIOS-API.md`. Sirve para los compañeros que
usan la API directo por Scalar o Postman.

```markdown
---

## Paso <N> — <título>

**Hay migración:** <no / sí, cuál y cuándo correr el script>

### `<MÉTODO> /ruta/{id}` (nuevo)

- **Quién:** <roles>
- **Pide:** <cuerpo o parámetros, con un ejemplo JSON>
- **Contesta:** <códigos y cuerpo>

### A quién le pega

- <qué cambia para quien ya usaba la API>

### Archivos tocados (API)

- `Ruta/Archivo.cs`
```
