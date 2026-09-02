import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion, tieneRol } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { idDoctorActual, idPacienteActual } from "../perfil.js";
import {
  $,
  $$,
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
  hoyParaInput,
  opcional,
  toast,
  valoresFormulario,
} from "../ui.js";

const sesion = requerirSesion();
const esDoctor = sesion?.rol === ROLES.DOCTOR;
const puedeCargarResultados = tieneRol(ROLES.ADMIN, ROLES.DOCTOR);

let idPaciente = null;
let medicamentos = [];
let turnosDelPaciente = [];
let recetasDelPaciente = [];

if (sesion) {
  montarLayout(sesion.rol === ROLES.PACIENTE ? "mi-perfil" : "pacientes");
  activarCierreModales();
  iniciar();
}

async function iniciar() {
  const idEnUrl = new URLSearchParams(location.search).get("id");
  idPaciente = idEnUrl ? Number(idEnUrl) : await idPacienteActual();

  if (sesion.rol === ROLES.PACIENTE) $("#btn-volver").hidden = true;

  if (!idPaciente) {
    aviso(
      "#mensaje",
      "No se encontró la ficha de paciente. Si sos paciente, pedile a un administrador que la cargue.",
      "info"
    );
    return;
  }

  activarPestanias();
  $("#btn-nuevo-historial").hidden = !esDoctor;
  $("#btn-nueva-receta").hidden = !esDoctor;
  $("#btn-nuevo-estudio").hidden = !esDoctor;

  await cargarPerfil();
  await cargarMedicamentos();
  await Promise.all([
    cargarCoberturas(),
    cargarHistorial(),
    cargarRecetas(),
    cargarEstudios(),
    cargarTurnosDelPaciente(),
  ]);

  $("#form-perfil").addEventListener("submit", guardarPerfil);
  $("#btn-nueva-cobertura").addEventListener("click", () => abrirModalCobertura());
  $("#form-cobertura").addEventListener("submit", guardarCobertura);
  $("#tabla-coberturas").addEventListener("click", accionCobertura);
  $("#btn-nuevo-historial").addEventListener("click", () => abrirModalHistorial());
  $("#form-historial").addEventListener("submit", guardarHistorial);
  $("#tabla-historial").addEventListener("click", accionHistorial);
  $("#btn-nueva-receta").addEventListener("click", () => abrirModalReceta());
  $("#form-receta").addEventListener("submit", guardarReceta);
  $("#lista-recetas").addEventListener("click", accionReceta);
  $("#btn-agregar-med").addEventListener("click", () => agregarFilaMedicamento());
  $("#btn-nuevo-estudio").addEventListener("click", abrirModalEstudio);
  $("#form-estudio").addEventListener("submit", guardarEstudio);
  $("#tabla-estudios").addEventListener("click", accionEstudio);
  $("#form-resultado").addEventListener("submit", guardarResultado);
}

function activarPestanias() {
  $$(".pestania").forEach((boton) => {
    boton.addEventListener("click", () => {
      $$(".pestania").forEach((b) => b.classList.remove("pestania--activa"));
      $$(".panel").forEach((p) => p.classList.remove("panel--activo"));
      boton.classList.add("pestania--activa");
      $(`#panel-${boton.dataset.panel}`).classList.add("panel--activo");
    });
  });
}

/* ── Perfil ──────────────────────────────────────────────── */

async function cargarPerfil() {
  try {
    const p = await api.get(`/pacientes/${idPaciente}`);
    $("#nombre-paciente").textContent = `${p.apellido}, ${p.nombre}`;
    $("#bajada").textContent = `Paciente #${p.idPaciente}`;
    $("#fecha-nacimiento").value = fechaParaInput(p.fechaNacimiento);
    $("#sexo").value = p.sexo ?? "";
    $("#grupo-sanguineo").value = p.grupoSanguineo ?? "";
    $("#alergias").value = p.alergias ?? "";
    $("#condiciones").value = p.condiciones ?? "";
  } catch (error) {
    aviso("#mensaje", error.message);
  }
}

