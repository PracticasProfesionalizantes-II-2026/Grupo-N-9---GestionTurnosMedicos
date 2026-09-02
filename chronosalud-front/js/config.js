// URL base de la API. Cambiar si la API corre en otro puerto o servidor.
export const API_URL = "http://localhost:5001";

export const ROLES = {
  ADMIN: "administrador",
  DOCTOR: "doctor",
  PACIENTE: "paciente",
  SECRETARIO: "secretario",
};

export const ESTADOS_TURNO = ["pendiente", "confirmado", "completado", "cancelado"];

export const TIPOS_ESTUDIO = ["sangre", "imagen", "biopsia", "otro"];
