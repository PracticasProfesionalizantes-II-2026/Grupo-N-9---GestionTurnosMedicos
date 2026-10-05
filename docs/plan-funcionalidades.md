# Plan de funcionalidades — ChronoSalud

## Contexto

Siete fases, cada una en una sesión aparte que lee este archivo entero. Orden: **A0, A1, B, C, E, D, A2**. Este documento es solo el plan: no se editó nada. Al aprobarlo, lo único que se hace es guardarlo en `docs/plan-funcionalidades.md`, sin commit ni push.

Reglas de todas las fases:
- Permisos y dueño del recurso se validan en la API; la Web solo oculta. Datos de salud: el propio paciente y el personal autorizado.
- Sin JavaScript nuevo ni PowerShell. Textos en voseo, sin jerga. Componentes de `docs/diseno.md`.
- `dotnet ef` lo corre Francis; la API no migra sola. No se levanta la API, la Web ni el seeder. Sin commits, push ni `Co-Authored-By`.
- Antes de tocar la API se lista cada archivo (agregado o modificación) y al terminar se muestra el diff.
- ChronoSaludWeb es el único cliente de la API. Los compañeros pueden usarla directo por Scalar o Postman: cada fase que cambia la API deja su nota en `CAMBIOS-API.md`.
- Despliegue: los dos workflows (`.github/workflows/main_chronosalud.yml`, `main_chronosaludfront.yml`) se disparan con cualquier push a `main`, sin filtro de rutas: API y Web salen juntas. Donde importa el orden hay que hacer dos pushes.

## Decisiones tomadas

1. **Registro abierto:** se cierra, en una fase nueva A0. Un rol distinto de paciente exige token de administrador.
2. **Campos que corrige el paciente (A1):** nombre, apellido, teléfono, dirección, nacionalidad, estado civil, fecha de nacimiento, sexo, grupo sanguíneo, alergias y condiciones. Email no. DNI solo si está vacío; después lo cambia administración. La foto propia va como paso opcional de A1.
3. **Horarios (B):** un rango por día. Sin horario cortado, sin migración.
4. **Baja (C):** incluye reactivar: filtro "Dadas de baja" en Usuarios y botón "Reactivar" con confirmación; si es doctor, también vuelve `Doctores.Activo`.
5. **Correo (A2):** sin proveedor por ahora. Se diseña con el enlace en el log y una interfaz de envío intercambiable; el proveedor se elige al empezar A2.
6. **Turnos sin chequeo de dueño:** pasan de "fuera de alcance" a la fase E, después de C.
7. **`Jwt:Key`:** la rotación la hace Francis en la configuración de Azure. No se toca código.
8. **htmx:** queda descrito en D como alternativa; se decide después.
9. **Clientes de la API:** no hay otro front. El aviso al grupo es la nota en `CAMBIOS-API.md` de cada fase.
10. **Zona horaria:** se resuelve con el ajuste `WEBSITE_TIME_ZONE` del App Service, no con código. No se crea ninguna clase de hora ni se tocan los "hoy" (ver B).
11. **Turnos (E):** el paciente cancela sus propios turnos con la pantalla de confirmación; el doctor reserva para cualquier paciente, en cualquier agenda, como hoy.

## Hallazgos que cruzan varias fases

- **Quién puede llamar hoy a las cuentas.** `PUT /usuarios/{id}`: solo el propio usuario o un administrador (`ChronoSaludApi/Endpoints/UsuarioEndpoints.cs:63-68`); nadie modifica cuentas ajenas. `GET /usuarios/{id}`: **cualquier usuario autenticado** (`UsuarioEndpoints.cs:49-57`) lee nombre, apellido, email, teléfono y rol de cualquier cuenta, incluso dadas de baja, y los ids son correlativos. **Urgente: va en A0.**
- **Queda en el PUT:** cambia la contraseña sin pedir la actual (`Logica/UsuarioLogica.cs:85-86`). Se cierra en A1.
- **"secretario" no existe como rol.** El registro acepta paciente, doctor y administrador (`UsuarioLogica.cs:30`); el nombre aparece solo en chequeos (`HorarioLaboralEndpoints.cs:42`, `TurnoEndpoints.cs:51,102`, `PacienteEndpoints.cs:49,67`).
- **El token dura 8 h y la API no vuelve a mirar `Activo`** (`Program.cs:21-36`, `UsuarioLogica.cs:145`). Se cierra en C.
- **Los PUT ignoran los campos vacíos** (`PacienteLogica.cs:92-111`): hoy no se puede borrar un dato.
- **`Jwt:Key`.** `Program.cs:31-34` lee `Jwt:Issuer`, `Jwt:Audience` y `Jwt:Key` de la configuración al arrancar, y `UsuarioLogica.cs:131,142-143` los vuelve a leer al firmar. `Program.cs:11` usa el armado por defecto, donde las variables de entorno van después de `appsettings.json`: **una variable `Jwt__Key` en el App Service pisa la del repo** (`appsettings.json:10`). Al rotarla: mínimo 32 caracteres (si no, falla al firmar), el App Service reinicia y todos vuelven a ingresar. La Web no usa la clave.
- **Migraciones.** La última del repo es `20261004164901_AgregarUsuarioFoto`. Antes de generar cualquier script hay que mirar en Azure `SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;` y usar la última aplicada como origen. Solo A2 tiene migración.