async function guardarPerfil(evento) {
  evento.preventDefault();
  aviso("#mensaje-perfil", "");

  const datos = valoresFormulario($("#form-perfil"));

  try {
    await api.put(`/pacientes/${idPaciente}`, {
      fechaNacimiento: fechaParaApi(datos.fechaNacimiento),
      sexo: opcional(datos.sexo),
      grupoSanguineo: opcional(datos.grupoSanguineo),
      alergias: opcional(datos.alergias),
      condiciones: opcional(datos.condiciones),
    });
    toast("Datos actualizados.");
  } catch (error) {
    aviso("#mensaje-perfil", error.message);
  }
}

/* ── Coberturas ──────────────────────────────────────────── */

async function cargarCoberturas() {
  const cuerpo = $("#tabla-coberturas");

  try {
    const [asociadas, todas] = await Promise.all([
      api.get(`/pacientes/${idPaciente}/coberturas`),
      api.get("/coberturas"),
    ]);

    $("#cobertura").innerHTML = (todas.coberturas ?? [])
      .map((c) => `<option value="${c.idCobertura}">${escapar(c.nombre)}</option>`)
      .join("");

    const lista = asociadas.coberturas ?? [];
    if (!lista.length) {
      cuerpo.innerHTML = filaMensaje(4, "El paciente no tiene coberturas asociadas.");
      return;
    }

    cuerpo.innerHTML = lista
      .map(
        (c) => `
        <tr>
          <td>${escapar(c.nombreCobertura)}</td>
          <td>${escapar(c.plan) || "—"}</td>
          <td>${escapar(c.idAfiliado)}</td>
          <td>
            <div class="acciones">
              <button type="button" class="boton boton--secundario boton--chico"
                data-editar="${c.idCobertura}"
                data-afiliado="${escapar(c.idAfiliado)}"
                data-plan="${escapar(c.plan ?? "")}">Editar</button>
              <button type="button" class="boton boton--peligro boton--chico"
                data-quitar="${c.idCobertura}">Quitar</button>
            </div>
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    cuerpo.innerHTML = filaMensaje(4, error.message);
  }
}

function abrirModalCobertura(datos = null) {
  const formulario = $("#form-cobertura");
  formulario.reset();
  aviso("#mensaje-cobertura", "");
  $("#titulo-cobertura").textContent = datos ? "Editar cobertura" : "Asociar cobertura";
  $("#modo-cobertura").value = datos ? "edicion" : "alta";
  $("#cobertura").disabled = !!datos;

  if (datos) {
    $("#cobertura").value = datos.idCobertura;
    $("#id-afiliado").value = datos.idAfiliado;
    $("#plan").value = datos.plan;
  }

  abrirModal("modal-cobertura");
}

async function guardarCobertura(evento) {
  evento.preventDefault();
  aviso("#mensaje-cobertura", "");

  const datos = valoresFormulario($("#form-cobertura"));
  // Un select deshabilitado no viaja en el FormData: se lee del DOM.
  const idCobertura = Number($("#cobertura").value);
  const cuerpo = {
    idCobertura,
    idAfiliado: datos.idAfiliado,
    plan: opcional(datos.plan),
  };

  try {
    if (datos.modo === "edicion") {
      await api.put(`/pacientes/${idPaciente}/coberturas`, cuerpo);
      toast("Cobertura actualizada.");
    } else {
      await api.post(`/pacientes/${idPaciente}/coberturas`, cuerpo);
      toast("Cobertura asociada.");
    }
    cerrarModal("modal-cobertura");
    await cargarCoberturas();
  } catch (error) {
    aviso("#mensaje-cobertura", error.message);
  }
}

async function accionCobertura(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.editar) {
    abrirModalCobertura({
      idCobertura: boton.dataset.editar,
      idAfiliado: boton.dataset.afiliado,
      plan: boton.dataset.plan,
    });
  }

  if (boton.dataset.quitar) {
    if (!confirm("¿Desvincular esta cobertura del paciente?")) return;
    try {
      await api.del(`/pacientes/${idPaciente}/coberturas/${boton.dataset.quitar}`);
      toast("Cobertura desvinculada.");
      await cargarCoberturas();
    } catch (error) {
      toast(error.message, "error");
    }
  }
}

/* ── Historial clínico ───────────────────────────────────── */

async function cargarTurnosDelPaciente() {
  try {
    const datos = await api.get("/turnos", { paciente_id: idPaciente, limite: 200 });
    turnosDelPaciente = datos.turnos ?? [];
    const opciones =
      `<option value="">Ninguno</option>` +
      turnosDelPaciente
        .map(
          (t) =>
            `<option value="${t.idTurno}">#${t.idTurno} — ${formatearFecha(t.fechaInicio)} (${escapar(
              t.doctor
            )})</option>`
        )
        .join("");
    $("#turno-historial").innerHTML = opciones;
    $("#turno-estudio").innerHTML = opciones;
  } catch {
    // Sin turnos disponibles los selects quedan sólo con "Ninguno".
  }
}

async function cargarHistorial() {
  const cuerpo = $("#tabla-historial");

  try {
    const datos = await api.get(`/pacientes/${idPaciente}/historiales-clinicos`);
    const historiales = datos.historiales ?? [];

    if (!historiales.length) {
      cuerpo.innerHTML = filaMensaje(5, "Sin entradas en el historial clínico.");
      return;
    }

    cuerpo.innerHTML = historiales
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
      .map(
        (h) => `
        <tr>
          <td>${formatearFecha(h.fecha)}</td>
          <td>${escapar(h.diagnostico)}</td>
          <td>${escapar(h.descripcion)}</td>
          <td>${h.idTurno ? `#${h.idTurno}` : "—"}</td>
          <td>
            ${
              esDoctor
                ? `<button type="button" class="boton boton--secundario boton--chico"
                     data-editar-historial="${escapar(fechaParaInput(h.fecha))}"
                     data-diagnostico="${escapar(h.diagnostico)}"
                     data-descripcion="${escapar(h.descripcion)}"
                     data-turno="${h.idTurno ?? ""}">Editar</button>`
                : "—"
            }
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    cuerpo.innerHTML = filaMensaje(5, error.message);
  }
}

