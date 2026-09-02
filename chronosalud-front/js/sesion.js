const CLAVE = "chronosalud.sesion";

export function guardarSesion(datos) {
  localStorage.setItem(CLAVE, JSON.stringify(datos));
}

export function obtenerSesion() {
  try {
    return JSON.parse(localStorage.getItem(CLAVE));
  } catch {
    return null;
  }
}

export function cerrarSesion() {
  localStorage.removeItem(CLAVE);
  window.location.href = "index.html";
}

export function tieneRol(...roles) {
  const sesion = obtenerSesion();
  return !!sesion && roles.includes(sesion.rol);
}

// Corta la carga de la página si no hay sesión o el rol no está habilitado.
export function requerirSesion(rolesPermitidos = null) {
  const sesion = obtenerSesion();
  if (!sesion) {
    window.location.href = "index.html";
    return null;
  }
  if (rolesPermitidos && !rolesPermitidos.includes(sesion.rol)) {
    window.location.href = "dashboard.html";
    return null;
  }
  return sesion;
}
