import { ROLES } from "./config.js";
import { api } from "./api.js";
import { obtenerSesion, cerrarSesion } from "./sesion.js";
import { $, escapar } from "./ui.js";

const MENU = [
  { id: "dashboard", texto: "Inicio", href: "dashboard.html", roles: null },
  { id: "turnos", texto: "Turnos", href: "turnos.html", roles: null },
  {
    id: "pacientes",
    texto: "Pacientes",
    href: "pacientes.html",
    roles: [ROLES.ADMIN, ROLES.DOCTOR],
  },
  { id: "mi-perfil", texto: "Mi perfil", href: "paciente.html", roles: [ROLES.PACIENTE] },
  { id: "doctores", texto: "Doctores", href: "doctores.html", roles: null },
  {
    id: "medicamentos",
    texto: "Medicamentos",
    href: "medicamentos.html",
    roles: [ROLES.ADMIN, ROLES.DOCTOR],
  },
  {
    id: "reportes",
    texto: "Reportes",
    href: "reportes.html",
    roles: [ROLES.ADMIN, ROLES.DOCTOR],
  },
  { id: "notificaciones", texto: "Notificaciones", href: "notificaciones.html", roles: null },
  { id: "cuenta", texto: "Mi cuenta", href: "cuenta.html", roles: null },
];

export function menuVisible(rol) {
  return MENU.filter((item) => !item.roles || item.roles.includes(rol));
}

export function montarLayout(paginaActiva) {
  const sesion = obtenerSesion();
  const contenedor = $("#encabezado");
  if (!sesion || !contenedor) return sesion;

  const enlaces = menuVisible(sesion.rol)
    .map(
      (item) =>
        `<a href="${item.href}" class="nav__enlace ${
          item.id === paginaActiva ? "nav__enlace--activo" : ""
        }" data-menu="${item.id}">${escapar(item.texto)}</a>`
    )
    .join("");

  contenedor.innerHTML = `
    <header class="cabecera">
      <a href="dashboard.html" class="marca">
        <span class="marca__icono">✚</span>
        <span class="marca__texto">Chrono<strong>Salud</strong></span>
      </a>
      <nav class="nav">${enlaces}</nav>
      <div class="sesion">
        <div class="sesion__datos">
          <span class="sesion__nombre">${escapar(sesion.nombre)}</span>
          <span class="sesion__rol">${escapar(sesion.rol)}</span>
        </div>
        <button type="button" class="boton boton--fantasma" id="btn-salir">Salir</button>
      </div>
    </header>`;

  $("#btn-salir").addEventListener("click", cerrarSesion);
  mostrarNoLeidas(sesion.idUsuario);
  return sesion;
}

async function mostrarNoLeidas(idUsuario) {
  try {
    const datos = await api.get(`/usuarios/${idUsuario}/notificaciones`, { leida: false });
    const total = datos?.total ?? 0;
    if (total > 0) {
      const enlace = $('[data-menu="notificaciones"]');
      if (enlace) enlace.innerHTML += ` <span class="contador">${total}</span>`;
    }
  } catch {
    // Un fallo al contar notificaciones no debe romper la navegación.
  }
}