// La API identifica la entrada a modificar por su fecha, no por su id: al
// editar, la fecha queda fija para no terminar creando una entrada nueva.
function abrirModalHistorial(datos = null) {
  $("#form-historial").reset();
  aviso("#mensaje-historial", "");

  $("#modo-historial").value = datos ? "edicion" : "alta";
  $("#titulo-historial").textContent = datos ? "Editar entrada" : "Nueva entrada de historial";
  $("#fecha-historial").readOnly = !!datos;

  if (datos) {
    $("#fecha-historial").value = datos.fecha;
    $("#diagnostico").value = datos.diagnostico;
    $("#descripcion-historial").value = datos.descripcion;
    $("#turno-historial").value = datos.idTurno;
    aviso("#mensaje-historial", "Se modifica la entrada de esta fecha.", "info");
  } else {
    $("#fecha-historial").value = hoyParaInput();
  }

  abrirModal("modal-historial");
}

async function guardarHistorial(evento) {
  evento.preventDefault();
  aviso("#mensaje-historial", "");

  const datos = valoresFormulario($("#form-historial"));
  const cuerpo = {
    fecha: fechaParaApi(datos.fecha),
    descripcion: datos.descripcion,
    diagnostico: datos.diagnostico,
    idTurno: datos.idTurno ? Number(datos.idTurno) : null,
  };

  try {
    if (datos.modo === "edicion") {
      await api.put(`/pacientes/${idPaciente}/historiales-clinicos`, cuerpo);
      toast("Entrada actualizada.");
    } else {
      await api.post(`/pacientes/${idPaciente}/historiales-clinicos`, cuerpo);
      toast("Entrada registrada.");
    }
    cerrarModal("modal-historial");
    await cargarHistorial();
  } catch (error) {
    aviso("#mensaje-historial", error.message);
  }
}

