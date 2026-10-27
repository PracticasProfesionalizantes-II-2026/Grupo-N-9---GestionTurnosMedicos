# TP Desarrollo con IA — ChronoSalud

**Grupo N°9:** Bujonok Francisco, Perez Facundo
**Proyecto:** ChronoSalud, sistema de gestión de turnos médicos (`ChronoSaludApi` + `ChronoSaludWeb`)

## Resumen

La consigna pide usar agentes, skills o MCP en el proyecto de la práctica, para una de
dos cosas: mejorar el proceso de desarrollo, o automatizar algo del negocio. Hicimos
las dos, y una sirve para construir la otra:

| Parte | Qué es | Opción de la consigna |
|---|---|---|
| Skill `paso-chronosalud` | Una skill de Claude Code con nuestro método de trabajo: plan aprobado, reglas del proyecto, pruebas, revisión y documentación | 1: proceso de desarrollo |
| Subagente `revisor-permisos` | Un agente de Claude Code que solo lee y revisa cada cambio buscando las fallas de seguridad que ya tuvimos | 1: proceso de desarrollo |
| Borrador del resultado con IA | El doctor o la administración suben el PDF de un estudio y la IA propone el texto del resultado para el paciente | 2: funcionalidad de negocio |

La funcionalidad de la tercera fila la construyó Claude Code entera, usando la skill
y el subagente de las dos primeras.

## Por qué estas ideas

**El proceso.** ChronoSalud se hizo en 23 pasos con Claude Code (ver
`docs/bitacora.md`). En cada paso repetíamos a mano, en el prompt, las mismas reglas:
no tocar `main` porque publica en Azure, validar permisos en la API y no en la Web,
nada de JavaScript nuevo, `dotnet ef` lo corre Francis, dejar la nota en
`CAMBIOS-API.md`, escribir la bitácora. Cuando nos olvidábamos de alguna, aparecían
problemas: en el paso A0 encontramos que cualquiera podía crearse una cuenta de
administrador y que `GET /usuarios/{id}` mostraba datos de cualquier cuenta. La skill
deja esas reglas escritas una sola vez, y el subagente revisa justo ese tipo de falla.

**El negocio.** Desde el paso 23 el resultado de un estudio tiene dos partes: el PDF
del laboratorio y un texto que lee el paciente en «Mis estudios». Ese texto hoy se
escribe a mano leyendo el PDF. Es la tarea que la consigna da de ejemplo
("interpretando el texto de un PDF"), y en nuestro caso el PDF ya está guardado en la
base.

## Herramientas

