# Bitácora ChronoSalud

## 2026-09-16 — laboratorio
**Hecho:** nada todavía, arrancando la vista de alta de pacientes.
**A medias:** —
**Sigue:** Pacientes/Crear con el plan acordado. Definir si
reemplaza a Admin/NuevoPaciente. Recompilar CSS a mano.
**Ojo:** el target CompilarCss ya no existe, usar la CLI de Tailwind.
## 2026-09-16 — laboratorio
**Hecho:** Pacientes/Crear completo y probado. Reemplaza a
Admin/NuevoPaciente (redirect 301). Registro real vía
RegistrarComoPacienteAsync. Permisos verificados con rol paciente.
**Sigue:** dashboards por rol desde los mockups de Figma.
**Pendiente API:** el alta no manda documento, fecha de nacimiento,
dirección, obra social, contacto de emergencia ni alergias — la API
no los acepta todavía. Hay TODO en PacientesController.
## 2026-10-01 — horarios, fase B (front)
**Hecho:** Turnos/Crear por pasos sin JS (especialidad → doctor o
"Cualquiera" → fecha → franjas libres como botones). Cada franja
postea idDoctor|inicio|fin; el 400/409 de la API vuelve a la pantalla
con el mensaje y las franjas recargadas. Ficha del doctor con horario
semanal y atajo "Pedir turno". Doctores/Index usa
/doctores/especialidades. Compila; falta probar a mano.
**Sigue:** prueba manual (paciente, admin desde ficha de paciente,
choque 409 con dos pestañas, teclado). Revisar el CSS generado.
**Pendiente:** pantalla de admin para editar horarios
(PUT /doctores/{id}/horarios).
## 2026-10-02 — medicamentos del vademécum
**Hecho:** `Medicamento` suma genérico, concentración, forma farmacéutica
y laboratorio (migración AgregarDatosVademecumAMedicamento). El seeder
carga los CSV de tools/Seed/data por POST /medicamentos, con
`--solo-medicamentos` y `--email` para Azure. Recetas/Crear arma la
etiqueta "COMERCIAL (genérico) concentración". Compila todo.
**A medias:** sin probar de punta a punta: no había una base con la
migración aplicada a la que conectarse.
**Sigue:** aplicar la migración (en Azure, con el script idempotente),
correr el seeder dos veces y revisar el desplegable.
**Ojo:** los user-secrets de la API apuntan a la base de Azure, así que
`dotnet run` y `dotnet ef database update` locales pegan ahí.
## 2026-10-03 — receta: filas dinámicas, "Otro..." y detalle
**Hecho:** la migración AgregarDatosVademecumAMedicamento ya está aplicada
y desplegada en Azure. Recetas/Crear arranca con 1 fila y suma/quita con
"+"/"−" sin JS (POST a AgregarFila/QuitarFila, hasta 10, vuelve anclado a
la fila). Opción "Otro..." con cuadro de texto por CSS `:has()`: la fila
apunta al medicamento marcador "Otro (ver indicaciones)" y el nombre se
guarda en Indicaciones como `Medicamento: <nombre>` + salto de línea +
indicaciones (tope 300 entre los dos). El detalle muestra genérico,
concentración y forma. La API no se tocó; el seeder carga el marcador.
Compilan Web y Seed, CSS recompilado.
**Sigue:** desplegar la Web, después correr el seeder con
`--solo-medicamentos` contra Azure (crea el marcador; hasta entonces
"Otro..." no aparece) y probar a mano.
**Ojo:** no borrar ni renombrar el marcador: la FK borra en cascada los
renglones de receta que lo usan. La Web no tiene pantalla para hacerlo,
pero la API sí lo permite (PUT/DELETE /medicamentos).
## 2026-10-04 — perfiles del admin, fase 1: partial de avatar
**Hecho:** `_Avatar` + `AvatarViewModel` (foto, iniciales o ícono de
persona; tamaños Chico/Mediano/Grande). Reemplaza el círculo copiado en
Pacientes/Index, Pacientes/Detalle, Turnos/Index, Turnos/Detalle y
Turnos/Cancelar. Recibe el nombre crudo: sin nombre ya no muestra "SD",
"?" ni "P#", muestra el ícono. Compila, CSS recompilado. La API no se tocó.
**A medias:** sin probar a mano. La rama con foto no tiene de dónde
salir hasta la fase 4.
**Sigue:** fase 2, edición de perfil de pacientes por el admin
(Usuarios/Editar desde la ficha), cuando Francis dé el OK.
**Pendiente API:** GET /usuarios (buscador), endpoints de foto y el
chequeo de dueño en PUT /usuarios/{id}; propuesta para llevar al grupo.
**Ojo:** Doctores/Index y Doctores/Detalle siguen con su círculo
terracota propio, a propósito. CLAUDE.md no está en el repo.
## 2026-10-04 — perfiles del admin, fase 2: edición de pacientes
**Hecho:** Usuarios/Editar (solo administrador, 403 al resto en GET y
POST) con dos tarjetas y dos formularios: cuenta (PUT /usuarios/{id}:
nombre, apellido, teléfono) y ficha del paciente (PUT /pacientes/{id}).
Se entra desde el botón "Editar perfil" de Pacientes/Detalle. Verifica
que el paciente sea de ese usuario, el 409 de DNI cae en el campo DNI y
el 403 de la API se traduce. Compila, CSS recompilado. La API no se tocó.
**A medias:** sin probar a mano. Pacientes/Detalle volvió al círculo
viejo (sin `_Avatar`): falta reponer esa línea de la fase 1.
**Sigue:** fase 3 (buscador, doctores y administradores) cuando exista
GET /usuarios; fase 4 (foto) cuando existan sus endpoints.
**Ojo:** vaciar un campo no borra el dato (la API ignora vacíos); la
pantalla lo avisa. Usuarios/Index todavía solo redirige a Cuentas.
PUT /usuarios/{id} sigue sin chequeo de dueño en la API.
## 2026-10-04 — perfiles del admin, etapa A: buscador de usuarios
**Hecho:** API (autorizado por el grupo, solo agregados): GET /usuarios
?buscar=&rol=&pagina=&limite= para administrador, con `UsuarioListaDto`
y `Buscar` en repositorio y lógica. Solo activos, orden por apellido y
nombre, paginado en SQL, rol inválido = 400, `TieneFoto` en false fijo.
Web: Usuarios/Index (buscador con filtro por rol y paginación de a 20),
tarjeta "Perfil de doctor" en Usuarios/Editar (PUT /doctores/{id}) y
tarjeta "Buscar y editar usuarios" en Cuentas. Editar ya resuelve solo
el IdPaciente y el IdDoctor con la fila del buscador. Compilan API y Web.
**A medias:** sin probar a mano (ni Scalar ni pantallas).
**Sigue:** etapa B (foto: tabla UsuarioFotos, endpoints y pantalla),
con migración que corre Francis. Etapa C queda como propuesta.
**Ojo:** la API no aplica migraciones al arrancar. Hay que desplegar la
API antes que la Web: sin GET /usuarios el buscador muestra error.
Pacientes/Detalle sigue con el círculo viejo en vez de `_Avatar`.