function accionHistorial(evento) {
  const boton = evento.target.closest("button[data-editar-historial]");
  if (!boton) return;

  abrirModalHistorial({
    fecha: boton.dataset.editarHistorial,
    diagnostico: boton.dataset.diagnostico,
    descripcion: boton.dataset.descripcion,
    idTurno: boton.dataset.turno,
  });
}

/* ── Recetas ─────────────────────────────────────────────── */

async function cargarRecetas() {
  const contenedor = $("#lista-recetas");

  try {
    const datos = await api.get(`/pacientes/${idPaciente}/recetas`);
    const recetas = datos.recetas ?? [];
    recetasDelPaciente = recetas;

    if (!recetas.length) {
      contenedor.innerHTML = `<p class="texto-suave">El paciente no tiene recetas emitidas.</p>`;
      return;
    }

    contenedor.innerHTML = recetas
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
      .map(
        (r) => `
        <article class="tarjeta" style="box-shadow:none">
          <h3 class="tarjeta__titulo">
            Receta #${r.idReceta}
            <span class="texto-suave">
              Emitida ${formatearFecha(r.fecha)} · vigente hasta ${formatearFecha(r.vigencia)}
            </span>
          </h3>
          ${r.detalles ? `<p class="texto-suave">${escapar(r.detalles)}</p>` : ""}
          <ul class="lista-simple">
            ${(r.medicamentos ?? [])
              .map(
                (m) =>
                  `<li><strong>${escapar(nombreMedicamento(m.idMedicamento))}</strong> — ${escapar(
                    m.dosis
                  )}, ${escapar(m.frecuencia)}${m.duracion ? ` durante ${escapar(m.duracion)}` : ""}${
                    m.indicaciones ? `. ${escapar(m.indicaciones)}` : ""
                  }</li>`
              )
              .join("")}
          </ul>
          <div class="acciones">
            ${
              esDoctor
                ? `<button type="button" class="boton boton--secundario boton--chico"
                     data-editar-receta="${r.idReceta}">Editar</button>`
                : ""
            }
            <button type="button" class="boton boton--secundario boton--chico"
              data-descargar-receta="${r.idReceta}">Descargar</button>
          </div>
        </article>`
      )
      .join("");
  } catch (error) {
    contenedor.innerHTML = `<div class="aviso aviso--error">${escapar(error.message)}</div>`;
  }
}

function nombreMedicamento(id) {
  return medicamentos.find((m) => m.idMedicamento === id)?.nombre ?? `Medicamento #${id}`;
}

async function cargarMedicamentos() {
  if (medicamentos.length) return;
  try {
    const datos = await api.get("/medicamentos");
    medicamentos = datos.medicamentos ?? [];
  } catch {
    medicamentos = [];
  }
}

async function abrirModalReceta(receta = null) {
  const idDoctor = await idDoctorActual();
  await cargarMedicamentos();

  $("#form-receta").reset();
  aviso("#mensaje-receta", "");
  $("#lista-medicamentos").innerHTML = "";
  $("#id-receta").value = receta?.idReceta ?? "";
  $("#titulo-receta").textContent = receta ? `Editar receta #${receta.idReceta}` : "Emitir receta";
  $("#btn-guardar-receta").textContent = receta ? "Guardar cambios" : "Emitir";

  if (!idDoctor) {
    aviso(
      "#mensaje-receta",
      "Tu usuario no tiene ficha de doctor cargada. Un administrador debe crearla para poder emitir recetas."
    );
  } else if (!medicamentos.length) {
    aviso("#mensaje-receta", "No hay medicamentos cargados en el sistema.", "info");
  } else if (receta) {
    $("#fecha-receta").value = fechaParaInput(receta.fecha);
    $("#vigencia-receta").value = fechaParaInput(receta.vigencia);
    $("#detalles-receta").value = receta.detalles ?? "";
    (receta.medicamentos ?? []).forEach((m) => agregarFilaMedicamento(m));
  } else {
    $("#fecha-receta").value = hoyParaInput();
    agregarFilaMedicamento();
  }

  abrirModal("modal-receta");
}

