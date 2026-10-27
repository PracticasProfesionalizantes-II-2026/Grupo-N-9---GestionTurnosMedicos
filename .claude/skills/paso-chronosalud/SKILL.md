---
name: paso-chronosalud
description: Agrega una funcionalidad o mejora a ChronoSalud (ChronoSaludApi + ChronoSaludWeb) siguiendo el método de "pasos" del grupo, de punta a punta, con plan aprobado, pruebas, revisión de permisos y documentación. Usala cuando pidan un paso nuevo, una funcionalidad, un endpoint, una pantalla o un cambio que toque la API o la Web.
when_to_use: 'Pedidos como: paso 24, agregá una funcionalidad, nuevo endpoint, nueva pantalla, implementá esto en la API y la Web.'
argument-hint: "[número y nombre del paso] [qué tiene que hacer]"
---

# Paso nuevo en ChronoSalud

Pedido: $ARGUMENTS

Seguí las cinco etapas en orden. No saltees ninguna y no pases a la siguiente sin
cumplir lo que pide la anterior. Hablale a Francis en voseo, sin jerga y en frases cortas.

## Etapa 1: entender (solo lectura)

1. Mirá en qué rama estás con `git status` y `git branch --show-current`.
   - Si estás en `main`, **no sigas**: cualquier push a `main` publica la API y la
     Web en Azure (`.github/workflows/`). Proponé una rama `feature/<nombre-corto>`
     y esperá el OK.
2. Leé, en este orden:
   - `README.md`
   - `docs/diseno.md` (principios y componentes)
   - `docs/plan-funcionalidades.md`, apartado "Reglas de todas las fases"
   - la última entrada de `docs/bitacora.md`
   - el último apartado de `CAMBIOS-API.md`
3. Ubicá el código que vas a tocar y **leelo en el archivo real**. Lo que digan los
   documentos puede estar desactualizado: si chocan, manda el código.
4. Si el pedido se puede entender de dos maneras que llevan a cosas distintas,
   preguntá antes de planificar. Una sola pregunta, con opciones.

## Etapa 2: plan (sin editar nada)

Escribí el plan con la plantilla de [plantillas.md](plantillas.md#plan). Tiene que tener:

- cada archivo que se agrega o se modifica, con ruta y líneas (`Archivo.cs:40-62`);
- permisos: quién puede hacer qué, resuelto con el token;
- si hay migración, y el orden de despliegue (base, API, Web);
- riesgos y a quién le pega el cambio (Scalar, Postman, el seeder);
- cómo probarlo, con pasos exactos y el resultado esperado.

Mostralo y **esperá la aprobación de Francis**. Sin aprobación no se edita nada.

## Etapa 3: implementar

Reglas que no se discuten (salen de los pasos 1 a 23):

**API (`ChronoSaludApi/`)**
- Quién llama sale del token: `Solicitante.Desde(ctx.User)`. Nunca del cuerpo, la
  ruta ni un campo oculto.
- Permisos y dueño del recurso se validan en la API, en la capa `Logica/`. La Web
  solo oculta botones.
- Cada endpoint nuevo lleva `.RequireAuthorization(...)` con los roles exactos.
  Un recurso ajeno se contesta como inexistente (404), como en `TurnoLogica`.
- Cambios aditivos: los pedidos que andaban tienen que seguir andando. Campos nuevos
  de un DTO, opcionales y al final.
- La lógica devuelve tuplas `(resultado, error)` y el endpoint traduce a 400, 403,
  404, 409 o 429. Nada de excepciones para casos esperados.
- Servicios externos (correo, IA, lo que sea) van detrás de una interfaz, con una
  implementación "sin configurar" que no rompe nada, como `CorreoSinConfigurar`.
- Claves y contraseñas: solo en user-secrets (local) o en la configuración del App
  Service (Azure). **Nunca** en `appsettings.json` ni en el código.
- Archivos subidos: se valida tamaño y firma (los primeros bytes), no la extensión.

**Web (`ChronoSaludWeb/`)**
- Sin JavaScript nuevo y sin CDN. Lo que haga falta, con formularios, enlaces,
  `formaction`, `popover` y `:has()`.
- Componentes y tokens de `docs/diseno.md` (`tarjeta`, `boton-*`, `campo`, `chip`,
  `aviso`). Clases de Tailwind escritas enteras, nunca armadas por partes.
- Textos en voseo, en sentence case y sin palabras técnicas.
- Los errores de la API vuelven a la misma pantalla, junto al campo que corresponde.
- Si tocaste clases de Tailwind: `dotnet build ChronoSaludWeb -t:CompilarCss`.

**Base de datos**
- `dotnet ef` está bloqueado para vos (`.claude/settings.json`). Si hace falta una
  migración, frená y dale a Francis los comandos exactos: `migrations add`, el
  script idempotente desde la última migración aplicada en Azure, y el orden.
- Nunca corras nada contra la base de Azure.

**Pruebas (`ChronoSalud.Tests/`)**
- xUnit con los repositorios falsos de `ChronoSalud.Tests/Falsos/`. Si falta un
  falso, se agrega ahí.
- Por cada regla nueva: el caso que anda, el que no tiene permiso y el que falla
  (servicio caído, archivo malo, dato inexistente).
- Al terminar: `dotnet build` y `dotnet test`. Las dos tienen que pasar: los
  workflows corren las mismas pruebas antes de publicar.

## Etapa 4: revisar

1. Pedile al subagente `revisor-permisos` que revise el diff de la rama contra
   `main`: "Usá el subagente revisor-permisos para revisar el diff contra main".
2. Corregí todo lo que marque como **alto** o **medio**. Lo **bajo**, listalo para
   que decida Francis.
3. Volvé a correr `dotnet test` después de corregir.

## Etapa 5: documentar y cerrar

1. Agregá la entrada del paso en `docs/bitacora.md` con la plantilla de
   [plantillas.md](plantillas.md#bitácora).
2. Si cambió la API, agregá el apartado del paso en `CAMBIOS-API.md`
   ([plantillas.md](plantillas.md#cambios-api)).
3. Si hay configuración nueva, agregala al `README.md`, con el nombre del ajuste en
   local (user-secrets) y en Azure (App Service, con `__`).
4. No hagas commit ni push salvo que Francis lo pida.
5. Cerrá con un resumen corto:
   - archivos tocados (lista);
   - pruebas: cuántas nuevas y cuántas en total, y si pasaron;
   - lo que dijo el revisor y qué se corrigió;
   - cómo probarlo a mano: pantalla o endpoint, pasos y resultado esperado;
   - qué tiene que hacer Francis (migración, ajustes en Azure, orden de despliegue).
