namespace ChronoSaludWeb.Services;

/// <summary>
/// Escritura del horario semanal de un doctor. La lectura sigue en
/// DoctorService (ObtenerHorariosAsync), que es lo que usan las pantallas que
/// solo lo muestran.
/// </summary>
public class HorarioService
{
    private readonly ApiClient _api;

    public HorarioService(ApiClient api) => _api = api;

    /// <summary>
    /// Único punto de la Web que escribe un horario: todo guardado pasa por acá.
    /// PUT /doctores/{id}/horarios reemplaza la semana entera por la lista que
    /// recibe: un día que no viene deja de atenderse, y la lista vacía deja al
    /// doctor sin horario. Reservado a administrador.
    /// Deja pasar la ApiException (400 si el doctor está inactivo o un rango es
    /// inválido, 404 si no existe) para que el controlador muestre el mensaje
    /// sin perder lo que se había cargado.
    /// </summary>
    public Task GuardarAsync(int idDoctor, IReadOnlyList<HorarioLaboral> nuevo)
        => _api.PutAsync($"/doctores/{idDoctor}/horarios", new { horarios = nuevo });
}