## 2026-10-04 — perfiles del admin, etapa B: foto
**Hecho:** API (autorizado por el grupo): entidad `UsuarioFoto` en tabla
aparte `UsuarioFotos` (PK = IdUsuario, cascada, sin navegación en
Usuario), repositorio, lógica y `UsuarioFotoEndpoints`: PUT (multipart,
campo "archivo", solo admin), GET (admin, doctor o el propio usuario) y
DELETE (solo admin) de /usuarios/{id}/foto. Valida 2 MB y firma de bytes
(JPEG, PNG, WebP). `TieneFoto` del buscador ya es real: única línea
existente modificada, en `UsuarioLogica.Buscar`.
Web: `ValidadorDeImagen`, multipart y bytes en `ApiClient`, acciones
SubirFoto / QuitarFoto / Foto en Usuarios, tarjeta "Foto" en Editar,
avatar con foto en el buscador y en Editar, y Pacientes/Crear sube la
foto después del alta. Compilan API y Web, CSS recompilado.
**A medias:** falta generar y aplicar la migración `AgregarUsuarioFoto`
(la corre Francis). Nada probado en ejecución.
**Sigue:** etapa C (chequeo de dueño en PUT /usuarios/{id}), solo propuesta.
**Ojo:** sin la migración aplicada falla también GET /usuarios (el
buscador consulta `UsuarioFotos`): aplicarla ANTES de desplegar la API.
La foto no usa `?v=`: se revalida con ETag (ver etapa B en el chat).
Usuarios/Foto en la Web es solo para administrador por ahora.

## 2026-10-04 — fotos en toda la app, fase 1: API
**Hecho:** GET /usuarios/fotos?pacientes=&doctores=&turnos= (hasta 100
ids por lista): devuelve qué pacientes, doctores o turnos (por su
paciente) tienen una foto que quien pregunta puede ver, con el
`IdUsuario` del dueño. Regla nueva del GET /usuarios/{id}/foto: foto de
doctor o administrador, cualquier autenticado; foto de paciente, solo
administrador, doctor o el propio paciente. Sin foto y sin permiso
contestan el mismo 404 "Foto no encontrada." (ya no hay 403). La regla
vive en `UsuarioFotoLogica.PuedeVer`. Compila. Sin migración.
**A medias:** sin probar en Scalar.
**Sigue:** fase 2 (Web: acción Foto solo con sesión, consulta por lote
y Pacientes), después Turnos y Doctores.
**Ojo:** cualquier doctor ve la foto de cualquier paciente, y el
registro abierto deja que cualquiera se registre como doctor por la API.

