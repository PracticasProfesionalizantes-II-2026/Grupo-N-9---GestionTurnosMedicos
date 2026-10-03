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