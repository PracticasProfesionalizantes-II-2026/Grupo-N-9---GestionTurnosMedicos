import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion, tieneRol } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { idPacienteActual } from "../perfil.js";
import {
  $,
  abrirModal,
  activarCierreModales,
  aviso,
  badge,
  cerrarModal,
  escapar,
  fechaParaApi,
  fechaParaInput,
  filaMensaje,
  formatearFecha,
  opcional,
  toast,
  valoresFormulario,
} from "../ui.js";

const sesion = requerirSesion();
const puedeEditar = tieneRol(ROLES.ADMIN, ROLES.SECRETARIO);
const esPaciente = sesion?.rol === ROLES.PACIENTE;

let miIdPaciente = null;
let doctores = [];
let pacientes = [];

if (sesion) {
  montarLayout("turnos");
  activarCierreModales();
  iniciar();
}

async function iniciar() {
  $("#bajada").textContent = esPaciente ? "Tus turnos médicos" : "Agenda de turnos de la clínica";

  if (esPaciente) {
    miIdPaciente = await idPacienteActual();
    if (!miIdPaciente) {
      aviso(
        "#mensaje",
        "Tu usuario todavía no tiene ficha de paciente asociada. Pedile a un administrador que la cargue.",
        "info"
      );
      $("#btn-nuevo").disabled = true;
    }
  }

  await Promise.all([cargarDoctores(), cargarPacientes()]);
  await cargarTurnos();

  $("#form-filtros").addEventListener("submit", (evento) => {
    evento.preventDefault();
    cargarTurnos();
  });

  $("#btn-limpiar").addEventListener("click", () => {
    $("#form-filtros").reset();
    cargarTurnos();
  });

  $("#btn-nuevo").addEventListener("click", () => abrirFormulario());
  $("#form-turno").addEventListener("submit", guardar);
  $("#tabla-turnos").addEventListener("click", manejarAccion);

  if (new URLSearchParams(location.search).get("nuevo") === "1" && !$("#btn-nuevo").disabled) {
    abrirFormulario();
  }
}

async function cargarDoctores() {
  try {
    const datos = await api.get("/doctores", { limite: 200 });
    doctores = datos.doctores ?? [];
  } catch {
    doctores = [];
  }

  const opciones = doctores
    .map((d) => `<option value="${d.idDoctor}">${escapar(d.nombre)} — ${escapar(d.especialidad)}</option>`)
    .join("");

  $("#filtro-doctor").innerHTML = `<option value="">Todos</option>${opciones}`;
  $("#doctor").innerHTML = doctores.length
    ? opciones
    : `<option value="">No hay doctores cargados</option>`;
}

async function cargarPacientes() {
  const campo = $("#campo-paciente");

  if (esPaciente) {
    // El paciente sólo puede sacar turnos para sí mismo.
    campo.hidden = true;
    $("#paciente").innerHTML = `<option value="${miIdPaciente ?? ""}">Yo</option>`;
    return;
  }

  try {
    const datos = await api.get("/pacientes", { limite: 500 });
    pacientes = datos.pacientes ?? [];
    $("#paciente").innerHTML = pacientes.length
      ? pacientes
          .map(
            (p) =>
              `<option value="${p.idPaciente}">${escapar(p.apellido)}, ${escapar(p.nombre)}</option>`
          )
          .join("")
      : `<option value="">No hay pacientes cargados</option>`;
  } catch {
    // Roles sin permiso para listar pacientes cargan el id a mano.
    campo.innerHTML = `
      <label for="paciente">ID de paciente</label>
      <input type="number" id="paciente" name="id_paciente" min="1" required />`;
  }
}

function filtrosActuales() {
  const datos = valoresFormulario($("#form-filtros"));
  const filtros = { limite: 200 };
  if (datos.doctor_id) filtros.doctor_id = datos.doctor_id;
  if (datos.estado) filtros.estado = datos.estado;
  if (datos.fecha_desde) filtros.fecha_desde = fechaParaApi(datos.fecha_desde);
  if (datos.fecha_hasta) filtros.fecha_hasta = fechaParaApi(datos.fecha_hasta);
  if (esPaciente && miIdPaciente) filtros.paciente_id = miIdPaciente;
  return filtros;
}