## 2026-10-04 — limpieza: nota del login y perfil de doctor (etapas 1 y 2)
**Hecho:** `CuentaController` ya no lee `Api:BaseUrl` (la nota del login
se había borrado en `23ed6d0`; quedaban el campo, el parámetro del
constructor y dos `ViewData`). Usuarios/Editar suma la tarjeta
"Completar perfil de doctor" (POST Usuarios/CrearPerfilDoctor ->
POST /doctores), visible solo si el usuario tiene rol doctor y la API
confirma que no tiene perfil. El enlace del alta de doctor fallida ahora
lleva a Usuarios/Editar. Compila, CSS recompilado. La API no se tocó.
**A medias:** sin probar a mano. Todavía NO se borró lo viejo: la
tarjeta de Cuentas, Admin/CompletarDoctor y su view model siguen ahí.
**Sigue:** etapa 3, borrar lo viejo, cuando Francis apruebe la etapa 2.
**Ojo:** si la búsqueda de usuarios falla, Editar no ofrece completar
el perfil (no puede confirmar que falte). `ApiClient` sigue mostrando la
URL de la API cuando no responde: se decidió no tocarlo.

## 2026-10-04 — limpieza: perfil de doctor, etapa 3 (borrar lo viejo)
**Hecho:** se quitó "Completar perfil de doctor" de Cuentas y se borraron
las dos acciones de `AdminController`, la vista y su view model. La
función vive ahora en Usuarios/Editar (tarjeta que aparece sola cuando a
un usuario con rol doctor le falta el perfil). Se conservan
`DoctorService.CrearAsync` e `IdUsuarioCreado`, que usa el alta de
doctor. Compila, CSS recompilado. La API no se tocó.
**A medias:** sin probar a mano.
**Sigue:** fotos en Pacientes, Turnos y Doctores (fases 2 a 4 del plan
de fotos), pendientes.
**Ojo:** la ruta vieja de la pantalla ya no existe (da 404).

## 2026-10-04 — fotos en toda la app, fase 2: Web base y Pacientes
**Hecho:** `UsuarioService.ObtenerFotosAsync` consulta GET /usuarios/fotos
(en tandas de 100 ids; si falla devuelve vacío y deja un Warning en el
log). La acción Usuarios/Foto ya no exige administrador: pide solo sesión
y decide la API. `_Avatar` carga la imagen con `loading="lazy"`.
Pacientes/Index y Pacientes/Detalle muestran la foto de quien la tiene,
con un solo pedido de consulta por pantalla. Compila. La API no se tocó.
**A medias:** sin probar a mano.
**Sigue:** fase 3 (Turnos) y fase 4 (Doctores).
**Ojo:** se trabajó en la rama `limpieza-login-doctor` por decisión de
Francis. SubirFoto y QuitarFoto siguen siendo solo de administrador.

## 2026-10-04 — fotos en toda la app, fases 3 y 4: Turnos y Doctores
**Hecho:** Turnos/Index consulta las fotos por `IdTurno` (el listado no
trae ids de persona); Turnos/Detalle y Cancelar, por el `IdPaciente` del
turno. Doctores/Index y Doctores/Detalle consultan por `IdDoctor` y
pasan a usar `_Avatar`, que suma el tono terracota y el tamaño 12 para
que las iniciales de los doctores se vean igual que antes. Un solo
pedido de consulta por pantalla. Compila, CSS recompilado. La API no se
tocó. Con esto queda completo el plan de fotos.
**A medias:** fases 2, 3 y 4 sin probar a mano.
**Sigue:** probar las tres fases; etapa C (chequeo de dueño en
PUT /usuarios/{id}) sigue como propuesta sin implementar.
**Ojo:** en Turnos el avatar es siempre el del paciente; la foto del
doctor no se muestra ahí. La home no tiene avatares.

## 2026-10-04 — etapa C: chequeo de dueño en PUT /usuarios/{id}
**Hecho:** PUT /usuarios/{id} ahora solo lo acepta del propio usuario o
de un administrador; al resto le contesta 403, antes de buscar al
usuario. Un solo archivo (`UsuarioEndpoints.cs`), sin migración. El
único llamador de la Web es Usuarios/Editar, que es de administrador:
no cambia nada ahí. Compila.
**A medias:** sin probar en Scalar.
**Sigue:** decidir qué hacer con el registro abierto.
**Ojo:** POST /usuarios/registro sigue siendo anónimo y acepta cualquier
rol: quien se registre como administrador por la API se saltea este
chequeo. GET /usuarios/{id} no se tocó: cualquier autenticado lee los
datos de cualquier usuario.

## 2026-10-04 — rediseño, F0: guía de diseño y prototipo
**Hecho:** `docs/diseno.md` (tokens, niveles de relieve, foco,
componentes, motivo de marca, textos y tabla de contraste) y el prototipo
estático `docs/prototipos/inicio-paciente.html` con su hoja propia
(`prototipo.css` -> `prototipo.build.css`, compilada con el CLI). Plus
Jakarta Sans bajada a `wwwroot/fonts` (latin y latin-ext, woff2 variable,
con su OFL). La app no cambia: nada de esto está referenciado todavía.
**Sigue:** F1 (tokens y clases en app.css).
**Ojo:** decididas sin consulta, por la opción más conservadora: (1) en
un aviso el enlace va en tinta subrayada, porque el teal sobre ese tinte
da 4,35:1; (2) el popover usa posición fija y no anclaje CSS, que no se
puede probar sin navegador; (3) los componentes nuevos no llevan
transiciones, quedan para F5.

