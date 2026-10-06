# Guía de diseño de ChronoSalud

Cómo se ve y cómo se comporta la interfaz de ChronoSaludWeb. Vale para cualquier pantalla nueva y para las que se vayan retocando. El prototipo que la acompaña está en `docs/prototipos/inicio-paciente.html` y se abre con doble clic.

## Principios

1. **Superficies con borde y sombra.** El fondo de la página es más oscuro que las piezas: tarjetas, menús y campos son superficies claras (blancas en el tema claro) con borde y sombra marcados, y el teal queda como acento. La sombra tiene un significado fijo (ver "Niveles de superficie") y nunca es lo único que comunica algo.
2. **Accesible por defecto.** Texto con contraste 4,5:1 como mínimo; bordes de campos y componentes con 3:1; foco siempre visible; nada depende solo del color.
3. **Sin JavaScript nuevo y sin CDN.** Interacciones con HTML y CSS (`popover`, `:has()`, `:focus-visible`). Fuentes e íconos se sirven desde el propio sitio.
4. **Una decoración grande por pantalla.** El motivo de marca aparece una vez; el resto es contenido.
5. **Textos en voseo y en sentence case**, sin jerga técnica.

## Tokens

Viven en el `@theme` de `ChronoSaludWeb/wwwroot/css/app.css`. El tema oscuro redefine los mismos nombres bajo `[data-theme="dark"]`; por eso las vistas no usan `dark:`.

### Color (tema claro)

| Token | Valor | Uso |
|---|---|---|
| `canvas` | `#EFE6DA` | Fondo de la página, más oscuro que las tarjetas |
| `surface` | `#FBF7F1` | Barra superior y barra inferior del celular |
| `card` | `#FFFFFF` | Tarjetas, menús flotantes y botón secundario |
| `hero` | `#F4EFE9` | Banner del inicio y portada, para que el motivo de marca se vea igual |
| `field` | `#FFFFFF` | Fondo de los campos |
| `ink` | `#1B2627` | Texto principal |
| `muted` | `#5A554E` | Texto secundario |
| `primary` | `#0B6E75` | Teal. Acción principal, ítem activo del menú, círculo de ícono, enlaces y foco |
| `primary-hover` | `#0B5F66` | Teal oscuro. Hover del botón primario y números grandes de las métricas |
| `primary-deep` | `#08464B` | Teal más oscuro. Título dentro del encabezado de una tarjeta y botón primario presionado |
| `primary-border` | `#08565C` | Borde del botón primario |
| `on-primary` | `#FFFFFF` | Texto e íconos sobre `primary` |
| `primary-soft` | `#D6EEF0` | Encabezado de una tarjeta, hover de ítems de menú y de filas (al 50 %) |
| `primary-line` | `#A8D5DB` | Solo decorativo: motivo de marca |
| `contorno` | `#8CC4C9` | Borde de 1,5 px de las tarjetas principales y borde inferior del encabezado de una tarjeta |
| `contorno-suave` | `#D3C4B1` | Borde de las tarjetas secundarias, de la barra superior y de los menús flotantes |
| `control` | `#8F7C66` | Borde de campos, botones secundarios y componentes |
| `border` | `#E3D8C8` | Solo divisores dentro de una tarjeta |
| `accent` | `#B08268` | Castaño, solo decorativo: un detalle del motivo |
| `accent-ink` | `#85593F` | Castaño para texto o ícono |
| `accent-soft` | `#F2E7E1` | Beige. Fondo de la etiqueta "Próximamente" |
| `status-pendiente` | `#96450A` | Íconos y avisos |
| `status-confirmado` | `#116B33` | Íconos y mensajes de éxito |
| `status-completado` | `#075E91` | Íconos |
| `status-cancelado` | `#B91C1C` | Íconos, errores y botón de peligro |

Cada estado tiene además tres tokens para chips y avisos: `-soft` (fondo), `-line` (borde) e `-ink` (texto del chip, un escalón más oscuro que el base). "Confirmado" es `#CBE7D5` / `#9CCBAF` / `#0E5330`; pendiente `#F6DEC4` / `#DDB27F` / `#7A3A06`; completado `#CFE3F2` / `#9DC2DF` / `#0A4E7A`; cancelado `#F6D4D2` / `#E3A5A1` / `#9B1818`.

