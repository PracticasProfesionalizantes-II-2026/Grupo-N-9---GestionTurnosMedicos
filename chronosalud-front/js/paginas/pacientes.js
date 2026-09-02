import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { $, aviso, escapar, filaMensaje, valoresFormulario } from "../ui.js";

const sesion = requerirSesion([ROLES.ADMIN, ROLES.DOCTOR]);

if (sesion) {
  montarLayout("pacientes");
  iniciar();
}

async function iniciar() {
  await cargarCoberturas();
  await cargarPacientes();

  $("#form-filtros").addEventListener("submit", (evento) => {
    evento.preventDefault();
    cargarPacientes();
  });

  $("#btn-limpiar").addEventListener("click", () => {
    $("#form-filtros").reset();
    cargarPacientes();
  });
}

async function cargarCoberturas() {
  try {
    const datos = await api.get("/coberturas");
    $("#filtro-cobertura").innerHTML =
      `<option value="">Todas</option>` +
      (datos.coberturas ?? [])
        .map((c) => `<option value="${c.idCobertura}">${escapar(c.nombre)}</option>`)
        .join("");
  } catch {
    // Sin coberturas cargadas el filtro queda sólo con "Todas".
  }
}

async function cargarPacientes() {
  const cuerpo = $("#tabla-pacientes");
  cuerpo.innerHTML = filaMensaje(4, "Cargando…");

  const filtros = valoresFormulario($("#form-filtros"));

  try {
    const datos = await api.get("/pacientes", { ...filtros, limite: 500 });
    const pacientes = datos.pacientes ?? [];
    $("#resumen").textContent = `${datos.total ?? pacientes.length} paciente(s)`;

    if (!pacientes.length) {
      cuerpo.innerHTML = filaMensaje(4, "No se encontraron pacientes.");
      return;
    }

    cuerpo.innerHTML = pacientes
      .map(
        (p) => `
        <tr>
          <td>${p.idPaciente}</td>
          <td>${escapar(p.apellido)}</td>
          <td>${escapar(p.nombre)}</td>
          <td>
            <a class="boton boton--secundario boton--chico" href="paciente.html?id=${p.idPaciente}">
              Ver ficha
            </a>
          </td>
        </tr>`
      )
      .join("");
  } catch (error) {
    aviso("#mensaje", error.message);
    cuerpo.innerHTML = filaMensaje(4, "No se pudieron cargar los pacientes.");
  }
}
