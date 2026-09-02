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

const sesion = requerirSesion();
const esAdmin = tieneRol(ROLES.ADMIN);

if (sesion) {
  montarLayout("doctores");
  activarCierreModales();
  iniciar();
}

function iniciar() {
  $("#btn-nuevo").hidden = !esAdmin;
  $("#col-acciones").hidden = !esAdmin;

  cargarDoctores();

  $("#form-filtros").addEventListener("submit", (evento) => {
    evento.preventDefault();
    cargarDoctores();
  });
  $("#btn-limpiar").addEventListener("click", () => {
    $("#form-filtros").reset();
    cargarDoctores();
  });
  $("#btn-nuevo").addEventListener("click", () => abrirFormulario());
  $("#form-doctor").addEventListener("submit", guardar);
  $("#tabla-doctores").addEventListener("click", manejarAccion);
}

async function cargarDoctores() {
  const cuerpo = $("#tabla-doctores");
  cuerpo.innerHTML = filaMensaje(5, "Cargando…");

  const filtros = valoresFormulario($("#form-filtros"));

  try {
    const datos = await api.get("/doctores", { ...filtros, limite: 200 });
    const doctores = datos.doctores ?? [];
    $("#resumen").textContent = `${datos.total ?? doctores.length} doctor(es)`;

    if (!doctores.length) {
      cuerpo.innerHTML = filaMensaje(5, "No se encontraron doctores.");
      return;
    }

    cuerpo.innerHTML = doctores
      .map(
        (d) => `
        <tr>
          <td>${d.idDoctor}</td>
          <td>${escapar(d.nombre)}</td>
          <td>${escapar(d.especialidad)}</td>
          <td>${escapar(d.matricula)}</td>
          ${
            esAdmin
              ? `<td>
                   <div class="acciones">
                     <button type="button" class="boton boton--secundario boton--chico" data-editar="${d.idDoctor}">Editar</button>
                     <button type="button" class="boton boton--peligro boton--chico" data-baja="${d.idDoctor}">Dar de baja</button>
                   </div>
                 </td>`
              : ""
          }
        </tr>`
      )
      .join("");
  } catch (error) {
    aviso("#mensaje", error.message);
    cuerpo.innerHTML = filaMensaje(5, "No se pudieron cargar los doctores.");
  }
}

async function abrirFormulario(id = null) {
  const formulario = $("#form-doctor");
  formulario.reset();
  aviso("#mensaje-modal", "");
  $("#id-doctor").value = id ?? "";
  $("#titulo-modal").textContent = id ? `Editar doctor #${id}` : "Nuevo doctor";
  // Al editar sólo se pueden cambiar especialidad y consultorio.
  $("#campo-usuario").hidden = !!id;
  $("#campo-matricula").hidden = !!id;
  $("#id-usuario").required = !id;
  $("#matricula").required = !id;

  if (id) {
    try {
      const d = await api.get(`/doctores/${id}`);
      $("#especialidad").value = d.especialidad;
      $("#consultorio").value = d.consultorio ?? "";
    } catch (error) {
      aviso("#mensaje-modal", error.message);
    }
  }

  abrirModal("modal-doctor");
}

async function guardar(evento) {
  evento.preventDefault();
  aviso("#mensaje-modal", "");

  const datos = valoresFormulario($("#form-doctor"));
  const id = datos.id_doctor;

  try {
    if (id) {
      await api.put(`/doctores/${id}`, {
        especialidad: datos.especialidad,
        consultorio: opcional(datos.consultorio),
      });
      toast("Doctor actualizado.");
    } else {
      await api.post("/doctores", {
        idUsuario: Number(datos.idUsuario),
        especialidad: datos.especialidad,
        matricula: datos.matricula,
        consultorio: opcional(datos.consultorio),
      });
      toast("Doctor creado.");
    }
    cerrarModal("modal-doctor");
    await cargarDoctores();
  } catch (error) {
    aviso("#mensaje-modal", error.message);
  }
}

async function manejarAccion(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.editar) await abrirFormulario(boton.dataset.editar);

  if (boton.dataset.baja) {
    if (!confirm(`¿Dar de baja al doctor #${boton.dataset.baja}?`)) return;
    try {
      await api.del(`/doctores/${boton.dataset.baja}`);
      toast("Doctor dado de baja.");
      await cargarDoctores();
    } catch (error) {
      toast(error.message, "error");
    }
  }
}