---

## A0 — Cerrar el registro y la lectura de cuentas ajenas

**Objetivo.** Que nadie pueda crearse una cuenta de doctor o administrador por la API, ni leer cuentas ajenas.

**Estado actual.**
- `POST /usuarios/registro` es anónimo y acepta cualquier rol (`UsuarioEndpoints.cs:14-31`, `UsuarioLogica.cs:24-53`).
- **La Web no manda el token al dar de alta doctores ni administradores.** `AdminController.cs:64-65` y `133-134` llaman a `AuthService`, que registra con `anonimo: true` (`ChronoSaludWeb/Services/AuthService.cs:156-159`), y así `ApiClient` no adjunta el token (`ApiClient.cs:166-171`). Hay que cambiarlo.
- El seeder registra al administrador y a los doctores de la demo sin sesión (`tools/Seed/Program.cs:93,246,685-695`). Con `--email` ya entra con una cuenta existente, pero solo junto con `--solo-medicamentos` (`70-74`).

**Cómo nace el primer administrador.** Si la base no tiene ningún administrador activo, el registro de un administrador se acepta sin token. Los entornos que ya tienen administradores no cambian; una base nueva se inicia con el seeder o con un registro por Scalar.

**API.**
- `Endpoints/UsuarioEndpoints.cs:14-31` — modificación: rol distinto de paciente solo con token de administrador (401 sin token, 403 con otro rol), salvo el caso del primer administrador. El endpoint sigue anónimo; el token, si viene, ya lo valida el pipeline (`Program.cs:22,89`).
- `Endpoints/UsuarioEndpoints.cs:49-57` — modificación: `GET /usuarios/{id}` solo para el propio usuario o un administrador (403 al resto).
- `Logica/UsuarioLogica.cs:24-53` e `IUsuarioLogica.cs:7` — modificación: `Registrar` recibe si quien llama es administrador. `Repositorios/UsuarioRepository.cs` e interfaz — agregado: saber si hay un administrador activo.

**Web.**
- `Services/AuthService.cs:153-165` — modificación: con rol distinto de paciente el registro va con el token de la sesión. El registro público y `Pacientes/Crear` siguen igual.
- `Controllers/AdminController.cs:67-71,136-140` — modificación: mensaje propio para el 403, que llega sin texto.

**Seed (`tools/Seed/Program.cs`).**
- `70-74` — modificación: `--email` vale también para la carga completa (entra con un administrador existente y usa su token).
- `88-99` — modificación: sin `--email`, primero intenta ingresar como el administrador de la demo; si no existe, lo registra sin sesión (solo funciona en una base sin administradores); si la API lo rechaza, corta con "Ya hay administradores: corré el seeder con --email".
- `685-695` — modificación: el alta de doctores viaja con el token del administrador (`ApiCliente` ya acepta token, `ApiCliente.cs:51`). `README.md:94-105`: actualizar el uso.

**Otros clientes que pueden depender del registro abierto.** Además de la Web y el seeder, solo los compañeros que crean cuentas de doctor o administrador directo por Scalar o Postman: desde A0 necesitan el token de un administrador. La solución no tiene proyecto de pruebas. Va en la nota de `CAMBIOS-API.md`, junto con el cambio de `GET /usuarios/{id}`.

**Esquema y despliegue.** Sin migración. Orden: **primero la Web** (mandar el token a la API vieja no rompe nada), después la API.

**Riesgos.** En una base nueva expuesta a internet, el primero que se registra queda de administrador. La respuesta del registro incluye el token de la cuenta creada (`UsuarioLogica.cs:50-52`), también cuando la crea un administrador; la Web lo descarta y no se cambia.

