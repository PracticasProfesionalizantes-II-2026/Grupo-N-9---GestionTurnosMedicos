import { api } from "../api.js";
import { guardarSesion, obtenerSesion } from "../sesion.js";
import { $, aviso, valoresFormulario, opcional } from "../ui.js";

if (obtenerSesion()) window.location.href = "dashboard.html";

const AYUDA_ROL = {
  paciente: "Se crea automáticamente tu ficha clínica para poder sacar turnos.",
  doctor: "Un administrador debe cargar después tu matrícula y especialidad.",
  administrador: "Acceso completo a la gestión del sistema.",
};

const formulario = $("#form-registro");
const boton = $("#btn-crear");
const selectRol = $("#rol");
const ayudaRol = $("#ayuda-rol");

const actualizarAyuda = () => (ayudaRol.textContent = AYUDA_ROL[selectRol.value] ?? "");
selectRol.addEventListener("change", actualizarAyuda);
actualizarAyuda();

formulario.addEventListener("submit", async (evento) => {
  evento.preventDefault();
  aviso("#mensaje", "");

  const datos = valoresFormulario(formulario);
  if (!datos.nombre || !datos.apellido || !datos.email || !datos.contrasena) {
    aviso("#mensaje", "Completá todos los campos obligatorios.");
    return;
  }
  if (datos.contrasena.length < 8) {
    aviso("#mensaje", "La contraseña debe tener al menos 8 caracteres.");
    return;
  }

  boton.disabled = true;
  boton.textContent = "Creando cuenta…";

  try {
    const respuesta = await api.post(
      "/usuarios/registro",
      { ...datos, telefono: opcional(datos.telefono) },
      { anonimo: true }
    );
    guardarSesion({
      token: respuesta.token,
      rol: respuesta.rol,
      idUsuario: respuesta.idUsuario,
      nombre: datos.nombre,
    });
    window.location.href = "dashboard.html";
  } catch (error) {
    aviso("#mensaje", error.message);
    boton.disabled = false;
    boton.textContent = "Crear cuenta";
  }
});