async function cargarTurnos() {
  const cuerpo = $("#tabla-turnos");
  cuerpo.innerHTML = filaMensaje(6, "Cargando…");

  if (esPaciente && !miIdPaciente) {
    cuerpo.innerHTML = filaMensaje(6, "Sin turnos para mostrar.");
    return;
  }

  try {
    const datos = await api.get("/turnos", filtrosActuales());
    const turnos = datos.turnos ?? [];
    $("#resumen").textContent = `${datos.total ?? turnos.length} turno(s)`;

    if (!turnos.length) {
      cuerpo.innerHTML = filaMensaje(6, "No se encontraron turnos con esos filtros.");
      return;
    }

    cuerpo.innerHTML = turnos
      .sort((a, b) => new Date(b.fechaInicio) - new Date(a.fechaInicio))
      .map(
        (t) => `
        <tr>
          <td>${t.idTurno}</td>
          <td>${formatearFecha(t.fechaInicio)}</td>
          <td>${escapar(t.paciente)}</td>
          <td>${escapar(t.doctor)}</td>
          <td>${badge(t.estado)}</td>
          <td>
            <div class="acciones">
              <button type="button" class="boton boton--secundario boton--chico" data-ver="${t.idTurno}">Ver</button>
              ${
                puedeEditar
                  ? `<button type="button" class="boton boton--secundario boton--chico" data-editar="${t.idTurno}">Editar</button>`
                  : ""
              }
              ${
                t.estado !== "cancelado"
                  ? `<button type="button" class="boton boton--peligro boton--chico" data-cancelar="${t.idTurno}">Cancelar</button>`
                  : ""
              }
            </div>
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    aviso("#mensaje", error.message);
    cuerpo.innerHTML = filaMensaje(6, "No se pudieron cargar los turnos.");
  }
}

async function manejarAccion(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.ver) await verDetalle(boton.dataset.ver);
  if (boton.dataset.editar) await abrirFormulario(boton.dataset.editar);
  if (boton.dataset.cancelar) await cancelar(boton.dataset.cancelar);
}

async function verDetalle(id) {
  const cuerpo = $("#cuerpo-detalle");
  cuerpo.innerHTML = "<p class='texto-suave'>Cargando…</p>";
  abrirModal("modal-detalle");

  try {
    const t = await api.get(`/turnos/${id}`);
    const doctor = doctores.find((d) => d.idDoctor === t.idDoctor);
    const paciente = pacientes.find((p) => p.idPaciente === t.idPaciente);

    cuerpo.innerHTML = `
      <dl class="definiciones">
        <div><dt>Turno</dt><dd>#${t.idTurno}</dd></div>
        <div><dt>Estado</dt><dd>${badge(t.estado)}</dd></div>
        <div><dt>Fecha</dt><dd>${formatearFecha(t.fechaInicio)}</dd></div>
        <div><dt>Horario</dt><dd>${escapar(t.horaInicio)} a ${escapar(t.horaFin)}</dd></div>
        <div><dt>Paciente</dt><dd>${
          paciente ? escapar(`${paciente.apellido}, ${paciente.nombre}`) : `#${t.idPaciente}`
        }</dd></div>
        <div><dt>Doctor</dt><dd>${
          doctor ? escapar(`${doctor.nombre} — ${doctor.especialidad}`) : `#${t.idDoctor}`
        }</dd></div>
      </dl>
      <div class="campo" style="margin-top:16px">
        <label>Observaciones</label>
        <p class="texto-suave" style="margin:0">${escapar(t.observaciones) || "Sin observaciones."}</p>
      </div>`;
  } catch (error) {
    cuerpo.innerHTML = `<div class="aviso aviso--error">${escapar(error.message)}</div>`;
  }
}

async function abrirFormulario(id = null) {
  const formulario = $("#form-turno");
  formulario.reset();
  aviso("#mensaje-modal", "");
  $("#id-turno").value = id ?? "";
  $("#titulo-modal").textContent = id ? `Editar turno #${id}` : "Nuevo turno";
  $("#campo-estado").hidden = !id;
  // Al editar sólo se cambian fecha, horario, estado y observaciones.
  $("#campo-paciente").hidden = !!id || esPaciente;
  $("#campo-doctor").hidden = !!id;

  if (id) {
    try {
      const t = await api.get(`/turnos/${id}`);
      $("#fecha").value = fechaParaInput(t.fechaInicio);
      $("#hora-inicio").value = t.horaInicio;
      $("#hora-fin").value = t.horaFin;
      $("#estado").value = t.estado;
      $("#observaciones").value = t.observaciones ?? "";
    } catch (error) {
      aviso("#mensaje-modal", error.message);
    }
  }

  abrirModal("modal-turno");
}

async function guardar(evento) {
  evento.preventDefault();
  aviso("#mensaje-modal", "");

  const datos = valoresFormulario($("#form-turno"));
  const id = datos.id_turno;
  const boton = $("#btn-guardar");
  boton.disabled = true;

  try {
    if (id) {
      await api.put(`/turnos/${id}`, {
        fechaInicio: fechaParaApi(datos.fecha_inicio),
        horaInicio: datos.hora_inicio,
        horaFin: datos.hora_fin,
        estado: datos.estado,
        observaciones: opcional(datos.observaciones),
      });
      toast("Turno actualizado.");
    } else {
      const idPaciente = esPaciente ? miIdPaciente : Number(datos.id_paciente);
      if (!idPaciente || !datos.id_doctor) {
        aviso("#mensaje-modal", "Seleccioná paciente y doctor.");
        boton.disabled = false;
        return;
      }
      await api.post("/turnos", {
        idPaciente,
        idDoctor: Number(datos.id_doctor),
        fechaInicio: fechaParaApi(datos.fecha_inicio),
        horaInicio: datos.hora_inicio,
        horaFin: datos.hora_fin,
        observaciones: opcional(datos.observaciones),
      });
      toast("Turno creado.");
    }

    cerrarModal("modal-turno");
    await cargarTurnos();
  } catch (error) {
    aviso("#mensaje-modal", error.message);
  } finally {
    boton.disabled = false;
  }
}

async function cancelar(id) {
  if (!confirm(`¿Cancelar el turno #${id}?`)) return;

  try {
    await api.del(`/turnos/${id}`);
    toast("Turno cancelado.");
    await cargarTurnos();
  } catch (error) {
    toast(error.message, "error");
  }
}
