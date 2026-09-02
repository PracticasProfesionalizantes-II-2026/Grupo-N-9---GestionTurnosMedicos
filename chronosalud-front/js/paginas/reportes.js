import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { $, aviso, escapar, fechaParaApi, fechaParaInput, hoyParaInput, valoresFormulario } from "../ui.js";

const sesion = requerirSesion([ROLES.ADMIN, ROLES.DOCTOR]);

if (sesion) {
  montarLayout("reportes");
  iniciar();
}

function iniciar() {
  const hoy = new Date();
  const haceUnMes = new Date();
  haceUnMes.setMonth(haceUnMes.getMonth() - 1);

  $("#desde").value = fechaParaInput(haceUnMes);
  $("#hasta").value = fechaParaInput(hoy);

  cargarDoctores();
  generar();

  $("#form-reporte").addEventListener("submit", (evento) => {
    evento.preventDefault();
    generar();
  });
}

async function cargarDoctores() {
  try {
    const datos = await api.get("/doctores", { limite: 200 });
    $("#doctor").innerHTML =
      `<option value="">Todos</option>` +
      (datos.doctores ?? [])
        .map((d) => `<option value="${d.idDoctor}">${escapar(d.nombre)}</option>`)
        .join("");
  } catch {
    // Sin doctores el filtro queda sólo con "Todos".
  }
}

async function generar() {
  aviso("#mensaje", "");
  const datos = valoresFormulario($("#form-reporte"));

  if (!datos.fecha_desde || !datos.fecha_hasta) {
    aviso("#mensaje", "Elegí un rango de fechas.");
    return;
  }

  try {
    const reporte = await api.get("/reportes/turnos", {
      fecha_desde: fechaParaApi(datos.fecha_desde),
      fecha_hasta: fechaParaApi(datos.fecha_hasta),
      doctor_id: datos.doctor_id,
    });

    pintarMetricas(reporte);
    pintarDistribucion(reporte);
  } catch (error) {
    aviso("#mensaje", error.message);
    $("#metricas").innerHTML = "";
    $("#tarjeta-distribucion").hidden = true;
  }
}

function pintarMetricas(reporte) {
  const metricas = [
    { etiqueta: "Total de turnos", valor: reporte.total_turnos ?? 0, color: "azul" },
    { etiqueta: "Completados", valor: reporte.completados ?? 0, color: "verde" },
    { etiqueta: "Pendientes", valor: reporte.pendientes ?? 0, color: "ambar" },
    { etiqueta: "Cancelados", valor: reporte.cancelados ?? 0, color: "rojo" },
  ];

  $("#metricas").innerHTML = metricas
    .map(
      (m) => `
      <article class="metrica metrica--${m.color}">
        <div class="metrica__etiqueta">${escapar(m.etiqueta)}</div>
        <div class="metrica__valor">${m.valor}</div>
      </article>`
    )
    .join("");
}

function pintarDistribucion(reporte) {
  const total = reporte.total_turnos ?? 0;
  $("#tarjeta-distribucion").hidden = total === 0;
  if (!total) return;

  const filas = [
    { etiqueta: "Completados", valor: reporte.completados ?? 0, color: "var(--verde)" },
    { etiqueta: "Pendientes", valor: reporte.pendientes ?? 0, color: "var(--ambar)" },
    { etiqueta: "Cancelados", valor: reporte.cancelados ?? 0, color: "var(--rojo)" },
  ];

  $("#distribucion").innerHTML = filas
    .map((f) => {
      const porcentaje = Math.round((f.valor / total) * 100);
      return `
        <div style="margin-bottom:14px">
          <div style="display:flex;justify-content:space-between;font-size:14px;margin-bottom:4px">
            <span>${escapar(f.etiqueta)}</span>
            <span class="texto-suave">${f.valor} (${porcentaje}%)</span>
          </div>
          <div style="height:10px;background:var(--gris-100);border-radius:999px;overflow:hidden">
            <div style="width:${porcentaje}%;height:100%;background:${f.color}"></div>
          </div>
        </div>`;
    })
    .join("");
}