**Cómo probarlo.** Scalar: registrar un administrador sin token (401), con token de paciente (403) y con token de administrador (201); registrar un paciente sin token (201); `GET /usuarios/{otro}` con token de paciente (403). Web: Nuevo doctor, Nuevo administrador, registro público y alta de paciente. Seeder contra una base local vacía y contra una que ya tiene datos, nunca contra Azure.

**Esfuerzo.** Bajo a medio.

---

## A1 — Perfil y cambio de contraseña

**Objetivo.** "Mi perfil" al hacer clic en el nombre o el avatar: el paciente ve todos sus datos y corrige los de la decisión 2; cualquier rol cambia su contraseña ingresando la actual.

**Estado actual.**
- El menú muestra nombre y rol sin enlace (`Views/Shared/_MenuCuenta.cshtml:19-22`); el "Más" del celular tampoco (`_BarraInferior.cshtml:66-83`).
- `MiPerfilController` solo tiene `CompletarPaciente`, con cinco campos clínicos (`MiPerfilController.cs:25-94`). No muestra cuenta, DNI, dirección, nacionalidad ni estado civil.
- La API ya sirve para leer y guardar: `GET /pacientes/me` (`PacienteEndpoints.cs:29-40`) y `PUT /pacientes/{id}` con chequeo de dueño (`PacienteLogica.cs:80-87`).
- Falta: cambio de contraseña, borrar un dato, y que el paciente no pueda cambiarse un DNI ya cargado (hoy puede, `PacienteLogica.cs:98-107`).

**API.**
- `Endpoints/UsuarioEndpoints.cs` — agregado: `POST /usuarios/me/contrasena` (usuario del token; pide actual y nueva; 400 si la actual no coincide, si la nueva tiene menos de 8 o si es igual).
- `Logica/UsuarioLogica.cs` — agregado `CambiarContrasena`; modificación `85-86`: el PUT deja de aceptar `Contrasena`. `IUsuarioLogica.cs` y `DTOs/UsuarioDTOs.cs`: agregados.
- `DTOs/PacienteDTOs.cs:32-43` — agregado al final: lista opcional de campos a borrar (quien no la manda no cambia nada).
- `Logica/PacienteLogica.cs:92-111` — modificación: aplica el borrado (alergias, condiciones, dirección, nacionalidad, estado civil) y, si quien llama no es personal, no cambia un DNI ya cargado.

**Web.**
- `Controllers/MiPerfilController.cs` — agregados: `Index`, `Editar`, `GuardarCuenta`, `GuardarFicha`, `Contrasena` (GET y POST). Tras guardar el nombre se refresca la sesión, como en `UsuariosController.cs:144-147`.
- `Services/UsuarioService.cs` — agregado `CambiarContrasenaAsync`. `Services/PacienteService.cs:136-160` — parámetro de borrado.
- `Models/PerfilViewModels.cs` — agregados; se reutilizan `CuentaEditarViewModel` y `PacienteEditarViewModel` (`UsuariosViewModels.cs:144,166`).
- Vistas nuevas: `MiPerfil/Index.cshtml` (tarjetas "Tu cuenta" y "Tu ficha"; para el doctor, "Datos profesionales" en solo lectura), `Editar.cshtml`, `Contrasena.cshtml`.
- Modificadas: `_MenuCuenta.cshtml:19-33`, `_BarraInferior.cshtml:66`, `Models/MenuViewModel.cs:46,66`. Seed: sin cambios.

**Paso opcional: foto propia.** Es un cambio chico y seguro, se incluye como paso aparte al final de la fase.
- Hoy subir y quitar son solo de administrador (`UsuarioFotoEndpoints.cs:18-41,75-84`). La validación ya está en la API: 2 MB (`UsuarioFotoLogica.cs:26`), tipo por los primeros bytes del archivo, solo JPG, PNG o WebP (`UsuarioFotoLogica.cs:108-127`), y tope de 5 MB por pedido (`UsuarioFotoEndpoints.cs:11,39`).
- API: `UsuarioFotoEndpoints.cs:18-41` y `75-84` — modificación: el propio usuario o un administrador; 403 al resto.
- Web: `MiPerfilController` — agregados `SubirFoto` y `QuitarFoto`, con el id de la sesión y nunca de la ruta; reutilizan `ValidadorDeImagen` y `UsuarioService.cs:127-135`, con los topes de `UsuariosController.cs:315-318`. La tarjeta se copia de `Usuarios/Editar.cshtml:142-172`.
- Quién ve la foto de un paciente no cambia: administrador, doctores y él mismo (`UsuarioFotoLogica.cs:98-104`).