El castaño y el beige son el terciario: **una sola pieza por pantalla** además del motivo. Nunca en botones ni en estados.

En el tema oscuro los nombres son los mismos y cambian los valores, con la misma lógica: el fondo es el escalón más oscuro (`canvas #16130F`), las barras suben uno (`surface #1F1B16`) y las tarjetas otro (`card #25201A`, que también es el `hero`). Los que más importan: `primary #2F9FA9` (el teal se aclara para leerse como texto), `on-primary #0B1F22` (los botones llevan tinta oscura en vez de blanco), y `primary-hover #4DB8C1` y `primary-deep #9ED8DD`, que aclaran en vez de oscurecer. Por eso el texto de un botón relleno se escribe siempre con `text-on-primary`, nunca con `text-white`.

### Sombras, radios y tipografía

Los colores de las sombras son tokens (`sombra-teal`, `sombra`, `sombra-media`, `sombra-barra`, `sombra-flotante`, `sombra-boton`) porque Tailwind copia la forma de la sombra en la utilidad y solo deja el color como variable; así el tema oscuro las cambia a negro.

| Token | Valor (claro) | Para qué |
|---|---|---|
| `shadow-elevado` | `0 10px 24px rgba(8,70,75,.20), 0 2px 4px rgba(70,48,25,.12)` | Tarjetas principales |
| `shadow-elevado-sm` | `0 8px 18px rgba(70,48,25,.16), 0 1px 3px rgba(70,48,25,.12)` | Tarjetas secundarias |
| `shadow-elevado-xs` | `0 1px 3px rgba(70,48,25,.12)` | Botón secundario, opciones y tarjeta secundaria presionada |
| `shadow-boton` | `0 3px 8px rgba(11,110,117,.35)` | Botón primario |
| `shadow-barra` / `shadow-barra-inferior` | `0 2px 8px rgba(70,48,25,.10)` (hacia arriba en la inferior) | Barras de navegación |
| `shadow-flotante` | `0 12px 28px rgba(70,48,25,.22)` | Menús flotantes |
| `inset-shadow-hundido` | `inset 0 1px 2px rgba(70,48,25,.12)` | Campos, botón secundario presionado, opción seleccionada |
| `inset-shadow-presionado` | `inset 0 2px 4px rgb(0 0 0 / .3)` | Botón primario presionado |
| `rounded-badge` 6 px, `rounded-input` 10 px, `rounded-card` 16 px, `rounded-modal` 18 px | | Chips; campos y botones; tarjetas y menús; diálogos |

Tipografía: Plus Jakarta Sans, servida desde `wwwroot/fonts` (woff2 variable, licencia OFL). Pesos en uso: 500, 600 y 700. Títulos de pantalla en `text-2xl font-bold` (`text-3xl` en el saludo del inicio a partir de 640 px), títulos de sección en `text-base font-semibold`, cuerpo en `text-sm` o `text-base`, y `text-xs` solo en chips y en la barra inferior.

Medidas en `rem`, para que acompañen el ajuste de tamaño de texto (`data-scale`). Quedan en `px` solo los trazos finos: bordes de 1 px, el contorno de foco y el desplazamiento de las sombras.

## Niveles de superficie

| Nivel | Se usa en | Cómo se logra |
|---|---|---|
| **Principal** | Estadísticas, listas, tablas, paneles, formularios | Fondo `card`, borde 1,5 px `contorno`, `rounded-card` y `shadow-elevado`. Clase `tarjeta` |
| **Secundaria** | Accesos rápidos e ítems clicables | Fondo `card`, borde 1,5 px `contorno-suave` y `shadow-elevado-sm`. Clases `tarjeta tarjeta-enlace` |
| **Hundido** | Campos, botón presionado, opción seleccionada | Sombra interior. Clase `campo` o `inset-shadow-hundido` |
| **Plano** | Lo deshabilitado | Sin fondo ni sombra, borde punteado `contorno-suave`, contenido con `opacity-60` y la etiqueta "Próximamente" a opacidad completa |

Reglas que no se negocian:

- El hundido no se usa para decorar ni para lo deshabilitado.
- La sombra sola no delimita un control: los botones llevan relleno teal o borde `control`, y los campos llevan borde `control`.
- El hover cambia el borde o el fondo a teal; no agranda la sombra. Al presionar, la sombra se achica.
- Lo seleccionado (ítem del menú, opción de Ajustes) se marca con relleno teal o fondo `primary-soft` **y** con peso de fuente, además del color.
- Lo deshabilitado no es un enlace ni recibe foco. El motivo se escribe a la vista, no en un `title`.

## Foco

Una regla global en `app.css` dibuja un contorno sólido de 2 px en `primary`, separado 2 px, sobre enlaces, botones, campos, selects, textareas, `summary` y elementos con `tabindex`, solo con `:focus-visible`. Está fuera de las capas de Tailwind para que ninguna utilidad `outline-none` la pueda anular.

En las vistas no se escribe ninguna clase de foco: ni `focus:ring-*` ni `focus:outline-none`. La única excepción son los controles ocultos con `sr-only` (un radio o un archivo detrás de su etiqueta): como el contorno del control no se ve, se le pone al contenedor con `has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-primary`.

## Componentes

| Componente | Marcado |
|---|---|
| Botón primario | `class="boton boton-primario"`: relleno `primary`, borde `primary-border`, `shadow-boton`. Hover `primary-hover`, presionado `primary-deep`. Uno por bloque como máximo |
| Botón secundario | `class="boton boton-secundario"`: fondo `card`, borde `control`, texto `ink`. El hover pasa el borde a teal y el fondo a `primary-soft` |
| Botón de peligro | `class="boton boton-peligro"` para la acción destructiva de una pantalla de confirmación. Una acción destructiva menor va como secundario con `border-status-cancelado text-status-cancelado` |
| Botón de texto | `class="boton-texto"`: sin relleno ni sombra, para "Volver a…" y para las acciones dentro de una fila (ahí se le suma `px-2 py-1`) |
| Campo | `class="campo"` más su `<label>`, también en selects y textareas. Con `w-auto` cuando no tiene que ocupar todo el ancho. El error va debajo, en `text-status-cancelado`, y el campo toma borde rojo con `aria-invalid="true"` o con la clase que pone ASP.NET |
| Tarjeta | `class="tarjeta"`. Si lleva lista, `overflow-hidden` y divisores `border-border`; las filas marcan el hover con `hover:bg-primary-soft/50` |
| Encabezado de tarjeta | `class="tarjeta-encabezado"` en el `<header>` del panel o en la fila de títulos de una tabla: fondo `primary-soft`, borde inferior `contorno` y título en `text-primary-deep` |
| Hero | `class="tarjeta tarjeta-hero"` en el banner del inicio y la portada: tarjeta principal sobre `hero` |
| Tarjeta que es enlace | `class="tarjeta tarjeta-enlace"`: tarjeta secundaria. El hover pasa el borde a `contorno` y al presionarla la sombra se achica |
| Chip | Parcial `_Chip` con `ChipViewModel { Texto, Tono, Capitalizar }`. Tonos: `Neutro` (fondo `card`, borde `control`, texto `muted`), `Exito`, `Aviso`, `Info`, `Peligro` (fondo `-soft`, borde `-line` y texto `-ink` de su estado) y `Proximamente`. El estado de un turno usa `_BadgeEstado`, que elige el tono |
| Círculo de ícono | `class="circulo-icono"` más el tamaño y `rounded-full`: relleno `primary`, ícono `on-primary` con trazo 1,9. En un estado vacío de error o de aviso se le suma `bg-status-cancelado` o `bg-status-pendiente` |
| Acceso rápido | Tarjeta que es enlace, con el ícono dentro de un `circulo-icono` de `size-12` y la etiqueta debajo |
| Estado vacío | Parcial `_EstadoVacio` dentro de una `tarjeta`: ícono en círculo, título, una línea de ayuda y **un botón** que lleve a la acción que lo resuelve. `Tono` distingue "no hay nada" (`Neutro`), "falta un paso" (`Aviso`) y "no se pudo cargar" (`Error`); `Nivel` es 2 cuando cuelga del `h1` de la pantalla y 3 dentro de un panel |
| Filtro seleccionable | Radios dentro de un `label` con borde `control` y fondo `card`; elegido, `has-[:checked]:border-primary has-[:checked]:bg-primary-soft has-[:checked]:inset-shadow-hundido` (Ajustes) |
| Aviso | `rounded-input border` con `border-status-X-line bg-status-X-soft` y el texto en `status-X` (en `ink` si lleva un enlace) |
| Barra superior | `class="barra-superior"`: fondo `surface`, borde inferior `contorno-suave` y `shadow-barra`. El ítem activo va relleno en `primary` con texto `on-primary` |
| Menú de cuenta | `<button popovertarget>` más un `<div popover class="menu-flotante">`. Abre, cierra con Escape o clic afuera y devuelve el foco sin JavaScript. Se ubica en una posición fija bajo el encabezado y, donde el navegador soporta anclaje CSS, pegado al botón |
| Barra inferior | Solo por debajo de 768 px. Fondo `surface`, borde `contorno-suave`. Cinco ítems como máximo, ícono y etiqueta siempre visible, `aria-current="page"` en el actual (relleno `primary`). El resto va en "Más" |