function agregarFilaMedicamento(valores = null) {
  const fila = document.createElement("div");
  fila.className = "campo";
  fila.dataset.medicamento = "";
  fila.innerHTML = `
    <div class="fila-campos">
      <div class="campo">
        <label>Medicamento</label>
        <select data-campo="idMedicamento">
          ${medicamentos
            .map(
              (m) =>
                `<option value="${m.idMedicamento}" ${
                  valores?.idMedicamento === m.idMedicamento ? "selected" : ""
                }>${escapar(m.nombre)}</option>`
            )
            .join("")}
        </select>
      </div>
      <div class="campo">
        <label>Dosis</label>
        <input type="text" data-campo="dosis" placeholder="500 mg" required
          value="${escapar(valores?.dosis ?? "")}" />
      </div>
      <div class="campo">
        <label>Frecuencia</label>
        <input type="text" data-campo="frecuencia" placeholder="cada 8 horas" required
          value="${escapar(valores?.frecuencia ?? "")}" />
      </div>
      <div class="campo">
        <label>Duración</label>
        <input type="text" data-campo="duracion" placeholder="7 días"
          value="${escapar(valores?.duracion ?? "")}" />
      </div>
      <div class="campo">
        <label>&nbsp;</label>
        <button type="button" class="boton boton--secundario boton--chico" data-quitar-med>Quitar</button>
      </div>
    </div>
    <input type="text" data-campo="indicaciones" placeholder="Indicaciones (opcional)"
      value="${escapar(valores?.indicaciones ?? "")}" />`;

  fila.querySelector("[data-quitar-med]").addEventListener("click", () => fila.remove());
  $("#lista-medicamentos").appendChild(fila);
}

function medicamentosDelFormulario() {
  return $$("[data-medicamento]").map((fila) => {
    const leer = (campo) => fila.querySelector(`[data-campo="${campo}"]`).value.trim();
    return {
      idMedicamento: Number(leer("idMedicamento")),
      dosis: leer("dosis"),
      frecuencia: leer("frecuencia"),
      duracion: opcional(leer("duracion")),
      indicaciones: opcional(leer("indicaciones")),
    };
  });
}

async function guardarReceta(evento) {
  evento.preventDefault();
  aviso("#mensaje-receta", "");

  const idDoctor = await idDoctorActual();
  if (!idDoctor) {
    aviso("#mensaje-receta", "No se pudo determinar tu ficha de doctor.");
    return;
  }

  const datos = valoresFormulario($("#form-receta"));
  const meds = medicamentosDelFormulario();

  if (!meds.length) {
    aviso("#mensaje-receta", "Agregá al menos un medicamento.");
    return;
  }
  if (meds.some((m) => !m.dosis || !m.frecuencia)) {
    aviso("#mensaje-receta", "Completá dosis y frecuencia de cada medicamento.");
    return;
  }

  const cuerpo = {
    idPaciente,
    idDoctor,
    idTurno: null,
    fecha: fechaParaApi(datos.fecha),
    vigencia: fechaParaApi(datos.vigencia),
    detalles: opcional(datos.detalles),
    medicamentos: meds,
  };

  try {
    if (datos.idReceta) {
      await api.put(`/recetas/${datos.idReceta}`, cuerpo);
      toast("Receta actualizada.");
    } else {
      await api.post("/recetas", cuerpo);
      toast("Receta emitida.");
    }
    cerrarModal("modal-receta");
    await cargarRecetas();
  } catch (error) {
    aviso("#mensaje-receta", error.message);
  }
}

async function accionReceta(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.editarReceta) {
    const receta = recetasDelPaciente.find(
      (r) => String(r.idReceta) === boton.dataset.editarReceta
    );
    if (receta) await abrirModalReceta(receta);
  }

  if (boton.dataset.descargarReceta) {
    try {
      const datos = await api.get(`/recetas/${boton.dataset.descargarReceta}/descargar`);
      // La API todavía no genera el PDF: devuelve los datos de la receta.
      toast(datos.mensaje ?? "Receta lista para descargar.", "info");
    } catch (error) {
      toast(error.message, "error");
    }
  }
}