**Esquema y despliegue.** Sin migración. API y Web pueden salir en el mismo push.

**Permisos.** Contraseña y foto: siempre el usuario del token. Ficha: el paciente solo la propia; doctor y administrador como hoy (`PacienteEndpoints.cs:49`).

**Riesgos.** Quien cambiaba contraseñas con el PUT por Scalar o Postman deja de poder: va en la nota de `CAMBIOS-API.md`. Las sesiones ya abiertas siguen valiendo hasta 8 h después del cambio (se cierra en A2).

**Cómo probarlo.** Paciente: menú → Mi perfil; ver todos los datos; borrar alergias y confirmar que quedó vacío; intentar cambiar un DNI cargado. Contraseña: actual mal, nueva corta, caso correcto, salir y entrar con la nueva. Scalar: el PUT con contraseña ya no la cambia. Foto: un PDF renombrado a .jpg y un archivo de 3 MB se rechazan; `PUT /usuarios/{otro}/foto` con token de paciente da 403.

**Esfuerzo.** Medio.

---

## B — Horarios de atención

**Confirmación.** Son horarios laborales semanales: `HorarioLaboral` es un rango por día de la semana por doctor (`Entidades/HorarioLaboral.cs:3-18`; índice único doctor + día, `AppDbContext.cs:168-169`). Los turnos individuales son otra tabla.

**Qué ya existe en la ficha del doctor.** `Doctores/Detalle` muestra el horario semanal en solo lectura (`Views/Doctores/Detalle.cshtml:59-82`, parcial `_HorarioSemanal.cshtml`) y el botón "Pedir turno con este doctor" (`62-69`). No hay pantalla de edición.

**Estado actual de la API.** `GET /doctores/{id}/horarios`: cualquier autenticado. `PUT /doctores/{id}/horarios`: reemplaza la semana entera (lista vacía = deja de atender) y solo lo acepta de administrador: **el doctor no puede editar el suyo** (`HorarioLaboralEndpoints.cs:31-42`). No mira los turnos reservados (`HorarioLaboralLogica.cs:41-75`). La Web no tiene método para el PUT (`DoctorService.cs:92-103`).

**Horarios con turnos reservados.** Se impide. El PUT contesta 409 si algún turno futuro pendiente o confirmado que hoy entra en el horario dejaría de entrar. Mensaje: "No podés cambiar el martes: hay 3 turnos reservados (el más próximo, el 14/10 a las 9:30). Cancelalos o cambiales la fecha primero." No se cancela nada solo.

**Zona horaria.** WEBSITE_TIME_ZONE ya está definida en la API y en la Web con hora de Argentina (verificado, sistema operativo: Windows). No hace falta tocar nada de hora en ninguna fase.

**API.**
- `Endpoints/HorarioLaboralEndpoints.cs:31-42` — modificación: suma el rol doctor, lee el usuario del token, 403 si el doctor no es el dueño, 409 para el choque con turnos.
- `Logica/HorarioLaboralLogica.cs:41-75` — modificación: chequeo de dueño y de turnos, con `ITurnoRepository.ObtenerTodos` (`TurnoRepository.cs:13-27`), ya inyectado. `IHorarioLaboralLogica.cs:8`: cambia la firma.

**Web.**
- `Controllers/DoctoresController.cs` — agregados `Horarios` GET y POST. `Services/DoctorService.cs` — agregado `GuardarHorariosAsync`. `Models/HorarioViewModels.cs` — agregado el modelo de edición.
- Vista nueva `Doctores/Horarios.cshtml`: un formulario con siete filas (casilla "Atiende", desde, hasta); quitar un día es destildarlo. Las horas del día destildado se atenúan con `:has()`.
- `Views/Doctores/Detalle.cshtml:60-69` — botón "Editar horario" para el administrador y para el propio doctor. En "Mi perfil" (A1), enlace "Mi horario de atención".
- Seed: sin cambios (carga con token de administrador, `tools/Seed/Program.cs:361`).

**Esquema y despliegue.** Sin migración. Primero la API: con la API vieja, el doctor recibe 403 al guardar.

**Permisos.** Doctor: solo su horario, resuelto del token. Administrador: cualquiera.

**Riesgos.** Hay turnos que ya están fuera de horario (el PUT de turnos no valida el horario, `TurnoLogica.cs:186-209`): por eso solo frenan los que hoy entran. Qué turno es "futuro" depende del ajuste de zona horaria del App Service: si se borra, el servidor vuelve a UTC.

