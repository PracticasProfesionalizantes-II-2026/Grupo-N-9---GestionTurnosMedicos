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