# ChronoSalud — Frontend

Interfaz web de la API `ChronoSaludApi`. Está hecha con HTML, CSS y JavaScript puro
(módulos ES), sin frameworks ni proceso de build.

## Cómo levantarlo

1. **Levantar la API** (desde la raíz del repo):

   ```
   dotnet run --project ChronoSaludApi
   ```

   Queda escuchando en `http://localhost:5001`.

2. **Servir el frontend.** Los módulos ES no funcionan abriendo el archivo con doble clic
   (`file://`): hay que servirlo por HTTP. En otra terminal:

   ```
   powershell -ExecutionPolicy Bypass -File chronosalud-front\servir.ps1
   ```

   [`servir.ps1`](servir.ps1) es un servidor estático que usa lo que ya trae Windows: no
   hace falta instalar Node ni Python. Para usar otro puerto: `-Puerto 8080`.

   Alternativas equivalentes, si las tenés a mano: la extensión **Live Server** de VS Code
   (botón "Go Live") o `python -m http.server 5500` dentro de `chronosalud-front/`.

3. Abrir `http://localhost:5500` en el navegador.

Si la API corre en otro puerto o servidor, cambiar `API_URL` en
[`js/config.js`](js/config.js).

## Estructura

```
chronosalud-front/
├── index.html            login
├── registro.html         alta de usuario
├── dashboard.html        resumen e ingreso rápido
├── turnos.html           agenda: alta, reprogramación y cancelación
├── pacientes.html        listado de pacientes (doctor / administrador)
├── paciente.html         ficha: perfil, coberturas, historial, recetas, estudios
├── doctores.html         listado y ABM de profesionales (alta sólo administrador)
├── medicamentos.html     vademécum
├── notificaciones.html   bandeja del usuario
├── reportes.html         estadísticas de turnos, disponibilidad y pacientes
├── cuenta.html           datos de la cuenta, contraseña y baja de usuarios
├── css/estilos.css
└── js/
    ├── config.js         URL de la API, roles y constantes
    ├── api.js            cliente HTTP: JWT, errores, 401 → login
    ├── sesion.js         sesión en localStorage y guardas por rol
    ├── perfil.js         resuelve el id de paciente/doctor del usuario logueado
    ├── layout.js         cabecera y menú según rol
    ├── ui.js             helpers de DOM, fechas, modales y avisos
    └── paginas/          lógica de cada pantalla
```

## Qué ve cada rol

| Pantalla       | Paciente            | Doctor | Administrador |
| -------------- | ------------------- | ------ | ------------- |
| Turnos         | sólo los suyos      | todos  | todos         |
| Pacientes      | —                   | sí     | sí            |
| Mi perfil      | sí                  | —      | —             |
| Doctores       | consulta            | consulta | ABM completo |
| Medicamentos   | —                   | ABM (sin borrar) | ABM completo |
| Reportes       | —                   | sí     | sí            |
| Notificaciones | sí                  | sí     | sí            |
| Mi cuenta      | sí                  | sí     | sí + baja de usuarios |

Las acciones que la API restringe por rol (emitir recetas, solicitar estudios, cargar
resultados, reprogramar turnos) se ocultan cuando el usuario no las tiene habilitadas.

## Cobertura de la API

El frontend consume los 43 endpoints de `ChronoSaludApi`. Tres de ellos responden hoy
con un esqueleto en el backend y se muestran tal cual lo que devuelven, sin inventar
datos: `GET /reportes/pacientes` y `GET /reportes/disponibilidad` (contestan el período
recibido, sin métricas) y `GET /recetas/{id}/descargar` (todavía no genera el PDF).
`GET /estudios/{id}/descargar` sí devuelve la URL del archivo y la pantalla la abre.

## Notas

- La sesión (token JWT, rol, nombre) se guarda en `localStorage`. El token dura 8 horas;
  al vencer, cualquier llamada devuelve 401 y la app vuelve al login.
- Al registrarse con rol **paciente** se crea automáticamente la ficha clínica.
  Los usuarios con rol **doctor** necesitan que un administrador les cargue matrícula y
  especialidad desde *Doctores → Nuevo doctor* antes de poder emitir recetas o estudios.
