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
