import { api } from "../api.js";
import { ROLES } from "../config.js";
import { guardarSesion, obtenerSesion, requerirSesion, tieneRol } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { $, aviso, escapar, opcional, toast, valoresFormulario } from "../ui.js";

const sesion = requerirSesion();
const esAdmin = tieneRol(ROLES.ADMIN);

if (sesion) {
  montarLayout("cuenta");
  iniciar();
}

function iniciar() {
  cargarCuenta();

  $("#form-cuenta").addEventListener("submit", guardar);

  $("#tarjeta-baja").hidden = !esAdmin;
  if (esAdmin) {
    $("#form-buscar-usuario").addEventListener("submit", buscarUsuario);
    $("#resultado-busqueda").addEventListener("click", darDeBaja);
  }
}

async function cargarCuenta() {
  try {
    const usuario = await api.get(`/usuarios/${sesion.idUsuario}`);

    $("#dato-email").textContent = usuario.email;
    $("#dato-rol").textContent = usuario.rol;
    $("#dato-id").textContent = `#${usuario.idUsuario}`;
    $("#nombre").value = usuario.nombre;
    $("#apellido").value = usuario.apellido;
    $("#telefono").value = usuario.telefono ?? "";
  } catch (error) {
    aviso("#mensaje", error.message);
  }
}

async function guardar(evento) {
  evento.preventDefault();
  aviso("#mensaje-perfil", "");

  const datos = valoresFormulario($("#form-cuenta"));

  if (datos.contrasena && datos.contrasena.length < 8) {
    aviso("#mensaje-perfil", "La contraseña debe tener al menos 8 caracteres.");
    return;
  }

  try {
    await api.put(`/usuarios/${sesion.idUsuario}`, {
      nombre: datos.nombre,
      apellido: datos.apellido,
      telefono: opcional(datos.telefono),
      contrasena: opcional(datos.contrasena),
    });

    // El nombre viaja en la sesión guardada: se actualiza para que la cabecera
    // no siga mostrando el anterior.
    const actual = obtenerSesion();
    if (actual) guardarSesion({ ...actual, nombre: datos.nombre });

    $("#contrasena").value = "";
    toast("Datos actualizados.");
    await cargarCuenta();
    montarLayout("cuenta");
  } catch (error) {
    aviso("#mensaje-perfil", error.message);
  }
}

/* ── Baja de usuarios (administrador) ────────────────────── */

async function buscarUsuario(evento) {
  evento.preventDefault();
  aviso("#mensaje-baja", "");

  const { id } = valoresFormulario($("#form-buscar-usuario"));
  const contenedor = $("#resultado-busqueda");
  contenedor.innerHTML = "";

  try {
    const usuario = await api.get(`/usuarios/${id}`);
    const esUnoMismo = Number(id) === Number(sesion.idUsuario);

    contenedor.innerHTML = `
      <dl class="definiciones">
        <div><dt>Nombre</dt><dd>${escapar(usuario.nombre)} ${escapar(usuario.apellido)}</dd></div>
        <div><dt>Email</dt><dd>${escapar(usuario.email)}</dd></div>
        <div><dt>Rol</dt><dd>${escapar(usuario.rol)}</dd></div>
      </dl>
      <div class="acciones">
        ${
          esUnoMismo
            ? `<p class="texto-suave">Es tu propio usuario: no podés darte de baja desde acá.</p>`
            : `<button type="button" class="boton boton--peligro" data-baja="${usuario.idUsuario}">
                 Dar de baja
               </button>`
        }
      </div>`;
  } catch (error) {
    aviso("#mensaje-baja", error.message);
  }
}

async function darDeBaja(evento) {
  const boton = evento.target.closest("button[data-baja]");
  if (!boton) return;

  if (!confirm("¿Dar de baja este usuario? No va a poder volver a iniciar sesión.")) return;

  try {
    await api.del(`/usuarios/${boton.dataset.baja}`);
    toast("Usuario dado de baja.");
    $("#resultado-busqueda").innerHTML = "";
    $("#form-buscar-usuario").reset();
  } catch (error) {
    aviso("#mensaje-baja", error.message);
  }
}
