import { api } from "../api.js";
import { requerirSesion } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { $, aviso, escapar, filaMensaje, formatearFecha, toast, valoresFormulario } from "../ui.js";

const sesion = requerirSesion();

if (sesion) {
  montarLayout("notificaciones");
  iniciar();
}

function iniciar() {
  cargarNotificaciones();

  $("#form-filtros").addEventListener("submit", (evento) => {
    evento.preventDefault();
    cargarNotificaciones();
  });
  $("#btn-limpiar").addEventListener("click", () => {
    $("#form-filtros").reset();
    cargarNotificaciones();
  });
  $("#tabla-notificaciones").addEventListener("click", marcarLeida);
}

async function cargarNotificaciones() {
  const cuerpo = $("#tabla-notificaciones");
  cuerpo.innerHTML = filaMensaje(5, "Cargando…");

  const filtros = valoresFormulario($("#form-filtros"));

  try {
    const datos = await api.get(`/usuarios/${sesion.idUsuario}/notificaciones`, filtros);
    const notificaciones = datos.notificaciones ?? [];
    $("#resumen").textContent = `${datos.total ?? notificaciones.length} notificación(es)`;

    if (!notificaciones.length) {
      cuerpo.innerHTML = filaMensaje(5, "No tenés notificaciones.");
      return;
    }

    cuerpo.innerHTML = notificaciones
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
      .map(
        (n) => `
        <tr>
          <td>${formatearFecha(n.fecha)}</td>
          <td style="text-transform:capitalize">${escapar(n.tipo)}</td>
          <td>${escapar(n.mensaje)}</td>
          <td>
            <span class="badge badge--${n.leida ? "completado" : "pendiente"}">
              ${n.leida ? "Leída" : "No leída"}
            </span>
          </td>
          <td>
            ${
              n.leida
                ? "—"
                : `<button type="button" class="boton boton--secundario boton--chico" data-leer="${n.id}">Marcar leída</button>`
            }
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    aviso("#mensaje", error.message);
    cuerpo.innerHTML = filaMensaje(5, "No se pudieron cargar las notificaciones.");
  }
}

async function marcarLeida(evento) {
  const boton = evento.target.closest("button[data-leer]");
  if (!boton) return;

  try {
    await api.patch(`/notificaciones/${boton.dataset.leer}/leer`);
    toast("Notificación marcada como leída.");
    await cargarNotificaciones();
  } catch (error) {
    toast(error.message, "error");
  }
}
