export const $ = (selector, raiz = document) => raiz.querySelector(selector);
export const $$ = (selector, raiz = document) => [...raiz.querySelectorAll(selector)];

export function escapar(valor) {
  if (valor === null || valor === undefined) return "";
  return String(valor)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

export function formatearFecha(valor) {
  if (!valor) return "—";
  const fecha = new Date(valor);
  if (Number.isNaN(fecha.getTime())) return "—";
  return fecha.toLocaleDateString("es-AR", { day: "2-digit", month: "2-digit", year: "numeric" });
}

// La API espera DateTime; se manda la fecha del input date como medianoche local.
export function fechaParaApi(valorInput) {
  return valorInput ? `${valorInput}T00:00:00` : null;
}

export function fechaParaInput(valor) {
  if (!valor) return "";
  const fecha = new Date(valor);
  if (Number.isNaN(fecha.getTime())) return "";
  const mes = String(fecha.getMonth() + 1).padStart(2, "0");
  const dia = String(fecha.getDate()).padStart(2, "0");
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}

export function hoyParaInput() {
  return fechaParaInput(new Date());
}

export function badge(estado) {
  const texto = escapar(estado ?? "—");
  return `<span class="badge badge--${escapar(estado ?? "")}">${texto}</span>`;
}

export function aviso(contenedor, mensaje, tipo = "error") {
  const elemento = typeof contenedor === "string" ? $(contenedor) : contenedor;
  if (!elemento) return;
  if (!mensaje) {
    elemento.innerHTML = "";
    return;
  }
  elemento.innerHTML = `<div class="aviso aviso--${tipo}">${escapar(mensaje)}</div>`;
}

let temporizadorToast;
export function toast(mensaje, tipo = "exito") {
  let contenedor = $("#toast");
  if (!contenedor) {
    contenedor = document.createElement("div");
    contenedor.id = "toast";
    document.body.appendChild(contenedor);
  }
  contenedor.className = `toast toast--${tipo} toast--visible`;
  contenedor.textContent = mensaje;
  clearTimeout(temporizadorToast);
  temporizadorToast = setTimeout(() => contenedor.classList.remove("toast--visible"), 3500);
}

export function filaMensaje(columnas, mensaje) {
  return `<tr><td colspan="${columnas}" class="tabla__vacia">${escapar(mensaje)}</td></tr>`;
}

export function abrirModal(id) {
  $(`#${id}`)?.classList.add("modal--abierto");
}

export function cerrarModal(id) {
  $(`#${id}`)?.classList.remove("modal--abierto");
}

// Cierra cualquier modal al tocar el fondo o los botones con data-cerrar.
export function activarCierreModales() {
  $$(".modal").forEach((modal) => {
    modal.addEventListener("click", (evento) => {
      if (evento.target === modal || evento.target.dataset.cerrar !== undefined) {
        modal.classList.remove("modal--abierto");
      }
    });
  });
  document.addEventListener("keydown", (evento) => {
    if (evento.key === "Escape") $$(".modal--abierto").forEach((m) => m.classList.remove("modal--abierto"));
  });
}

export function valoresFormulario(formulario) {
  const datos = {};
  for (const [clave, valor] of new FormData(formulario).entries()) {
    datos[clave] = typeof valor === "string" ? valor.trim() : valor;
  }
  return datos;
}

// Convierte "" en null para que la API no pise campos con vacío.
export function opcional(valor) {
  return valor === "" || valor === undefined ? null : valor;
}