**Cómo probarlo.** Doctor: editar un día sin turnos; achicar un día con un turno confirmado y ver el mensaje; `PUT` al horario de otro doctor en Scalar (403). Administrador: editar a cualquiera.

**Esfuerzo.** Medio.

---

## C — Baja y reactivación de usuarios

**Objetivo.** El administrador da de baja y reactiva pacientes, doctores y administradores, con baja lógica y confirmación en pantalla aparte.

**Estado actual.**
- `DELETE /usuarios/{id}` existe, solo administrador, y pone `Usuarios.Activo` en falso (`UsuarioEndpoints.cs:81-93`, `UsuarioLogica.cs:92-100`). No impide darse de baja a uno mismo ni al último administrador, y no toca el perfil de doctor ni los turnos. No hay forma de reactivar.
- **Login con un usuario inactivo:** se rechaza con el mismo 401 que una contraseña mal (`UsuarioLogica.cs:57-59`) y la Web muestra "Email o contraseña incorrectos." (`AuthService.cs:182-187`). Bien: no revela que la cuenta existe. Pero quien ya estaba adentro sigue hasta 8 h.
- Un inactivo sigue apareciendo: `GET /pacientes` no filtra por usuario activo (`PacienteRepository.cs:15`) y `GET /doctores` filtra por `Doctores.Activo`, no por el usuario (`DoctorRepository.cs:15`): un doctor dado de baja seguiría recibiendo turnos.
- `GET /usuarios` devuelve solo activos (`UsuarioRepository.cs:49`). La Web no tiene pantalla ni servicio de baja.

**API.**
- `Endpoints/UsuarioEndpoints.cs:81-93` — modificación: lee el usuario del token y contesta 409 con el motivo.
- `Logica/UsuarioLogica.cs:92-100` y constructor `13-22` — modificación: no a uno mismo; no al último administrador activo; no si tiene turnos futuros pendientes o confirmados; si es doctor, también `Doctores.Activo` en falso, en la misma operación.
- `Endpoints/UsuarioEndpoints.cs` — agregado: `POST /usuarios/{id}/reactivar` (solo administrador): vuelve `Usuarios.Activo` y, si tiene perfil de doctor, `Doctores.Activo`.
- `Endpoints/UsuarioEndpoints.cs:95-113`, `UsuarioLogica.cs:102-127`, `UsuarioRepository.cs:42-49` — modificación: parámetro opcional para listar las cuentas dadas de baja. `DTOs/UsuarioDTOs.cs:3-10` y `UsuarioLogica.cs:74` — agregado `Activo` al final del DTO.
- `Repositorios/UsuarioRepository.cs` e interfaz — agregados: contar administradores activos; baja y reactivación en una transacción.
- `Program.cs:23-36` — modificación: al validar el token rechaza al usuario inactivo (una consulta por clave en cada pedido). La Web ya cierra la sesión ante un 401 (`ApiClient.cs:191-196`).
- `Repositorios/PacienteRepository.cs:15` — modificación: el listado trae solo pacientes con usuario activo. La ficha por id sigue accesible, para no perder la historia clínica.

**Turnos futuros de un doctor dado de baja (opción conservadora).** No se cancela nada automáticamente. La baja se impide mientras tenga turnos futuros pendientes o confirmados; la pantalla de confirmación los lista con enlace a cada uno. El administrador los cancela uno por uno (cada cancelación ya avisa al paciente, `TurnoLogica.cs:225-237`). Misma regla para un paciente. Al reactivar a un doctor, su horario semanal sigue como estaba.

**Web.**
- `Services/UsuarioService.cs` — agregados `DarDeBajaAsync` y `ReactivarAsync`; modificación `109-121` y `222-233` (buscar también entre las bajas).
- `Controllers/UsuariosController.cs` — agregados `Baja` y `Reactivar`, GET y POST, con el patrón de `Turnos/Cancelar` (`TurnosController.cs:124-194`); modificación `48-98` (filtro de estado).
- Vistas nuevas `Usuarios/Baja.cshtml` (`boton-peligro`, qué va a pasar, turnos que la frenan) y `Usuarios/Reactivar.cshtml`.
- `Views/Usuarios/Index.cshtml:41-69,100-113` — filtro "Activas / Dadas de baja" como enlaces y chip "Dada de baja" en la fila. `Usuarios/Editar.cshtml` (al final, `331`) — tarjeta "Dar de baja", que no se muestra en la propia cuenta; en una cuenta dada de baja, aviso y botón "Reactivar". `Models/UsuariosViewModels.cs` — agregados.
- Seed: sin cambios.

