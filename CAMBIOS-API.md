# Cambios en la API — Rama `fix/pacientes-y-turnos`

**Fecha:** 2026-10-08 · **Rama:** `fix/pacientes-y-turnos`

Un apartado por cada paso de la rama.

---

## Paso 2 — Alta de paciente completa y borrado de datos de la ficha

**Hay migración:** `AgregarDatosAltaPaciente` agrega seis columnas a `Pacientes`, todas admiten nulo y no toca datos. El script se corre en Azure **antes** de desplegar la API: la API nueva lee esas columnas.

### Campos nuevos del paciente

| Campo (JSON) | Largo máximo |
|---|---|
| `tipoDocumento` | 20 (`DNI`, `LC`, `LE`, `Pasaporte`) |
| `provincia` | 80 |
| `localidad` | 100 |
| `codigoPostal` | 10 |
| `contactoEmergenciaNombre` | 120 |
| `contactoEmergenciaTelefono` | 30 |

- Vienen en `GET /pacientes/{id}` y en `GET /pacientes/me`.
- Se pueden mandar en `PUT /pacientes/{id}`.

### `POST /usuarios/registro` acepta la ficha del paciente

```json
{
  "nombre": "Lucía", "apellido": "Gómez", "email": "lucia@demo.com",
  "contrasena": "Clave2026!", "telefono": "11-5000-0000", "rol": "paciente",
  "ficha": { "dni": "30111222", "tipoDocumento": "DNI", "fechaNacimiento": "1990-05-20",
             "provincia": "Córdoba", "localidad": "Villa María" }
}
```

- **Quién la manda:** la ficha solo se tiene en cuenta si el pedido trae el token de un **administrador**. Sin token (el registro público) se ignora, así nadie puede averiguar qué DNI ya están cargados.
- **Cómo se guarda:** la cuenta y el paciente se guardan juntos: o se crean los dos o ninguno.
- **DNI repetido:** `409` con `"Ya existe un paciente registrado con ese DNI."`, y no se crea la cuenta.
- **Respuesta:** suma `idPaciente`.

### `PUT /pacientes/{id}` puede borrar datos

- **Lo de siempre:** un campo que no viene, o que viene vacío, no cambia lo que ya estaba.
- **Nuevo, `borrar`:** la lista de campos a vaciar.
  ```json
  { "localidad": "Villa María", "borrar": ["alergias", "direccion"] }
  ```
  - Nombres válidos (sin importar mayúsculas): `fechaNacimiento`, `sexo`, `grupoSanguineo`, `alergias`, `condiciones`, `direccion`, `nacionalidad`, `estadoCivil`, `tipoDocumento`, `provincia`, `localidad`, `codigoPostal`, `contactoEmergenciaNombre`, `contactoEmergenciaTelefono` y `dni`.
  - Un nombre desconocido → `400` y no se guarda nada.
- **DNI:** un **paciente** no puede cambiar ni borrar un DNI ya cargado (`400`, con el motivo). El personal (doctor o administrador) sí.

### A quién le pega

- Todos los pedidos que andaban siguen andando: todo lo nuevo es opcional.
- Si alguien probaba con Scalar o Postman cambiar el DNI de un paciente usando el token de ese mismo paciente, ahora recibe `400`.

### Archivos tocados (API)

| Archivo | Cambio |
|---|---|
| `Entidades/Paciente.cs` | Seis campos nuevos |
| `Datos/AppDbContext.cs` | Largo máximo de los campos nuevos |
| `Logica/DTOs/PacienteDTOs.cs` | Campos nuevos en `PacienteDto` y `PacienteUpdateDto`, y `Borrar` |
| `Logica/DTOs/UsuarioDTOs.cs` | `Ficha` en el registro, `IdPaciente` en la respuesta |
| `Logica/FichaPaciente.cs` | Nuevo: copiar los datos de la ficha y borrar campos |
| `Logica/UsuarioLogica.cs` | `Registrar` guarda cuenta y paciente juntos, con la ficha |
| `Logica/PacienteLogica.cs` | `Actualizar` usa `FichaPaciente` y la regla del DNI |
| `Repositorios/UsuarioRepository.cs` | `Agregar` traduce el dato repetido de SQL Server |
| `Repositorios/ErroresDeBase.cs`, `DatoRepetidoException.cs` | Nuevos: reconocer el error de dato repetido |
