import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion, tieneRol } from "../sesion.js";
import { montarLayout } from "../layout.js";
import {
  $,
  abrirModal,
  activarCierreModales,
  aviso,
  cerrarModal,
  escapar,
  filaMensaje,
  opcional,
  toast,
  valoresFormulario,
} from "../ui.js";

const sesion = requerirSesion([ROLES.ADMIN, ROLES.DOCTOR]);
const puedeEliminar = tieneRol(ROLES.ADMIN);

if (sesion) {
  montarLayout("medicamentos");
  activarCierreModales();
  iniciar();
}

function iniciar() {
  cargarMedicamentos();
  $("#btn-nuevo").addEventListener("click", () => abrirFormulario());
  $("#form-medicamento").addEventListener("submit", guardar);
  $("#tabla-medicamentos").addEventListener("click", manejarAccion);
}

async function cargarMedicamentos() {
  const cuerpo = $("#tabla-medicamentos");
  cuerpo.innerHTML = filaMensaje(4, "Cargando…");

  try {
    const datos = await api.get("/medicamentos");
    const medicamentos = datos.medicamentos ?? [];
    $("#resumen").textContent = `${medicamentos.length} medicamento(s)`;

    if (!medicamentos.length) {
      cuerpo.innerHTML = filaMensaje(4, "Todavía no hay medicamentos cargados.");
      return;
    }

    cuerpo.innerHTML = medicamentos
      .map(
        (m) => `
        <tr>
          <td>${m.idMedicamento}</td>
          <td>${escapar(m.nombre)}</td>
          <td>${escapar(m.descripcion) || "—"}</td>
          <td>
            <div class="acciones">
              <button type="button" class="boton boton--secundario boton--chico"
                data-editar="${m.idMedicamento}">Editar</button>
              ${
                puedeEliminar
                  ? `<button type="button" class="boton boton--peligro boton--chico" data-eliminar="${m.idMedicamento}">Eliminar</button>`
                  : ""
              }
            </div>
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    aviso("#mensaje", error.message);
    cuerpo.innerHTML = filaMensaje(4, "No se pudieron cargar los medicamentos.");
  }
}

function abrirFormulario(datos = null) {
  $("#form-medicamento").reset();
  aviso("#mensaje-modal", "");
  $("#id-medicamento").value = datos?.id ?? "";
  $("#titulo-modal").textContent = datos ? "Editar medicamento" : "Nuevo medicamento";
  if (datos) {
    $("#nombre").value = datos.nombre;
    $("#descripcion").value = datos.descripcion;
  }
  abrirModal("modal-medicamento");
}

async function guardar(evento) {
  evento.preventDefault();
  aviso("#mensaje-modal", "");

  const datos = valoresFormulario($("#form-medicamento"));
  const cuerpo = { nombre: datos.nombre, descripcion: opcional(datos.descripcion) };

  try {
    if (datos.id_medicamento) {
      await api.put(`/medicamentos/${datos.id_medicamento}`, cuerpo);
      toast("Medicamento actualizado.");
    } else {
      await api.post("/medicamentos", cuerpo);
      toast("Medicamento creado.");
    }
    cerrarModal("modal-medicamento");
    await cargarMedicamentos();
  } catch (error) {
    aviso("#mensaje-modal", error.message);
  }
}

async function manejarAccion(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.editar) {
    // Se relee el medicamento de la API para editar sobre el dato actual y no
    // sobre lo que quedó pintado en la tabla.
    try {
      const m = await api.get(`/medicamentos/${boton.dataset.editar}`);
      abrirFormulario({
        id: m.idMedicamento,
        nombre: m.nombre,
        descripcion: m.descripcion ?? "",
      });
    } catch (error) {
      toast(error.message, "error");
    }
  }

  if (boton.dataset.eliminar) {
    if (!confirm("¿Eliminar este medicamento del vademécum?")) return;
    try {
      await api.del(`/medicamentos/${boton.dataset.eliminar}`);
      toast("Medicamento eliminado.");
      await cargarMedicamentos();
    } catch (error) {
      toast(error.message, "error");
    }
  }
}
