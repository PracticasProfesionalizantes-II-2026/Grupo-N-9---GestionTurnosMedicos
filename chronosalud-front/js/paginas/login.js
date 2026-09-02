import { api } from "../api.js";
import { guardarSesion, obtenerSesion } from "../sesion.js";
import { $, aviso, valoresFormulario } from "../ui.js";

if (obtenerSesion()) window.location.href = "dashboard.html";

const formulario = $("#form-login");
const boton = $("#btn-ingresar");

formulario.addEventListener("submit", async (evento) => {
  evento.preventDefault();
  aviso("#mensaje", "");

  const { email, contrasena } = valoresFormulario(formulario);
  if (!email || !contrasena) {
    aviso("#mensaje", "Completá email y contraseña.");
    return;
  }

  boton.disabled = true;
  boton.textContent = "Ingresando…";

  try {
    const sesion = await api.post("/usuarios/login", { email, contrasena }, { anonimo: true });
    guardarSesion(sesion);
    window.location.href = "dashboard.html";
  } catch (error) {
    aviso("#mensaje", error.status === 401 ? "Email o contraseña incorrectos." : error.message);
    boton.disabled = false;
    boton.textContent = "Ingresar";
  }
});