/* ── Estudios ────────────────────────────────────────────── */

async function cargarEstudios() {
  const cuerpo = $("#tabla-estudios");

  try {
    const datos = await api.get(`/pacientes/${idPaciente}/estudios`);
    const estudios = datos.estudios ?? [];

    if (!estudios.length) {
      cuerpo.innerHTML = filaMensaje(6, "El paciente no tiene estudios solicitados.");
      return;
    }

    cuerpo.innerHTML = estudios
      .sort((a, b) => new Date(b.fechaSolicitud) - new Date(a.fechaSolicitud))
      .map(
        (e) => `
        <tr>
          <td>${e.idEstudio}</td>
          <td style="text-transform:capitalize">${escapar(e.tipo)}</td>
          <td>${formatearFecha(e.fechaSolicitud)}</td>
          <td>${badge(e.estado)}</td>
          <td>${escapar(e.resultado) || "—"}</td>
          <td>
            <div class="acciones">
              ${
                puedeCargarResultados
                  ? `<button type="button" class="boton boton--secundario boton--chico" data-resultado="${e.idEstudio}">
                       Cargar resultado
                     </button>`
                  : ""
              }
              ${
                e.archivoUrl
                  ? `<button type="button" class="boton boton--secundario boton--chico" data-descargar-estudio="${e.idEstudio}">
                       Descargar
                     </button>`
                  : ""
              }
              ${!puedeCargarResultados && !e.archivoUrl ? "—" : ""}
            </div>
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    cuerpo.innerHTML = filaMensaje(6, error.message);
  }
}

function abrirModalEstudio() {
  $("#form-estudio").reset();
  aviso("#mensaje-estudio", "");
  $("#fecha-estudio").value = hoyParaInput();
  abrirModal("modal-estudio");
}

async function guardarEstudio(evento) {
  evento.preventDefault();
  aviso("#mensaje-estudio", "");

  const datos = valoresFormulario($("#form-estudio"));

  try {
    await api.post("/estudios", {
      idPaciente,
      idTurno: datos.idTurno ? Number(datos.idTurno) : null,
      tipo: datos.tipo,
      descripcion: datos.descripcion,
      fechaSolicitud: fechaParaApi(datos.fechaSolicitud),
    });
    toast("Estudio solicitado.");
    cerrarModal("modal-estudio");
    await cargarEstudios();
  } catch (error) {
    aviso("#mensaje-estudio", error.message);
  }
}

async function accionEstudio(evento) {
  const boton = evento.target.closest("button");
  if (!boton) return;

  if (boton.dataset.resultado) {
    $("#form-resultado").reset();
    aviso("#mensaje-resultado", "");
    $("#id-estudio").value = boton.dataset.resultado;
    $("#fecha-resultado").value = hoyParaInput();
    abrirModal("modal-resultado");
  }

  if (boton.dataset.descargarEstudio) {
    try {
      const datos = await api.get(`/estudios/${boton.dataset.descargarEstudio}/descargar`);
      if (datos.archivo_url) {
        window.open(datos.archivo_url, "_blank", "noopener");
      } else {
        toast(datos.mensaje ?? "El estudio no tiene archivo asociado.", "info");
      }
    } catch (error) {
      toast(error.message, "error");
    }
  }
}

async function guardarResultado(evento) {
  evento.preventDefault();
  aviso("#mensaje-resultado", "");

  const datos = valoresFormulario($("#form-resultado"));

  try {
    await api.put(`/estudios/${datos.idEstudio}/resultados`, {
      resultado: datos.resultado,
      archivoUrl: opcional(datos.archivoUrl),
      estado: datos.estado,
      fechaResultado: fechaParaApi(datos.fechaResultado),
    });
    toast("Resultado cargado.");
    cerrarModal("modal-resultado");
    await cargarEstudios();
  } catch (error) {
    aviso("#mensaje-resultado", error.message);
  }
}