| Herramienta | Para qué la usamos |
|---|---|
| [Claude Code](https://code.claude.com/docs/en/overview) | El agente que programa dentro del repo |
| [Skills de Claude Code](https://code.claude.com/docs/en/skills.md) | `.claude/skills/paso-chronosalud/` |
| [Subagentes de Claude Code](https://code.claude.com/docs/en/sub-agents.md) | `.claude/agents/revisor-permisos.md` |
| [API de Claude, lectura de PDF](https://platform.claude.com/docs/en/build-with-claude/pdf-support) | La API de ChronoSalud le manda el PDF y recibe el borrador |

Consideramos agregar un MCP, pero no hacía falta: Claude Code ya lee el repo y corre
`dotnet`, y la funcionalidad de negocio llama a la API de Claude directo desde .NET.

---

## Parte 1: la skill `paso-chronosalud`

**Dónde está:** `.claude/skills/paso-chronosalud/SKILL.md` y `plantillas.md`.

**Cómo se usa:** dentro de Claude Code, en la carpeta del repo:

```
/paso-chronosalud paso 24: <qué hay que hacer>
```

También se carga sola cuando el pedido se parece a "agregá una funcionalidad" o
"nuevo endpoint".

**Qué hace:** obliga a Claude Code a trabajar en cinco etapas, en orden.

| Etapa | Qué pasa | Qué evita |
|---|---|---|
| 1. Entender | Revisa la rama (frena si está en `main`), lee README, guía de diseño, reglas, última bitácora y el código real | Publicar en Azure sin querer; planificar sobre documentos viejos |
| 2. Plan | Arma el plan con nuestra plantilla (archivos con líneas, permisos, migración, riesgos, cómo probar) y espera la aprobación | Que edite antes de que lo revisemos |
| 3. Implementar | Aplica las reglas de API, Web, base y pruebas del proyecto | Permisos en la Web y no en la API, JavaScript nuevo, claves en `appsettings.json`, migraciones corridas solas |
| 4. Revisar | Llama al subagente `revisor-permisos` y corrige lo alto y lo medio | Que el mismo que escribió el código se dé el visto bueno |
| 5. Documentar | Bitácora, `CAMBIOS-API.md`, README y un resumen con cómo probar | Cambios en la API sin aviso para los compañeros |

Las reglas no son inventadas: salen de `docs/plan-funcionalidades.md`, de
`docs/diseno.md` y de lo que fue pasando en la bitácora.

## Parte 2: el subagente `revisor-permisos`

**Dónde está:** `.claude/agents/revisor-permisos.md`.

**Cómo se usa:** la skill lo llama sola en la etapa 4. También se puede pedir a mano:

```
Usá el subagente revisor-permisos para revisar el diff contra main
```

**Qué hace:** lee el diff de la rama contra `main` y, por cada endpoint o acción
tocada, revisa una lista de control armada con fallas que ya tuvimos: endpoints sin
roles, el id de quien llama tomado del cuerpo o de la URL, falta de chequeo de dueño,
datos de salud de más en una respuesta, claves en el repo, servicios externos sin
tiempo máximo o sin tope de pedidos, archivos aceptados por la extensión.

Devuelve una tabla con nivel (alto, medio, bajo), archivo y línea, cómo se aprovecha
la falla y cómo se arregla.

**Por qué es un agente aparte:** tiene su propio contexto y solo herramientas de
lectura (`Read`, `Grep`, `Glob` y `git` de lectura). No vio cómo se escribió el
código, así que no se lo justifica, y no puede "arreglar" algo a escondidas.

---

## Parte 3: borrador del resultado con IA

### Qué hace

En **Estudios → Cargar resultado**, al lado de «Guardar resultado» aparece un botón
**«Proponer el texto con IA»**.

1. La doctora o la administración eligen el PDF del informe (o usan el que ya está
   subido) y tocan el botón.
2. La API le manda el PDF a Claude y recibe un borrador en voseo, para el paciente:
   qué se midió, qué valores el informe marca fuera de rango y la sugerencia de
   consultarlo con su médico.
3. La pantalla vuelve con el borrador en el cuadro «Resultado» y un aviso: «Texto
   propuesto por IA a partir del PDF. Revisalo y corregilo antes de guardar: el
   paciente lo va a leer.»
4. Nada se guarda ni se le avisa al paciente hasta que la persona toca «Guardar
   resultado», como siempre.

La IA no diagnostica ni decide: propone un texto que una persona del equipo de salud
revisa y firma con su usuario.

### Diseño

**API**

| Archivo | Qué |
|---|---|
| `Logica/ILectorDeEstudios.cs` (nuevo) | `Task<(string? borrador, string? error)> ProponerResultado(byte[] pdf, string tipo, string descripcion, CancellationToken)` |
| `Logica/LectorDeEstudiosConClaude.cs` (nuevo) | Llama a `POST https://api.anthropic.com/v1/messages` con `HttpClient`, sin paquetes nuevos. Encabezados `x-api-key` y `anthropic-version: 2023-06-01`. El PDF va en base64 en un bloque `document`, antes del texto con las instrucciones. Tiempo máximo de 60 segundos. Si la API de Claude falla, devuelve un error y no tira excepción |
| `Logica/LectorDeEstudiosSinConfigurar.cs` (nuevo) | Si no hay clave: contesta «La lectura con IA no está configurada.» Mismo criterio que `CorreoSinConfigurar` |
| `Logica/OpcionesDeIA.cs` (nuevo) | Sección `IA`: `ApiKey`, `Modelo` (por defecto `claude-haiku-5-5`), `MaxTokens` (por defecto 800) |
| `Logica/EstudioLogica.cs` | Nuevo `ProponerResultado(int idEstudio, Solicitante quien)`: el estudio existe, tiene PDF guardado y quien pide entra en el tope. No cambia el estado ni el resultado guardado |
| `Logica/LimiteIntentos.cs` | Nuevo tope: 20 borradores por hora por usuario, porque cada lectura es un pedido pago |
| `Endpoints/EstudioEndpoints.cs` | `POST /estudios/{id}/resultado/borrador`, solo `doctor` y `administrador` |
| `Program.cs` | Registra las opciones y elige la implementación según haya clave o no |

Respuestas del endpoint:

| Caso | Código |
|---|---|
| Borrador listo | `200` con `{ "borrador": "...", "hechoPorIA": true }` |
| El estudio no existe | `404` |
| El estudio no tiene PDF | `409` «Primero subí el PDF del resultado.» |
| Pasó el tope | `429` |
| IA sin configurar o caída | `503` con el motivo, sin detalles técnicos |
| Paciente | `403` (por el rol del endpoint) |

**Qué se le manda a Claude:** el PDF, el tipo de estudio y la descripción del pedido
(por ejemplo «sangre», «Hemograma completo»). No se mandan el nombre, el DNI ni otros
datos de la ficha del paciente.

**Instrucciones del borrador:** escribir para el paciente, en voseo y sin jerga;
nombrar solo los valores que el informe marca fuera de rango; no diagnosticar ni
recomendar tratamientos; no inventar datos que no estén en el PDF; si el PDF no es un
informe de estudio o no se puede leer, decirlo en una frase; hasta 1200 caracteres.

**Web**

| Archivo | Qué |
|---|---|
| `Services/EstudioService.cs` | `ProponerResultadoAsync(int id)` |
| `Controllers/EstudiosController.cs` | Acción POST `ProponerResultado`. Si en el formulario eligieron un PDF nuevo, primero lo valida y lo sube (igual que `Resultado`). Después pide el borrador. Si el cuadro estaba vacío, lo llena; si ya tenía texto, agrega el borrador abajo, después de una línea «Propuesta de la IA:», sin borrar lo escrito |
| `Views/Estudios/Resultado.cshtml` | Segundo botón del mismo formulario con `formaction` y `formnovalidate`, sin JavaScript. Aviso del borrador con el componente `aviso` de `docs/diseno.md` |

**Configuración**

| Dónde | Ajuste |
|---|---|
| Local | `dotnet user-secrets set "IA:ApiKey" "<clave>" --project ChronoSaludApi` |
| Azure (App Service de la API) | `IA__ApiKey`, y opcional `IA__Modelo` |

La clave nunca va en `appsettings.json`. Sin clave, todo sigue andando y el botón
contesta que la lectura con IA no está configurada.

**Base de datos:** sin migración. El PDF ya está en `EstudioArchivos` desde el paso 23.

---

## Cómo se construyó con IA

Todo el código de la Parte 3 lo escribió Claude Code. Nosotros aprobamos el plan,
revisamos el diff y probamos a mano.

1. Rama nueva desde `main` actualizado:
   ```
   git checkout main
   git pull
   git checkout -b feature/borrador-resultado-ia
   ```
2. Abrir Claude Code en la carpeta del repo (`claude`) y verificar que aparecen la
   skill (escribir `/` y buscar `paso-chronosalud`) y el subagente (`/agents`).
3. Prompt usado:
   ```
   /paso-chronosalud paso 24: borrador del resultado con IA. Implementá la
   "Parte 3" de docs/tp-desarrollo-con-ia.md tal como está diseñada. Si algo del
   diseño choca con el código real, avisame en el plan antes de cambiarlo. Al
   final, además de la bitácora y CAMBIOS-API.md, completá la sección "Registro de
   la implementación" de docs/tp-desarrollo-con-ia.md con los datos reales.
   ```
4. Revisar el plan que propone (etapa 2) y aprobarlo o corregirlo.
5. Dejar que implemente, pruebe y llame al revisor. Leer el resumen final.

## Registro de la implementación

> Esta sección la completa Claude Code al terminar el paso 24 (etapa 5 de la skill).

| Dato | Valor |
|---|---|
| Rama | |
| Archivos agregados | |
| Archivos modificados | |
| Pruebas nuevas / total | |
| Hallazgos del revisor (alto / medio / bajo) | |
| Qué se corrigió por el revisor | |
| Cambios al diseño respecto de este documento | |

---

## Cómo probarlo

**Con pruebas automáticas** (desde la carpeta del repo):

```
dotnet test
```

Tienen que pasar todas, las nuevas incluidas.

**A mano**, con la API y la Web levantadas (`levantar.ps1`) y los datos de prueba:

1. Cargar la clave: `dotnet user-secrets set "IA:ApiKey" "<clave>" --project ChronoSaludApi` y reiniciar la API.
2. Entrar como la doctora de la demo (`laura.gomez@chronosalud.demo`, contraseña
   `Chrono2026!`, la del README). El seeder no crea estudios, así que primero hay que
   pedir uno: abrir uno de sus turnos, tocar **Pedir estudio**, tipo «Sangre»,
   descripción «Hemograma, glucemia y perfil lipídico».
3. Ir a **Estudios**, tocar **Cargar resultado** en ese estudio, elegir
   `docs/ejemplos/informe-laboratorio-ejemplo.pdf` (un informe inventado, sin datos
   reales) y tocar **Proponer el texto con IA**.
   - Esperado: el cuadro «Resultado» se llena con un texto en voseo que nombra la
     glucemia, el colesterol total y el LDL como altos (son los tres que el informe
     marca), y aparece el aviso de revisar. El estudio sigue «Esperando resultado».
4. Escribir algo en el cuadro y volver a tocar el botón.
   - Esperado: lo escrito sigue ahí y el borrador aparece abajo, después de «Propuesta de la IA:».
5. Tocar **Guardar resultado**.
   - Esperado: «Resultado guardado. Le avisamos al paciente…». Como paciente, el texto
     se ve en «Mis estudios».
6. Subir un PDF que no sea un informe (por ejemplo un apunte).
   - Esperado: el borrador dice en una frase que no parece un informe de estudio.
7. Sin clave (`dotnet user-secrets remove "IA:ApiKey" --project ChronoSaludApi`, reiniciar la API) tocar el botón.
   - Esperado: mensaje «La lectura con IA no está configurada.» y la pantalla sigue andando.
8. En Scalar (`http://localhost:5001/scalar`), con el token de un paciente:
   `POST /estudios/{id}/resultado/borrador`.
   - Esperado: `403`.

## Límites y riesgos

- **Datos de salud a un tercero.** El PDF del laboratorio puede traer el nombre del
  paciente, y se le manda a la API de Claude. Para el TP usamos solo informes de
  ejemplo. Antes de usarlo con pacientes reales habría que revisar la ley de datos
  personales (Ley 25.326), el consentimiento del paciente y las condiciones del
  proveedor.
- **La IA se puede equivocar.** Por eso nunca guarda sola: el texto lo revisa y lo
  guarda una persona con su usuario, y queda el PDF original para comparar.
- **Costo.** Cada borrador es un pedido pago a la API de Claude. El tope de 20 por hora
  por usuario y el modelo chico por defecto lo limitan.
- **PDF escaneados o muy largos.** Pueden tardar más o leerse peor. El tope de 5 MB del
  paso 23 sigue valiendo.
- **La skill y el subagente no reemplazan la revisión.** Ordenan el trabajo y avisan,
  pero el plan y el diff los aprobamos nosotros.

## Archivos de esta entrega

```
.claude/
├── agents/
│   └── revisor-permisos.md        subagente revisor (Parte 2)
├── settings.json                  ya existía: bloquea dotnet ef
└── skills/
    └── paso-chronosalud/
        ├── SKILL.md               la skill (Parte 1)
        └── plantillas.md          plan, bitácora y CAMBIOS-API
docs/
├── ejemplos/
│   └── informe-laboratorio-ejemplo.pdf   informe inventado para probar la Parte 3
└── tp-desarrollo-con-ia.md        este documento
```

Más los archivos de código de la Parte 3, listados en el "Registro de la implementación".
