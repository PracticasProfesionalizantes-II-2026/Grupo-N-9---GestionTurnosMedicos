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
