import { api } from "./api.js";
import { ROLES } from "./config.js";
import { obtenerSesion } from "./sesion.js";

let cachePaciente = null;
let cacheDoctor = null;

// Devuelve el id_paciente del usuario logueado (null si su rol no es paciente).
export async function idPacienteActual() {
  const sesion = obtenerSesion();
  if (!sesion || sesion.rol !== ROLES.PACIENTE) return null;
  if (cachePaciente?.idUsuario === sesion.idUsuario) return cachePaciente.id;

  try {
    const paciente = await api.get("/pacientes/me");
    cachePaciente = { idUsuario: sesion.idUsuario, id: paciente.idPaciente };
    return paciente.idPaciente;
  } catch {
    return null;
  }
}

// Devuelve el id_doctor del usuario logueado (null si no tiene ficha de doctor).
export async function idDoctorActual() {
  const sesion = obtenerSesion();
  if (!sesion || sesion.rol !== ROLES.DOCTOR) return null;
  if (cacheDoctor?.idUsuario === sesion.idUsuario) return cacheDoctor.id;

  try {
    const doctor = await api.get("/doctores/me");
    cacheDoctor = { idUsuario: sesion.idUsuario, id: doctor.idDoctor };
    return doctor.idDoctor;
  } catch {
    return null;
  }
}
