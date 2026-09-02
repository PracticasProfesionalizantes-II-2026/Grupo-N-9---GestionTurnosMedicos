import { API_URL } from "./config.js";
import { obtenerSesion, cerrarSesion } from "./sesion.js";

export class ApiError extends Error {
  constructor(mensaje, status) {
    super(mensaje);
    this.status = status;
  }
}

function construirUrl(ruta, params) {
  const url = new URL(API_URL + ruta);
  if (params) {
    for (const [clave, valor] of Object.entries(params)) {
      if (valor !== null && valor !== undefined && valor !== "") {
        url.searchParams.set(clave, valor);
      }
    }
  }
  return url.toString();
}

async function pedir(metodo, ruta, { body, params, anonimo = false } = {}) {
  const cabeceras = {};
  if (body !== undefined) cabeceras["Content-Type"] = "application/json";

  if (!anonimo) {
    const sesion = obtenerSesion();
    if (sesion?.token) cabeceras["Authorization"] = `Bearer ${sesion.token}`;
  }

  let respuesta;
  try {
    respuesta = await fetch(construirUrl(ruta, params), {
      method: metodo,
      headers: cabeceras,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError("No se pudo conectar con la API. ¿Está levantada en " + API_URL + "?", 0);
  }

  // El token venció o no es válido: se vuelve al login.
  if (respuesta.status === 401 && !anonimo) {
    cerrarSesion();
    throw new ApiError("Sesión expirada. Volvé a iniciar sesión.", 401);
  }

  if (respuesta.status === 204) return null;

  const texto = await respuesta.text();
  const datos = texto ? JSON.parse(texto) : null;

  if (!respuesta.ok) {
    throw new ApiError(datos?.error ?? `Error ${respuesta.status}`, respuesta.status);
  }

  return datos;
}

export const api = {
  get: (ruta, params, opciones) => pedir("GET", ruta, { params, ...opciones }),
  post: (ruta, body, opciones) => pedir("POST", ruta, { body, ...opciones }),
  put: (ruta, body, opciones) => pedir("PUT", ruta, { body, ...opciones }),
  patch: (ruta, body, opciones) => pedir("PATCH", ruta, { body, ...opciones }),
  del: (ruta, opciones) => pedir("DELETE", ruta, opciones),
};