**Esquema y despliegue.** Sin migración: `Usuarios.Activo` y `Doctores.Activo` ya existen. Primero la API.

**Permisos.** Rol y reglas en la API. Depende de A0: sin el registro cerrado, cualquiera se hace administrador.

**Riesgos.** Dos administradores dándose de baja a la vez: por eso la transacción. Un email dado de baja no se puede volver a registrar (índice único, `AppDbContext.cs:28-30`): se reactiva. Si se da de baja una cuenta de la demo, el seeder falla con un mensaje confuso (`tools/Seed/Program.cs:721-725`).

**Cómo probarlo.** Baja de un paciente sin turnos: sale de Pacientes y no puede ingresar; con su sesión abierta en otro navegador, el próximo clic lo manda al login. Darse de baja a uno mismo y al único administrador por Scalar: 409. Doctor con un turno confirmado: se impide; se cancela el turno; se da de baja; ya no aparece en Doctores ni al pedir turno. Filtro "Dadas de baja" → Reactivar: vuelve a ingresar y, si es doctor, vuelve a Doctores con su horario.

**Esfuerzo.** Medio.

---

## E — Dueño del turno en la API

**Objetivo.** Un paciente solo reserva y cancela a su nombre; el personal, según su rol.

**Estado actual.**
- `POST /turnos`: cualquier autenticado crea un turno para cualquier paciente; el id sale del cuerpo tal cual (`TurnoEndpoints.cs:61-81`, `TurnoLogica.cs:135-167`).
- `DELETE /turnos/{id}`: cualquier autenticado cancela cualquier turno (`TurnoEndpoints.cs:105-115`, `TurnoLogica.cs:225-237`).
- Hoy lo frena solo la Web: fuerza el paciente desde el perfil (`TurnosController.cs:391-406`), revalida el alcance antes de cancelar (`177-183`) y reserva la cancelación al personal (`AuthService.cs:80-81`).
- `GET` y `PUT` de turnos ya chequean dueño (`TurnoLogica.cs:99-121,177-184`).

**Reglas.**
| Rol | Reservar | Cancelar |
|---|---|---|
| Paciente | Solo a su nombre: el paciente sale del token y se ignora el del cuerpo | Solo sus turnos |
| Doctor | Para cualquier paciente, en cualquier agenda (como hoy) | Solo los de su agenda |
| Administrador | Cualquiera | Cualquiera |

Un turno ajeno se contesta como inexistente (404), igual que en `TurnoLogica.cs:106-107,182-183`.

**API.**
- `Endpoints/TurnoEndpoints.cs:61-81` y `105-115` — modificación: leen usuario y rol del token y los pasan a la lógica.
- `Logica/TurnoLogica.cs:135-167` y `225-237` — modificación: aplican la tabla con los repositorios ya inyectados. `ITurnoLogica.cs:12,15`: cambian las firmas.

**Web.** El paciente pasa a cancelar sus propios turnos con la pantalla de confirmación que ya existe (`Turnos/Cancelar`, `TurnosController.cs:124-194`, que ya revalida que el turno sea suyo). Modificaciones: `Services/AuthService.cs:80-81` (suma al paciente), los textos de `TurnosController.cs:130-133,168-171` y la columna de acciones de `Views/Turnos/Index.cshtml:181-186,228-256`. Seed: sin cambios (crea los turnos con token de administrador, `tools/Seed/Program.cs:464-473`).

**Esquema y despliegue.** Sin migración. Compatible con la Web actual: se puede desplegar en cualquier orden.

**Riesgos.** Quien creaba o cancelaba turnos ajenos por Scalar o Postman con token de paciente o de doctor deja de poder: va en la nota de `CAMBIOS-API.md`. La cancelación no mira el estado (`TurnoLogica.cs:225-237`): con el paciente cancelando, conviene limitarla en esta fase a turnos pendientes o confirmados. No hay plazo mínimo para cancelar.

**Cómo probarlo.** Scalar, token de paciente: `POST /turnos` con el id de otro paciente crea el turno a nombre propio; `DELETE` de un turno ajeno da 404 y del propio 204. Token de doctor: cancelar un turno de otra agenda da 404. Administrador: todo. Web: como paciente, cancelar un turno propio desde el listado pasando por la confirmación; pedir y cancelar con cada rol.

**Esfuerzo.** Bajo.

---

## D — Menos pasos al pedir turno y al filtrar (sin JavaScript)