Área táctil mínima: `min-h-11` (44 px con el tamaño de texto normal).

Íconos: estilo Lucide en línea, `viewBox="0 0 24 24"`, trazo 1,5 (1,9 dentro de un `circulo-icono`, por CSS), sin relleno, `stroke="currentColor"` y `aria-hidden="true"`. Los trazos compartidos viven en `Models/IconosLucide.cs`.

## Motivo de marca

Una línea de latido que entra por la izquierda y termina en el centro de unos arcos concéntricos tipo reloj, con doce marcas de hora y dos agujas, emparentado con el favicon.

- SVG en línea con `aria-hidden="true"` (parcial `_MotivoMarca`). Arcos y agujas en `primary-line`, latido en `primary` y un solo arco en `accent`.
- Una vez por pantalla: banner del inicio, portada y pantallas de acceso.
- Nunca debajo de texto. En pantallas angostas se achica y pasa arriba del contenido.
- En el inicio el latido se dibuja una sola vez al cargar: el contenedor lleva la clase `motivo-animado`. En login y registro queda quieto.

## Movimiento

Todo el movimiento es CSS y está dentro de `@media (prefers-reduced-motion: no-preference)`: quien pidió menos movimiento en su sistema no ve ninguno.

| Qué | Cómo |
|---|---|
| Cambio de estado | `boton`, `boton-texto`, `tarjeta-enlace` y `campo` transicionan sombra, fondo, color y borde en 150 ms. El hover cambia borde o fondo; `:active` achica o hunde la sombra |
| Entre páginas | `@view-transition { navigation: auto; }` con un fundido de 180 ms. Las dos páginas tienen que pedirlo |
| Una vista sin fundido | Declara `@view-transition { navigation: none; }` en su sección `Head`. Lo hace Recetas/Crear, porque «+» y «−» la repintan anclada a otra fila |
| Menús flotantes | Entran con opacidad y un desplazamiento de 0,375rem, desde `@starting-style`. Cerrados no se les toca la opacidad |
| Listados | Las filas entran escalonadas con `fila-animada` |
| Motivo de marca | El latido se dibuja una vez, en 1,4 s |

No se anima nada que mueva el layout (alto, ancho, márgenes).

## Textos

- Voseo: "Ingresá", "Elegí", "No tenés turnos".
- Sentence case: mayúscula solo en la primera palabra y en nombres propios. Sin mayúsculas forzadas por CSS en los títulos.
- Sin jerga: nada de "API", "endpoint" ni rutas como `POST /doctores` en un mensaje al usuario.
- Un verbo por acción en todo el sitio: "Ingresar" para entrar, "Agendar turno" para pedir uno.
- Los errores dicen qué pasó y qué hacer: "No pudimos cargar tu inicio. Probá de nuevo en un momento."
- Un enlace o botón se entiende leído solo: "Ver todos mis turnos", no "Ver más".

## Tabla de contraste

Relaciones medidas con la fórmula de WCAG 2. Mínimos: 4,5 para texto, 3 para bordes de campos y componentes.

### Tema claro