## 2026-10-04 — rediseño, F1: tokens, fuente local y clases
**Hecho:** `app.css` con los valores nuevos (primary #0D717A, muted,
los tres estados que no pasaban), los tokens `control`, `accent-ink`,
`on-primary` y las sombras como tokens, `@font-face` local, clases
`tarjeta`, `boton`, `boton-primario`, `boton-secundario`, `campo` y
`chip`, y la regla global de foco. `.neu-elevado` y `.neu-hundido`
conservan el nombre. No se tocó ninguna vista. CSS recompilado, compila.
**Sigue:** F2 (layout, menú de cuenta y barra inferior).
**Ojo:** la regla de foco va FUERA de las capas y no en `@layer base`
como se había pedido: los 99 `focus:outline-none` de las vistas compilan
en la capa utilities y le ganaban. Si se prefiere en base, es mover el
bloque. En oscuro `on-primary` sigue blanco (4,23:1) hasta F5. Los campos
viejos siguen con borde `primary-line` (1,59:1) hasta el barrido. El
enlace a Google Fonts sigue en el layout hasta F2.

## 2026-10-04 — rediseño, F2: layout, menú de cuenta y barra inferior
**Hecho:** `_Layout` sin Google Fonts (la fuente ya es local), con
enlace "Saltar al contenido", destinos en el encabezado desde 768 px,
botón de cuenta con popover nativo (`_MenuCuenta`: nombre, rol, Ajustes,
Cerrar sesión) y barra inferior en celular (`_BarraInferior`: cuatro
destinos según el rol más "Más"). Los ítems salen de `MenuViewModel`,
con las mismas banderas de `AuthService`. "Administración" pasó a
"Cuentas". Íconos nuevos en `IconosLucide`. Compila, CSS recompilado.
La API no se tocó; jQuery y `site.js` siguen como estaban.
**A medias:** sin probar en navegador (lo prueba Francis).
**Sigue:** F3 (inicios por rol).
**Ojo:** decididas sin consulta: sin sesión ya no hay enlace "Inicio"
(lleva el logo); "Más" repite Ajustes y Cerrar sesión, que también están
en el menú de cuenta; los márgenes laterales siguen en `px-6`; Usuarios/*
sigue sin marcar "Cuentas" como activo, igual que antes. En un navegador
sin `popover` los dos menús quedan a la vista dentro de la página.

## 2026-10-04 — rediseño, F3: inicios por rol
**Hecho:** banner de bienvenida (`_BannerBienvenida` + `_MotivoMarca`)
con una frase por rol y dos botones; accesos rápidos con ícono en
círculo y el deshabilitado plano con "Próximamente"; estados vacíos con
botón (`_EstadoVacio`); paneles como `tarjeta` con el "ver todo" escrito
en vez de "…"; `_BadgeEstado` sobre la clase `chip`. Textos: títulos de
sección sin mayúsculas forzadas, "Historial de consultas", aviso del
doctor sin `POST /doctores`, "No pudimos cargar tu inicio". Compila, CSS
recompilado. Ningún pedido nuevo a la API; la API no se tocó.
**A medias:** sin probar en navegador.
**Sigue:** F4 (portada, login y registro).
**Ojo:** el próximo turno se ordena en la Web (día y hora) entre los seis
que ya se pedían, salteando cancelados y completados: con más de seis
turnos futuros puede no ser el real, porque GET /turnos no tiene orden
fijo. No se compara la hora con la actual (el servidor puede estar en
otro huso): un turno de hoy cuenta como próximo todo el día. Decididas
sin consulta: la lista del panel de paciente y doctor también sale
ordenada, para que coincida con el banner; el botón de los estados
vacíos es secundario (el primario está en el banner); el admin ve el
total de turnos de hoy. `_BadgeEstado` cambia también Turnos/Index,
Detalle y Cancelar. "Mis coberturas" sigue como "Próximamente".

## 2026-10-04 — rediseño, F4: portada, login y registro
**Hecho:** portada (Home/Index sin sesión) como tarjeta con el motivo de
marca y dos botones (Ingresar, Crear cuenta). Login y Registro en una
tarjeta de acceso: formulario con campos `campo` y botón
`boton-primario`, y el motivo arriba en celular o en una segunda columna
desde 768 px. Un solo verbo para entrar ("Ingresar") y uno para
registrarse ("Crear cuenta"), también en el encabezado. Compila, CSS
recompilado. La API no se tocó.
**A medias:** F0 a F4 sin probar en navegador: no se levantó la Web ni
la API. Hay que mirar los tres roles y sin sesión, claro y oscuro, texto
normal y extra grande, 360 px y teclado.
**Sigue:** F5 (transiciones y barrido del resto de las pantallas),
cuando Francis lo pida.
**Ojo:** no cambió ningún `asp-for`, ni el `ReturnUrl` oculto, ni los
mensajes de validación, ni `novalidate`, ni la sección `Scripts`.
Decididas sin consulta: la portada cambió de texto ("Tus turnos médicos,
en un mismo lugar"), el resumen de errores suma `role="alert"` y la
tarjeta de acceso pasa a dos columnas en pantallas anchas. Pendiente
para F5: 28 botones con `text-white`, 55 campos con borde `primary-line`,
las tarjetas planas de 30 vistas y el `primary` del tema oscuro.

## 2026-10-04 — rediseño, F5.1: contraste pendiente
**Hecho:** tema oscuro con `primary #2F9FA9` (5,87:1 como texto sobre
canvas, 5,42 sobre surface, 4,72 sobre primary-soft), `on-primary
#0B1F22` (5,39:1 en botones) y `primary-hover #4DB8C1` (7,25:1). Los 25
`text-white` pasaron a `text-on-primary`. Todos los campos de texto,
selects y textareas usan la clase `campo` (borde `control`, hundido):
las 9 constantes `claseCampo` y los filtros escritos en línea. Clase
nueva `boton-peligro`. Prototipo con los mismos valores. Compila, CSS
recompilado, chequeo de atributos de formulario sin diferencias.
**Sigue:** F5.2, barrido por grupos de vistas.
**Ojo:** corrección de un dato de F4: no eran "28 botones" ni "55
campos". Eran 25 botones, y de las 51 apariciones de `primary-line` solo
19 eran clases de campo (unos 90 campos por las constantes); el resto
son botones con borde teal y hovers, que se van en F5.2. Aclarar el teal
solo hasta 4,5:1 sobre canvas (#178E98) no alcanzaba: ahí ni el blanco
(3,92) ni la tinta (4,34) pasaban en el botón. El botón de cancelar turno
también usa `on-primary`: en oscuro el blanco sobre ese rojo daba 3,02.

## 2026-10-04 — rediseño, F5.2: barrido por grupos de vistas
**Hecho:** piezas compartidas: parcial `_Chip` con `ChipViewModel` y
`TonoChip` (`_BadgeEstado` ahora delega en él), `_EstadoVacio` con tono
(neutro, aviso, error) y nivel de título, clases `tarjeta-enlace` y
`boton-texto`, íconos `alerta`, `info` y `persona`. En cada grupo: las
tarjetas planas y las `neu-elevado` pasan a `tarjeta`, los botones a
`boton-*`, los "Volver" a `boton-texto`, los estados vacíos, de aviso y
de error a `_EstadoVacio`, y los títulos pierden las mayúsculas forzadas.
Un commit por grupo, con build y chequeo de atributos de formulario (sin
diferencias) antes de cada uno.
- a. Turnos: Index, Crear, Detalle, Cancelar. Las franjas horarias son
  `boton-secundario` y siguen siendo `submit` con su `name` y `value`;
  "Confirmar" y "Cancelar" de cada fila son botones de texto.
- b. Pacientes: Index, Detalle, Crear y MiPerfil/CompletarPaciente. El
  grupo sanguíneo pasó de `text-accent` (2,77:1 sobre el beige) a
  `text-accent-ink`.
- c. Doctores: Index y Detalle; Coberturas/Index. El listado vacío de
  doctores ofrece "Agregar doctor" al administrador (la vista ahora
  inyecta `AuthService`).
- d. Recetas: Index, Detalle, Crear; Historial: Index, Crear. Vigente,
  vencida y "Fuera del vademécum" son `_Chip`. Los vacíos ofrecen "Nueva
  receta" o "Nueva entrada" a quien puede cargarlas. "+" y "−" de
  Recetas/Crear son `boton-secundario` de 44 px; no cambiaron
  `formnovalidate`, acción, `asp-route-indice` ni `asp-fragment`.
- e. Cuentas: Admin/Index (tarjetas que son enlace: `tarjeta-enlace`),
  NuevoDoctor, NuevoAdministrador; Usuarios/Index (rol como `_Chip`,
  paginación con botones, el paso inactivo plano) y Usuarios/Editar
  ("Quitar foto" con borde y texto rojos a contraste completo).
- f. Ajustes (cada opción elevada en reposo y hundida y en negrita al
  elegirla), Home/Privacy, Shared/Error, NoEncontrado y SinPermiso (ícono
  en círculo; el candado pasó de `text-accent` a `accent-ink`). Las
  iniciales del avatar de doctor también pasaron a `accent-ink`. Se
  borraron `.neu-elevado` y `.neu-hundido`: ya no las usa ninguna vista.
**Sigue:** F5.2g (textos con jerga), después foco y movimiento.
**Ojo:** decididas sin consulta: las tres páginas sueltas (Error,
NoEncontrado, SinPermiso) no usan `_EstadoVacio` porque su título es el
`h1` de la página y traen texto con formato; los dos avisos chicos de
Turnos/Crear ("No hay horarios libres ese día") quedaron como nota
dentro del formulario; "Reintentar" de la búsqueda de usuarios ya no
manda filtros vacíos en la URL. Los botones miden ahora 44 px de alto
mínimo y los campos algo más: las filas de filtros quedaron más altas.

## 2026-10-04 — rediseño, F5.2g: textos sin jerga
**Hecho:** 22 textos visibles que nombraban "la API", rutas o verbos
HTTP pasaron a frases en voseo sin términos técnicos: 7 en vistas
(Cuentas, Nuevo doctor, Nuevo administrador, Recetas, Historia clínica,
detalle de turno) y 15 mensajes de controladores y servicios que llegan
a la pantalla (motivos de "sin permiso", errores de formulario y de
conexión). Además, dos títulos a sentence case en Pacientes/Crear. Solo
cambió el texto de los literales; compila y el chequeo de atributos no
muestra diferencias.
**Sigue:** F5.3 (foco) y F5.4 (movimiento).
**Ojo:** NO se tocó "No se pudo conectar con la API. ¿Está levantada en
{url}?" de `ApiClient`: en la limpieza del login se había decidido
dejarlo. Es el único texto visible que sigue nombrando la API. El mensaje
de log de `UsuarioService` tampoco, porque no se muestra. Los comentarios
del código siguen hablando de la API, a propósito.

## 2026-10-04 — rediseño, F5.3: foco sin anillos duplicados
**Hecho:** en las vistas ya no queda ningún `focus:outline-none`,
`focus:ring-*`, `focus-visible:ring-*`, `focus:border-primary` ni
`has-[:focus-visible]:ring-*` (eran 90, 56, 126, 19 y 2 al empezar F5;
casi todos se fueron al pasar botones y campos a las clases de
componente, y acá se barrieron los 24 que quedaban en enlaces de fila y
en el campo de archivo). El foco lo dibuja solo la regla global. Compila,
CSS recompilado, chequeo de atributos sin diferencias.
**Sigue:** F5.4 (movimiento).
**Ojo:** la regla global sigue fuera de las capas. Pacientes/Crear tiene
el input de la foto oculto detrás de su etiqueta: el contorno se dibuja
en el contenedor con `has-[:focus-visible]:outline-*` (antes no tenía
indicador de foco).

## 2026-10-04 — rediseño, F5.4: transiciones y movimiento
**Hecho:** todo en CSS y dentro de `prefers-reduced-motion:
no-preference`: fundido entre páginas con `@view-transition` (180 ms);
botones, tarjetas-enlace y campos con transición de sombra, fondo y
color (150 ms), hover que acorta la sombra (`shadow-elevado-xs`) y
`:active` hundido; entrada de los dos menús flotantes con
`@starting-style`; el latido del motivo de marca se dibuja una vez en el
inicio (banner y portada). El layout suma la sección opcional `Head`.
`docs/diseno.md` al día: valores oscuros, clases nuevas, sección
"Movimiento" y tabla de contraste. Compila; las reglas nuevas están en el
CSS generado.
**A medias:** nada de F5 está probado en navegador.
**Sigue:** F5.4b, anclaje CSS del menú de cuenta, en commit aparte.
**Ojo:** Recetas/Crear NO participa del fundido entre páginas (sección
`Head` con `navigation: none`): no se pudo ver si "+" y "−" parpadeaban,
y cada clic repinta la página anclada a otra fila, así que se eligió la
salida segura que se había previsto. Para reactivarlo, borrar esa
sección. El chequeo de atributos marca `required` en `_Layout`: es el
argumento `required: false` de la sección nueva, no un campo.

## 2026-10-04 — rediseño, F5.4b: anclaje CSS del menú de cuenta
**Hecho:** dentro de `@supports (anchor-name: --a) and (top:
anchor(bottom))`, el botón de cuenta (`boton-cuenta`) declara un ancla y
el menú se ubica con `top: anchor(bottom)` y `right: anchor(right)`. La
posición fija de antes queda igual para los navegadores sin soporte.
Compila; la regla está en el CSS generado.
**A medias:** sin probar en navegador. Va en commit aparte para poder
revertirlo solo (`git revert` de este commit) si el menú queda mal
ubicado.
**Sigue:** prueba manual de todo F5: tema oscuro, cada grupo de
pantallas, teclado, Recetas/Crear con "+" y "−", menú de cuenta, y el
sistema con "reducir movimiento" activado.
**Ojo:** con F5 queda cerrado el rediseño. Afuera, a propósito: el
mensaje de conexión de `ApiClient`, jQuery y la validación del cliente,
y la pantalla de "Mis coberturas".

## 2026-10-08 — mejoras, paso 1: pruebas automáticas
**Hecho:** proyecto `ChronoSalud.Tests` (xUnit) en la solución, con
repositorios falsos en memoria (`Falsos/`). Primeras pruebas: reservar y
cancelar turnos (`TurnoLogica`), cambio de horario semanal
(`HorarioLaboralLogica`), firma de las fotos, lista blanca de
preferencias y "hoy" en hora de Argentina. Los dos workflows corren
`dotnet test` antes de publicar.
**Sigue:** paso 2, alta de paciente completa.
**Ojo:** las pruebas no usan la base: lo que depende de SQL Server
(índices, transacciones) se prueba a mano.

## 2026-10-08 — mejoras, paso 2: alta de paciente completa
**Hecho:** el alta de paciente (Pacientes/Crear) manda toda la ficha en
el mismo pedido del registro, y la API guarda cuenta y paciente juntos.
`Paciente` suma tipo de documento, provincia, localidad, código postal y
contacto de emergencia (migración AgregarDatosAltaPaciente). PUT
/pacientes acepta `borrar` para vaciar datos; en Usuarios/Editar, vaciar
un campo lo borra. La ficha (Pacientes/Detalle) muestra documento,
dirección, teléfono y contacto de emergencia. Salen del alta los campos
de obra social (vuelven con las coberturas). Listas fijas en
`OpcionesPaciente`.
**Sigue:** paso 3, el paciente cancela sus turnos.
**Ojo:** el alta de paciente ahora viaja con el token del administrador
(antes iba anónima): la API solo acepta la ficha de un administrador.

## 2026-10-08 — mejoras, paso 3: el paciente cancela sus turnos
**Hecho:** `PuedeCancelarTurnos` suma al paciente (la API ya lo
permitía): en Turnos aparece "Cancelar" en sus turnos en pie, y también
en cada próximo turno del inicio. El detalle del turno tiene una tarjeta
"Acciones" con "Cancelar turno" para los tres roles; confirmar y
completar siguen siendo del personal. En Development, la API avisa al
arrancar si a la base local le falta una migración
(`Datos/AvisoDeMigraciones.cs`). El README dice el comando correcto para
levantar todo (`levantar.ps1`).
**Sigue:** paso 4, listados ordenados y paginados en la base.
**Ojo:** que el turno sea del paciente lo revisan `TurnosController`
(con el ámbito) y la API; la Web solo mira el rol para mostrar el botón.

## 2026-10-08 — mejoras, paso 3b: el aviso de migraciones dice qué base
**Hecho:** el aviso de migraciones pendientes nombra la base que revisó
y da el `dotnet ef database update` con `--connection` de esa misma
base. Si la base pide usuario y contraseña (Azure), no muestra la
cadena y remite al script en SSMS. El README explica de dónde sale la
cadena de conexión.
**Ojo:** el "Error 500" local después del paso 2 era la migración
aplicada en Azure (user-secrets) y no en la base de `levantar.ps1`.
Para la base local, `dotnet ef database update` va siempre con
`--connection`.

## 2026-10-08 — mejoras, paso 4: listados ordenados y paginados en la base
**Hecho:** GET /turnos filtra, ordena (`orden`, `dir`) y pagina en SQL,
acepta varios estados (`estados`) y devuelve `conteos` por estado. Lo
mismo para pacientes, doctores y notificaciones; los reportes cuentan en
la base. En la Web, Turnos pagina de a 20 con el orden de la API, arranca
en "de hoy en adelante" con "Ver todos, también los pasados", y se fue el
orden en memoria (`OrdenTurnos`). Paginador compartido
(`Shared/_Paginador`) en Turnos, Usuarios y Actividad. El inicio pide
solo pendientes y confirmados, ordenados, de a 6. La ficha del paciente
muestra el último turno atendido. El seeder recorre de a 100.
**Ojo:** sin migración. Durante el despliegue, si la Web llega antes que
la API, las tarjetas de Turnos muestran "—" unos minutos.

## 2026-10-08 — mejoras, paso 5: reglas de estado, doble reserva y hora de Argentina
**Hecho:** estado nuevo `ausente`. `EstadosTurno.PuedeCambiar` decide los
cambios permitidos: completado y ausente recién cuando empieza el turno,
y completado, ausente y cancelado son finales. PUT /turnos valida el
estado y, para reprogramar, que el turno esté en pie, el horario del
doctor y el choque. La hora sale de `IReloj` (hora de Argentina): el
paciente no reserva ni cancela un horario que ya pasó, y la
disponibilidad de hoy ya no depende del reloj de Azure. Índice único
filtrado contra la doble reserva (migración EvitarTurnosDuplicados), con
409. En la Web: "Marcar ausente", tarjeta de ausentes, completar y
ausente solo cuando empezó, y la hora de Argentina en los seis lugares
que usaban la del servidor. El seeder no fuerza "completado" en un turno
que todavía no empezó.
**Ojo:** antes de correr la migración en Azure, revisar que no haya
turnos duplicados (si hay, el índice no se crea).

## 2026-10-08 — mejoras, paso 6: pedir turno con días con lugar y confirmación
**Hecho:** GET /doctores/{id}/dias-disponibles (cuántas franjas libres
por día, comparte el cálculo con /disponibilidad). En la Web, "Nuevo
turno" va en tres pasos con enlaces: especialidad, doctor (o
"Cualquiera") y una tira de 14 días con cuántos horarios libres tiene
cada uno (partial `_TiraDeDias`, que va a usar Reprogramar). Tocar un
horario ya no reserva: lleva a `Turnos/Confirmar`, con el resumen, las
observaciones y, para el personal, el paciente. Al confirmar se abre el
detalle del turno nuevo. Clase `.dia` en el CSS, recompilado con
Tailwind 4.3.3. Probado contra una API de mentira: los tres pasos,
"Cualquiera", la confirmación del personal y la del paciente.
**Ojo:** sin migración. Si la Web llega antes que la API al desplegar,
la tira se muestra sin los números unos minutos.

## 2026-10-08 — mejoras, paso 7: reprogramar
**Hecho:** botón "Reprogramar" en el detalle del turno, para el personal
(cada doctor en su agenda y la administración en cualquiera). Lleva a
`Turnos/Reprogramar/{id}`: el turno como está y la misma tira de días del
paso 6, con el mismo doctor. Tocar un horario abre
`Turnos/ConfirmarReprogramacion`, con el "de … a …". La tira pasó a
recibir los doctores y una ruta base, así la usan las dos pantallas. La
API avisa al paciente cuando se le mueve el turno. Probado contra una API
de mentira, incluido el 409 de un horario que se ocupó.
**Ojo:** sin migración. El turno reprogramado conserva su estado.

## 2026-10-09 — mejoras, paso 8: Mi perfil, contraseña y límite de intentos
**Hecho:** "Mi perfil" en el menú de la cuenta y en el "Más" del celular,
para los tres roles. Muestra la cuenta y, según el rol, la ficha del
paciente o los datos profesionales del doctor (con enlaces a su horario y
a su actividad). En Editar cada uno corrige su nombre, apellido y
teléfono, y el paciente su ficha: el DNI ya cargado se ve pero no se
cambia (lo corrige la administración). "Cambiar contraseña" pide la
actual. En la API: POST /usuarios/me/contrasena, el PUT /usuarios/{id} ya
no cambia la contraseña, y un tope de intentos (429) para el login (10
cada 15 minutos por email) y para el cambio de contraseña (5 por
usuario). La vieja `MiPerfil/CompletarPaciente` redirige a Editar.
Probado contra una API de mentira como paciente, doctor, administrador y
paciente sin ficha.
**Ojo:** sin migración. Si se corre el seeder más de 10 veces seguidas,
el login del administrador de la demo queda frenado 15 minutos.

## 2026-10-09 — mejoras, paso 9: baja y reactivación de cuentas
**Hecho:** DELETE /usuarios/{id} frena con 409 la baja de la propia cuenta,
del último administrador y de una cuenta con turnos pendientes o
confirmados desde hoy (no cancela nada solo). La baja de un doctor también
apaga su perfil, así deja de aparecer en Doctores. POST
/usuarios/{id}/reactivar los vuelve a encender. GET /usuarios?bajas=true
lista las bajas y GET /pacientes ya no las muestra. En cada pedido con
token la API revisa que la cuenta siga activa: una cuenta dada de baja
sale al login en el próximo clic, sin esperar las 8 horas del token. En la
Web: pestañas "Activas" y "Dadas de baja" en Usuarios, y en Editar las
tarjetas "Dar de baja" y "Reactivar", cada una con su confirmación (la de
la baja lista los turnos que la frenan). Probado contra una API de
mentira.
**Ojo:** sin migración. No dar de baja cuentas de la demo: el seeder no
puede entrar con ellas (si pasa, reactivarlas).

## 2026-10-09 — mejoras, paso 10: recetas que no cambian solas, firmante e impresión
**Hecho:** cada renglón de la receta guarda una copia del nombre, el
genérico, la concentración y la forma del medicamento, así la receta no
cambia si después se edita el medicamento. DELETE /medicamentos/{id}
contesta 409 si el medicamento figura en una receta, y la base ya no borra
en cascada esos renglones. Las recetas y la historia clínica traen quién
las firmó o escribió (nombre, especialidad y matrícula). En la Web:
"Firmada por" en el detalle y en la lista de recetas, "Escrita por" en la
historia clínica, y la hoja nueva `Recetas/Imprimir/{id}`. La hoja se
imprime con Ctrl + P, sin JavaScript. El bloque `@media print` de
app.css la deja en blanco y negro aunque se haya elegido el tema oscuro.
El listado de recetas ya no baja el vademécum completo. CSS recompilado
con Tailwind 4.3.3. Probado contra SQL Server 2022 de verdad: se crearon
recetas con la API vieja, se corrió el script idempotente dos veces, y
después se probaron la API y la Web nuevas, con la hoja impresa a PDF.
**Ojo:** hay migración (CopiarMedicamentoEnReceta). Lleva a mano un
`UPDATE` dentro de `EXEC` que llena las recetas existentes. Sin el `EXEC`,
el script idempotente falla con "nombre de columna no válido". El script
se corre en Azure antes del merge.