**Estado actual.** `Turnos/Crear`: elegir especialidad → "Continuar" → elegir doctor y fecha → "Ver horarios" → tocar un horario (`Views/Turnos/Crear.cshtml:44-84,170-190`; `TurnosController.cs:329-373`). Cada cambio de fecha es otra vuelta con botón. Filtros con botón en `Turnos/Index.cshtml:42-84`, `Doctores/Index.cshtml:22-45`, `Usuarios/Index.cshtml:41-69`, `Pacientes/Index.cshtml:39-60`, `Historial/Index.cshtml:43`, `Recetas/Index.cshtml:47` y `Coberturas/Index.cshtml:12`.

**Propuesta con enlaces y formularios GET.**
1. **Pedir turno en tres clics.** Especialidades como chips que son enlaces → doctores como `tarjeta-enlace` (más "Cualquiera") → tira de 14 días como enlaces, con el primer día en que atiende ya elegido y sus horarios a la vista → tocar un horario reserva, igual que hoy. Los días en que no atiende van planos, sin enlace. Desde la ficha del doctor se entra con los dos primeros pasos resueltos (`Detalle.cshtml:64-68`). Paciente (para administrador y doctor) y observaciones siguen en el formulario de reserva.
2. **Doctores.** Chips de especialidad; cada fila suma el enlace "Pedir turno".
3. **Turnos.** Las cuatro tarjetas de conteo pasan a ser el filtro de estado (más "Todos"); chips de período: Hoy, Esta semana, Próximos 30 días, Todos. El rango a mano queda en un `<details>` con su botón: un campo de fecha no se envía solo sin JavaScript.
4. **Usuarios.** El rol como chips, junto al filtro de C; el texto sigue con Enter o botón.
5. Pacientes, Recetas, Historial y Coberturas quedan como están: texto libre o una lista larga de pacientes.

**Archivos (solo Web).** `TurnosController.cs:37-98` (conteos sobre el listado sin filtrar por estado) y `329-373, 467-596` (días y primer día con atención); `Models/TurnoCrearViewModel.cs`; `Views/Turnos/Crear.cshtml:44-96` (se reescribe); `Turnos/Index.cshtml:42-151`; `Doctores/Index.cshtml:22-45,76-95`; `Usuarios/Index.cshtml:49-54`; parcial nuevo `Shared/_ChipsFiltro.cshtml` con su modelo; `wwwroot/css/app.css` (chip seleccionado) y recompilar con la CLI de Tailwind; `docs/diseno.md` (fila "Filtro como enlaces"). API y Seed: sin cambios. Sin migración.

**Riesgos.** La tira marca los días por el horario semanal, no por cupos reales: un día puede aparecer y no tener horarios libres. Con "Cualquiera" se consulta doctor por doctor (`TurnosController.cs:565-584`), así que esa espera sigue. Lo seleccionado lleva `aria-current`, relieve hundido y negrita (`diseno.md:74`).

**Alternativa htmx (no se implementa).**
- *Qué es:* un archivo de unos 16 KB comprimido, servido desde `wwwroot/lib` (sin CDN), que se usa con atributos en el HTML. Sin JavaScript propio.
- *Pantallas que cambiarían:* `Turnos/Crear` (cambiar especialidad, doctor o fecha actualiza solo el bloque de horarios, sin botón), `Turnos/Index`, `Doctores/Index`, `Usuarios/Index` y `Pacientes/Index` (buscar mientras se escribe), y los selectores de paciente de Recetas e Historial.
- *Costo:* bajo a medio. Una línea en `_Layout.cshtml:141` y atributos en seis o siete vistas; los controladores no cambian si se toma el fragmento de la página completa.
- *Riesgo:* contradice el principio 3 de la guía y hay que enmendarla. Tras cada reemplazo hay que cuidar el foco y el anuncio a lectores de pantalla. Una sesión vencida a mitad de un reemplazo necesita manejo propio en `ApiExceptionFilter.cs:25-32`. El fundido entre páginas y `fila-animada` no se comportan igual. Hay que mantener la versión con enlaces como respaldo, así que D se hace primero de todas formas.

**Cómo probarlo.** Contar clics de inicio a turno reservado como paciente y como administrador desde la ficha de un paciente; solo teclado; 360 px; tema oscuro; cada chip conserva los demás filtros en la URL.

**Esfuerzo.** Medio (bajo si se dejan afuera los cambios de `Turnos/Crear`).

---

## A2 — Restablecimiento por correo

**Objetivo.** "Olvidé mi contraseña": pedir un enlace y elegir una contraseña nueva. Se construye sin proveedor: el envío queda detrás de una interfaz.