| Par | Relación | Mínimo |
|---|---|---|
| `ink` sobre `card` / `canvas` | 15,52 / 12,56 | 4,5 |
| `muted` sobre `card` / `canvas` / `surface` / `hero` | 7,38 / 5,98 / 6,92 / 6,46 | 4,5 |
| `muted` sobre `primary-soft` | 6,10 | 4,5 |
| `primary` sobre `card` / `canvas` / `surface` / `primary-soft` | 6,00 / 4,86 / 5,62 / 4,95 | 4,5 |
| `primary-hover` sobre `card` (números de métricas) | 7,39 | 4,5 |
| `primary-deep` sobre `primary-soft` (título de encabezado) | 8,73 | 4,5 |
| `on-primary` sobre `primary` / `primary-hover` / `primary-deep` | 6,00 / 7,39 / 10,56 | 4,5 |
| `ink` / `muted` / `primary` sobre el hover de fila (`primary-soft` al 50 % sobre `card`) | 14,07 / 6,69 / 5,44 | 4,5 |
| `accent-ink` sobre `accent-soft` | 4,94 | 4,5 |
| `control` sobre `card` / `canvas` | 4,01 / 3,24 | 3 |
| Contorno de foco (`primary`) sobre `canvas` | 4,86 | 3 |
| Chip confirmado / pendiente / completado / cancelado (`-ink` sobre `-soft`) | 6,93 / 6,64 / 6,68 / 6,02 | 4,5 |
| Aviso de éxito / error (`status-X` sobre `-soft`) | 5,02 / 4,70 | 4,5 |
| `ink` sobre el fondo del aviso de pendiente | 11,95 | 4,5 |
| `status-cancelado` sobre `field` (error de campo) / blanco sobre `status-cancelado` | 6,47 / 6,47 | 4,5 |
| Ícono `on-primary` sobre el círculo de pendiente | 6,66 | 3 |

Dos cosas a recordar: dentro de un aviso los enlaces van en `ink` subrayado; y `primary-line`, `contorno`, `contorno-suave`, `accent` y `border` no llegan a 3:1 a propósito, así que no llevan información: lo que separa una tarjeta del fondo es el salto de `card` a `canvas` más la sombra.

### Tema oscuro

| Par | Relación | Mínimo |
|---|---|---|
| `ink` sobre `canvas` / `card` | 15,66 / 13,66 | 4,5 |
| `muted` sobre `card` / `surface` / `primary-soft` | 6,00 / 6,36 / 5,53 | 4,5 |
| `primary` sobre `card` / `surface` / `primary-soft` | 5,12 / 5,42 / 4,72 | 4,5 |
| `primary-hover` sobre `card` | 6,88 | 4,5 |
| `primary-deep` sobre `primary-soft` | 9,45 | 4,5 |
| `on-primary` sobre `primary` / `primary-hover` / `primary-deep` | 5,39 / 7,25 / 10,80 | 4,5 |
| `accent-ink` sobre `accent-soft` | 5,26 | 4,5 |
| `control` sobre `card` / `field` | 4,03 / 4,62 | 3 |
| Chips (pendiente, confirmado, completado, cancelado) | 7,76 / 7,39 / 7,95 / 7,58 | 4,5 |
| Aviso de éxito / error | 4,58 / 5,04 | 4,5 |
| `on-primary` sobre `status-cancelado` (botón de peligro) | 5,64 | 4,5 |
| Contorno de foco (`primary`) sobre `canvas` | 5,87 | 3 |

En oscuro no alcanza con aclarar el teal hasta 4,5:1 sobre `canvas`: en ese punto (`#178E98`) ni el blanco (3,92) ni la tinta (4,34) pasan como texto del botón. `#2A9CA6` es el primer valor en el que pasan todos los pares; `#2F9FA9` deja un poco de margen.

## Qué falta aplicar

El barrido de F5 llevó todas las pantallas a esta guía. Lo que queda afuera a propósito:

- El mensaje "No se pudo conectar con la API…" de `ApiClient`, que se decidió dejar como está.
- jQuery y la validación del cliente, que no forman parte del rediseño.
- La pantalla de "Mis coberturas" del paciente, que sigue como "Próximamente".
