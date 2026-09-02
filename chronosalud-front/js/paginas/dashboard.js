import { api } from "../api.js";
import { ROLES } from "../config.js";
import { requerirSesion } from "../sesion.js";
import { montarLayout } from "../layout.js";
import { idPacienteActual } from "../perfil.js";
import { $, aviso, badge, escapar, filaMensaje, formatearFecha, fechaParaApi, hoyParaInput } from "../ui.js";

const sesion = requerirSesion();
if (sesion) {
  montarLayout("dashboard");
  iniciar();
}

const BAJADA = {
  [ROLES.PACIENTE]: "Tus próximos turnos y tu actividad reciente.",
  [ROLES.DOCTOR]: "Agenda y actividad de la clínica.",
  [ROLES.ADMIN]: "Resumen general del sistema.",
  [ROLES.SECRETARIO]: "Agenda de turnos de la clínica.",
};

const ACCESOS = {
  [ROLES.PACIENTE]: [
    { texto: "Sacar turno", href: "turnos.html?nuevo=1", estilo: "boton" },
    { texto: "Mi perfil", href: "paciente.html", estilo: "boton boton--secundario" },
  ],
  [ROLES.DOCTOR]: [
    { texto: "Nuevo turno", href: "turnos.html?nuevo=1", estilo: "boton" },
    { texto: "Pacientes", href: "pacientes.html", estilo: "boton boton--secundario" },
  ],
  [ROLES.ADMIN]: [
    { texto: "Nuevo turno", href: "turnos.html?nuevo=1", estilo: "boton" },
    { texto: "Doctores", href: "doctores.html", estilo: "boton boton--secundario" },
    { texto: "Reportes", href: "reportes.html", estilo: "boton boton--secundario" },
  ],
  [ROLES.SECRETARIO]: [{ texto: "Nuevo turno", href: "turnos.html?nuevo=1", estilo: "boton" }],
};

async function iniciar() {
  $("#saludo").textContent = `Hola, ${sesion.nombre}`;
  $("#bajada").textContent = BAJADA[sesion.rol] ?? "Resumen de actividad";
  $("#accesos-rapidos").innerHTML = (ACCESOS[sesion.rol] ?? [])
    .map((a) => `<a href="${a.href}" class="${a.estilo}">${escapar(a.texto)}</a>`)
    .join("");

  const filtros = {};
  if (sesion.rol === ROLES.PACIENTE) {
    const idPaciente = await idPacienteActual();
    if (!idPaciente) {
      aviso(
        "#mensaje",
        "Tu usuario todavía no tiene ficha de paciente asociada. Pedile a un administrador que la cargue.",
        "info"
      );
      $("#tabla-proximos").innerHTML = filaMensaje(4, "Sin turnos para mostrar.");
      $("#metricas").innerHTML = "";
      return;
    }
    filtros.paciente_id = idPaciente;
  }

  try {
    const desdeHoy = await api.get("/turnos", {
      ...filtros,
      fecha_desde: fechaParaApi(hoyParaInput()),
      limite: 200,
    });
    const historico = await api.get("/turnos", { ...filtros, limite: 1000 });

    pintarMetricas(historico.turnos ?? [], desdeHoy.turnos ?? []);
    pintarProximos(desdeHoy.turnos ?? []);
  } catch (error) {
    aviso("#mensaje", error.message);
    $("#tabla-proximos").innerHTML = filaMensaje(4, "No se pudieron cargar los turnos.");
  }
}

function pintarMetricas(todos, proximos) {
  const contar = (estado) => todos.filter((t) => t.estado === estado).length;
  const metricas = [
    { etiqueta: "Turnos próximos", valor: proximos.length, color: "azul" },
    { etiqueta: "Pendientes", valor: contar("pendiente"), color: "ambar" },
    { etiqueta: "Completados", valor: contar("completado"), color: "verde" },
    { etiqueta: "Cancelados", valor: contar("cancelado"), color: "rojo" },
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

function pintarProximos(turnos) {
  const cuerpo = $("#tabla-proximos");
  const ordenados = [...turnos]
    .filter((t) => t.estado !== "cancelado")
    .sort((a, b) => new Date(a.fechaInicio) - new Date(b.fechaInicio))
    .slice(0, 6);

  if (!ordenados.length) {
    cuerpo.innerHTML = filaMensaje(4, "No hay turnos programados.");
    return;
  }

  cuerpo.innerHTML = ordenados
    .map(
      (t) => `
      <tr>
        <td>${formatearFecha(t.fechaInicio)}</td>
        <td>${escapar(t.paciente)}</td>
        <td>${escapar(t.doctor)}</td>
        <td>${badge(t.estado)}</td>
      </tr>`
    )
    .join("");
}