**Estado actual.** No existe nada: ni tabla, ni endpoints, ni envío de correo, ni pantallas. El login no tiene el enlace (`Views/Cuenta/Login.cshtml:39-49`).

**API.**
- `Entidades/RestablecimientoContrasena.cs` — nuevo: usuario, hash del token, creado, vence, usado. `Entidades/Usuario.cs` — agregado: fecha del último cambio de contraseña.
- `Datos/AppDbContext.cs` — agregados: `DbSet` y configuración (índice único del hash, borrado en cascada con el usuario).
- `Endpoints/UsuarioEndpoints.cs` — agregados, anónimos: pedir el enlace (contesta siempre lo mismo, exista o no el email) y restablecer (token más contraseña nueva; un único mensaje: "El enlace no es válido o ya venció.").
- Lógica y repositorio nuevos. Token de 32 bytes aleatorios; se guarda solo su SHA-256; vence a los 30 minutos; se marca usado en la misma operación que cambia la contraseña; pedir uno nuevo anula los anteriores.
- `Servicios/IEnviadorDeCorreo.cs` — nuevo, con una sola implementación por ahora: escribe el enlace en el log **solo en desarrollo**. En producción sin proveedor no envía ni escribe el token; deja un aviso sin el enlace. El envío va fuera del pedido, para que la respuesta tarde lo mismo exista o no la cuenta.
- `Program.cs` — modificación: registro de servicios; límite de pedidos por IP en los dos endpoints (viene con ASP.NET, sin paquete nuevo); el chequeo de token de C pasa a rechazar también los tokens anteriores al último cambio de contraseña.

**Web.** `Controllers/CuentaController.cs` — agregados `Olvide` y `Restablecer` (GET y POST). `Services/AuthService.cs` — agregados los dos pedidos anónimos. Vistas nuevas `Cuenta/Olvide.cshtml` y `Cuenta/Restablecer.cshtml`, con la tarjeta de acceso del login. `Login.cshtml:44-49` — enlace "¿Olvidaste tu contraseña?". Seed: sin cambios.

**Esquema y despliegue.** Hay migración: tabla nueva y una columna que admite nulos en `Usuarios`.
1. `dotnet ef migrations add AgregarRestablecimientoContrasena --project ChronoSaludApi`
2. Mirar `__EFMigrationsHistory` en Azure y tomar la última aplicada como origen (si el repo no cambió, `20261004164901_AgregarUsuarioFoto`).
3. `dotnet ef migrations script <origen> AgregarRestablecimientoContrasena --idempotent --project ChronoSaludApi --output AgregarRestablecimientoContrasena.sql` (en la raíz, como `AgregarUsuarioFoto.sql`).
4. Orden: correr el script en Azure → cargar `Web__UrlPublica` en el App Service de la API → push de la API → push de la Web. Cuando haya proveedor, su clave va solo como ajuste del App Service (`Correo__ApiKey`) y en user-secrets en local; nunca en `appsettings.json`.

**Permisos y seguridad.** Los dos endpoints son anónimos por diseño. La dirección del enlace sale de la configuración, nunca del pedido. Una cuenta inactiva recibe la misma respuesta y ningún correo. La pantalla de restablecer no carga nada externo y manda `Referrer-Policy: no-referrer`, para que el token no se filtre.

**Riesgos.** Sin proveedor la función no sirve en producción: se despliega cuando se elija uno. Detrás de Azure, el límite por IP necesita leer la IP reenviada. Sin dominio propio el correo puede caer en spam.

**Cómo probarlo.** En desarrollo, con el enlace en el log: email que existe y que no existe (misma pantalla y mismo tiempo); usar el enlace dos veces; dejarlo vencer; pedir dos y usar el primero; contraseña corta; ingresar con la nueva; una sesión vieja queda afuera. En la base, confirmar que se guarda el hash y no el token.

**Esfuerzo.** Alto.

---

## Orden y dependencias

A0 → A1 → B → C → E → D → A2.
- A0 va primero: sin el registro cerrado, los permisos de administrador de las demás fases se saltean.
- B se apoya en "Mi perfil" (A1) para el enlace del doctor a su horario.
- A2 reutiliza el chequeo de token que agrega C.
- D es solo Web. Va después de C y E porque toca las mismas pantallas (Usuarios y Turnos).

## Pendiente de confirmar

1. **Zona horaria de la Web.** Falta confirmar si el App Service de la Web tiene `WEBSITE_TIME_ZONE` = `Argentina Standard Time`, como la API. Si falta, se agrega ese ajuste; en ningún caso se escribe código (decisión 10).
