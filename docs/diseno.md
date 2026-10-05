# Guía de diseño de ChronoSalud

Cómo se ve y cómo se comporta la interfaz de ChronoSaludWeb. Vale para cualquier pantalla nueva y para las que se vayan retocando. El prototipo que la acompaña está en `docs/prototipos/inicio-paciente.html` y se abre con doble clic.

## Principios

1. **Soft UI con reglas.** Las piezas comparten el color del fondo y el volumen lo da la sombra. El relieve tiene un significado fijo (ver "Niveles de relieve") y nunca es lo único que comunica algo.
2. **Accesible por defecto.** Texto con contraste 4,5:1 como mínimo; bordes de campos y componentes con 3:1; foco siempre visible; nada depende solo del color.
3. **Sin JavaScript nuevo y sin CDN.** Interacciones con HTML y CSS (`popover`, `:has()`, `:focus-visible`). Fuentes e íconos se sirven desde el propio sitio.
4. **Una decoración grande por pantalla.** El motivo de marca aparece una vez; el resto es contenido.
5. **Textos en voseo y en sentence case**, sin jerga técnica.

## Tokens

Viven en el `@theme` de `ChronoSaludWeb/wwwroot/css/app.css`. El tema oscuro redefine los mismos nombres bajo `[data-theme="dark"]`; por eso las vistas no usan `dark:`.

### Color (tema claro)

| Token | Valor | Uso |
|---|---|---|
| `canvas` | `#F4EFE9` | Crema. Fondo de la página y de las piezas con relieve |
| `surface` | `#FBF7F2` | Menús flotantes y resaltados |
| `field` | `#FFFFFF` | Fondo de los campos |
| `ink` | `#1C2321` | Texto principal |
| `muted` | `#5B6661` | Texto secundario |
| `primary` | `#0D717A` | Teal. Acción principal, enlaces, íconos y foco |
| `primary-hover` | `#0A5F67` | Hover y presionado del botón primario |
| `on-primary` | `#FFFFFF` | Texto e íconos sobre `primary` |
| `primary-soft` | `#D7EDF0` | Círculo de los íconos, hover de ítems de menú |
| `primary-line` | `#A8D5DB` | Solo decorativo: motivo de marca |
| `control` | `#7F7A70` | Borde de campos, botones secundarios y componentes |
| `border` | `#E3DAD1` | Solo divisores dentro de una tarjeta |
| `accent` | `#B08268` | Castaño, solo decorativo: un detalle del motivo |
| `accent-ink` | `#85593F` | Castaño para texto o ícono |
| `accent-soft` | `#F2E7E1` | Beige. Fondo de la etiqueta "Próximamente" |
| `status-pendiente` | `#96450A` | Chip y avisos |
| `status-confirmado` | `#116B33` | Chip y mensajes de éxito |
| `status-completado` | `#075E91` | Chip |
| `status-cancelado` | `#B91C1C` | Chip y errores |

El castaño y el beige son el terciario: **una sola pieza por pantalla** además del motivo. Nunca en botones ni en estados.

### Sombras, radios y tipografía

| Token | Para qué |
|---|---|
| `shadow-elevado` | Tarjetas |
| `shadow-elevado-sm` | Botones y hover de tarjetas que son enlace |
| `shadow-flotante` | Menús flotantes |
| `inset-shadow-hundido` | Campos, botón secundario presionado, filtro o ítem seleccionado |
| `inset-shadow-presionado` | Botón primario presionado |
| `rounded-badge` 6 px, `rounded-input` 10 px, `rounded-card` 14 px, `rounded-modal` 18 px | Chips; campos y botones; tarjetas y menús; diálogos |

Tipografía: Plus Jakarta Sans, servida desde `wwwroot/fonts` (woff2 variable, licencia OFL). Pesos en uso: 500, 600 y 700. Títulos de pantalla en `text-2xl font-bold` (`text-3xl` en el saludo del inicio a partir de 640 px), títulos de sección en `text-base font-semibold`, cuerpo en `text-sm` o `text-base`, y `text-xs` solo en chips y en la barra inferior.

Medidas en `rem`, para que acompañen el ajuste de tamaño de texto (`data-scale`). Quedan en `px` solo los trazos finos: bordes de 1 px, el contorno de foco y el desplazamiento de las sombras.

## Niveles de relieve

| Nivel | Se usa en | Cómo se logra |
|---|---|---|
| **Elevado** | Tarjetas y botones | Fondo `canvas` con sombra doble. Clases `tarjeta`, `boton-primario`, `boton-secundario` |
| **Hundido** | Campos, botón presionado, filtro o ítem de menú seleccionado | Sombra interior. Clase `campo` o `inset-shadow-hundido` |
| **Plano** | Lo deshabilitado | Sin sombra, borde `border`, contenido con `opacity-60` y la etiqueta "Próximamente" a opacidad completa |

Reglas que no se negocian:

- El hundido no se usa para decorar ni para lo deshabilitado.
- La sombra sola no delimita un control: los botones llevan relleno teal o borde `control`, y los campos llevan borde `control`.
- El hover no cambia de nivel; a lo sumo acorta la sombra.
- Lo seleccionado se marca con relieve hundido **y** con peso de fuente, además del color.
- Lo deshabilitado no es un enlace ni recibe foco. El motivo se escribe a la vista, no en un `title`.

## Foco

Una regla global en `app.css` dibuja un contorno sólido de 2 px en `primary`, separado 2 px, sobre enlaces, botones, campos, selects, textareas, `summary` y elementos con `tabindex`, solo con `:focus-visible`. Está fuera de las capas de Tailwind para ganarle a los `focus:outline-none` que quedaron en las vistas. En código nuevo no hace falta agregar clases de foco.

## Componentes

| Componente | Marcado |
|---|---|
| Botón primario | `class="boton boton-primario"`. Uno por bloque como máximo |
| Botón secundario | `class="boton boton-secundario"` |
| Campo | `class="campo"` más su `<label>`. El error va debajo, en `text-status-cancelado`, y el campo toma borde rojo con `aria-invalid="true"` o con la clase que pone ASP.NET |
| Tarjeta | `class="tarjeta"`. Si lleva lista, `overflow-hidden` y divisores `border-border` |
| Chip de estado | `class="chip bg-status-X/10 text-status-X"`; parcial `_BadgeEstado` |
| Acceso rápido | Tarjeta que es enlace, con el ícono dentro de un círculo `bg-primary-soft text-primary` de `size-12` y la etiqueta debajo |
| Estado vacío | Ícono en círculo, título, una línea de ayuda y **un botón** que lleve a la acción que lo resuelve |
| Filtro seleccionable | Radios dentro de `label.boton.boton-secundario` con `has-[:checked]:inset-shadow-hundido` |
| Menú de cuenta | `<button popovertarget>` más un `<div popover class="menu-flotante">`. Abre, cierra con Escape o clic afuera y devuelve el foco sin JavaScript |
| Barra inferior | Solo por debajo de 768 px. Cinco ítems como máximo, ícono y etiqueta siempre visible, `aria-current="page"` en el actual. El resto va en "Más" |

Área táctil mínima: `min-h-11` (44 px con el tamaño de texto normal).

Íconos: estilo Lucide en línea, `viewBox="0 0 24 24"`, trazo 1,5, sin relleno, `stroke="currentColor"` y `aria-hidden="true"`. Los trazos compartidos viven en `Models/IconosLucide.cs`.

## Motivo de marca

Una línea de latido que entra por la izquierda y termina en el centro de unos arcos concéntricos tipo reloj, con doce marcas de hora y dos agujas, emparentado con el favicon.

- SVG en línea con `aria-hidden="true"`. Arcos y agujas en `primary-line`, latido en `primary` y un solo arco en `accent`.
- Una vez por pantalla: banner del inicio, portada y pantallas de acceso.
- Nunca debajo de texto. En pantallas angostas se achica y pasa arriba del contenido.

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
| `ink` sobre `canvas` / `surface` / `field` | 14,00 / 15,01 / 16,01 | 4,5 |
| `muted` sobre `canvas` / `surface` / `field` | 5,22 / 5,60 / 5,97 | 4,5 |
| `muted` sobre `primary-soft` | 4,91 | 4,5 |
| `primary` sobre `canvas` / `surface` | 5,02 / 5,38 | 4,5 |
| `primary` sobre `primary-soft` | 4,72 | 4,5 |
| `on-primary` sobre `primary` / `primary-hover` | 5,74 / 7,38 | 4,5 |
| `accent-ink` sobre `canvas` / `accent-soft` | 5,25 / 4,94 | 4,5 |
| `control` sobre `canvas` / `surface` / `field` | 3,73 / 4,00 / 4,27 | 3 |
| Contorno de foco (`primary`) sobre `canvas` | 5,02 | 3 |
| Chip pendiente sobre su tinte | 5,05 | 4,5 |
| Chip confirmado sobre su tinte | 5,02 | 4,5 |
| Chip completado sobre su tinte | 5,26 | 4,5 |
| Chip cancelado sobre su tinte | 4,81 | 4,5 |
| `ink` sobre el fondo de un aviso | 12,14 | 4,5 |
| `status-cancelado` sobre `field` (error de campo) | 6,47 | 4,5 |

Dos cosas a recordar: dentro de un aviso los enlaces van en `ink` subrayado, porque `primary` sobre ese tinte da 4,35; y `primary-line`, `accent` y `border` no llegan a 3:1 a propósito, así que no llevan información.

### Tema oscuro

| Par | Relación | Mínimo |
|---|---|---|
| `ink` sobre `canvas` / `surface` | 15,66 / 14,48 | 4,5 |
| `muted` sobre `canvas` / `surface` | 6,88 / 6,36 | 4,5 |
| `accent-ink` sobre `canvas` / `accent-soft` | 6,54 / 5,26 | 4,5 |
| `control` sobre `canvas` / `surface` | 3,82 / 3,53 | 3 |
| Chips sobre su tinte (pendiente, confirmado, completado, cancelado) | 6,73 / 5,88 / 6,43 / 5,43 | 4,5 |
| `primary` sobre `canvas` / `surface` | 4,38 / 4,05 | 4,5 (**pendiente**) |
| `on-primary` sobre `primary` | 4,23 | 4,5 (**pendiente**) |

Las dos filas pendientes se resuelven juntas: aclarar `primary` a `#3AA9B3` y pasar `on-primary` a `#0B1F22` da 6,62 como texto y 6,09 en botones. No se puede hacer antes de reemplazar `text-white` por `text-on-primary` en los botones viejos, porque quedarían en 2,80.

## Qué falta aplicar

Esta guía describe el destino. Hasta que se haga el barrido general conviven con ella las tarjetas planas con borde, los campos con borde `primary-line` (1,59:1) y los títulos de sección en mayúsculas de las pantallas que todavía no se tocaron.
