using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class RecetasController : ControladorBase
{
    private const string AvisoSinPerfil =
        "Tu usuario no tiene un perfil de paciente asociado, así que no podemos " +
        "mostrar tus recetas. Pedile a la administración que lo cree.";

    private const int LargoMaximoDeGenerico = 40;

    private readonly RecetaService _recetas;
    private readonly MedicamentoService _medicamentos;
    private readonly PacienteService _pacientes;
    private readonly TurnoService _turnos;
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;

    public RecetasController(
        RecetaService recetas,
        MedicamentoService medicamentos,
        PacienteService pacientes,
        TurnoService turnos,
        AuthService auth,
        PerfilService perfil)
    {
        _recetas = recetas;
        _medicamentos = medicamentos;
        _pacientes = pacientes;
        _turnos = turnos;
        _auth = auth;
        _perfil = perfil;
    }

    public async Task<IActionResult> Index(int? paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index), new { paciente }));

        try
        {
            var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);

            if (aviso is not null)
                return View(new RecetasIndexViewModel { Aviso = aviso, PuedeCrear = _auth.PuedeEmitirRecetas });

            // Doctor y administrador todavía no eligieron a quién mirarle las recetas.
            if (idPaciente is null)
            {
                return View(new RecetasIndexViewModel
                {
                    PuedeElegirPaciente = true,
                    Pacientes = await OpcionesDePacienteAsync(null),
                    PuedeCrear = _auth.PuedeEmitirRecetas
                });
            }

            var recetas = await _recetas.ObtenerDePacienteAsync(idPaciente.Value);

            return View(new RecetasIndexViewModel
            {
                IdPaciente = idPaciente,
                NombrePaciente = await NombreDePacienteAsync(idPaciente.Value),
                PuedeElegirPaciente = _auth.PuedeElegirPaciente,
                Pacientes = _auth.PuedeElegirPaciente
                    ? await OpcionesDePacienteAsync(idPaciente)
                    : Array.Empty<SelectListItem>(),
                PuedeCrear = _auth.PuedeEmitirRecetas,
                Recetas = recetas.Select(RecetaFilaViewModel.Desde)
                                 .OrderByDescending(r => r.Fecha)
                                 .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new RecetasIndexViewModel { Error = error.Message });
        }
    }

    /// <summary>
    /// El detalle necesita saber de quién es la receta: ver BuscarRecetaAsync.
    /// </summary>
    public async Task<IActionResult> Detalle(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id, paciente }));

        try
        {
            var (idPaciente, receta) = await BuscarRecetaAsync(id, paciente);

            // No existe, o es de otro paciente: se responde lo mismo.
            if (idPaciente is null || receta is null)
                return NoEncontrado(id, "Receta no encontrada", "receta");

            var fila = RecetaFilaViewModel.Desde(receta);
            var idDoctorPropio = await IdDoctorPropioAsync();
            var esSuya = idDoctorPropio is not null && fila.Firma?.IdDoctor == idDoctorPropio;

            return View(new RecetaDetalleViewModel
            {
                Receta = fila,
                IdPaciente = idPaciente.Value,
                NombrePaciente = await NombreDePacienteAsync(idPaciente.Value),
                PuedeEditar = esSuya,
                // El paciente y la administración ven el turno; un doctor, solo los de su agenda.
                EnlaceAlTurno = !_auth.EsDoctor || esSuya
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new RecetaDetalleViewModel { IdPaciente = paciente, Error = error.Message });
        }
    }

    /// <summary>
    /// La hoja de la receta para imprimir. Mismo permiso que el detalle: el
    /// paciente solo ve las suyas. Sin JavaScript: se imprime con el menú del
    /// navegador, y los estilos @media print dejan solo la hoja.
    /// </summary>
    public async Task<IActionResult> Imprimir(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Imprimir), new { id, paciente }));

        try
        {
            var (idPaciente, receta) = await BuscarRecetaAsync(id, paciente);

            if (idPaciente is null || receta is null)
                return NoEncontrado(id, "Receta no encontrada", "receta");

            var ficha = await FichaParaImprimirAsync(idPaciente.Value);

            return View(new RecetaImprimirViewModel
            {
                Receta = RecetaFilaViewModel.Desde(receta),
                IdPaciente = idPaciente.Value,
                NombrePaciente = ficha is null ? null : $"{ficha.Nombre} {ficha.Apellido}".Trim(),
                TipoDocumento = ficha?.TipoDocumento,
                Dni = ficha?.Dni,
                GeneradaEl = FechaArgentina.Ahora()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new RecetaImprimirViewModel { IdPaciente = paciente, Error = error.Message });
        }
    }

    /// <summary>
    /// Nueva receta. Con <paramref name="turno"/> es la receta de ese turno:
    /// el paciente sale del turno y la receta queda vinculada.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crear(int? paciente, int? turno)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear), new { paciente, turno }));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        var modelo = new RecetaCrearViewModel
        {
            IdPaciente = paciente,
            Fecha = FechaArgentina.Hoy(),
            Vigencia = FechaArgentina.Hoy().AddDays(30)
        };

        if (turno is not null)
        {
            try
            {
                var atendido = await TurnoParaAtenderAsync(turno.Value);
                if (atendido is null)
                    return NoEncontrado(turno.Value, "Turno no encontrado", "turno");

                if (!atendido.SePuedeAtender)
                {
                    TempData["Error"] = $"El turno #{atendido.IdTurno} está {atendido.Estado}: no se le puede emitir una receta.";
                    return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = atendido.IdTurno });
                }

                modelo.IdTurno = atendido.IdTurno;
                modelo.IdPaciente = atendido.IdPaciente;
                modelo.Turno = atendido;
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
                TempData["Error"] = error.Message;
                return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = turno });
            }
        }

        await CargarFormularioAsync(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RecetaCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        if (!ModelState.IsValid)
        {
            await CargarFormularioAsync(modelo);
            return View(modelo);
        }

        // El doctor que firma es el que está logueado; no se elige.
        var idDoctor = await _perfil.IdPerfilAsync(esDoctor: true);
        if (idDoctor is null)
        {
            ModelState.AddModelError(string.Empty,
                "Tu cuenta es de doctor pero todavía no tiene el perfil completo " +
                "(matrícula y especialidad), así que no podemos registrar quién firma la receta. " +
                "Pedile a administración que lo complete.");
            await CargarFormularioAsync(modelo);
            return View(modelo);
        }

        try
        {
            // Desde un turno, el paciente sale del turno y no del formulario.
            TurnoAtendidoViewModel? atendido = null;
            if (modelo.IdTurno is not null)
            {
                atendido = await TurnoParaAtenderAsync(modelo.IdTurno.Value);
                if (atendido is null)
                    return NoEncontrado(modelo.IdTurno.Value, "Turno no encontrado", "turno");

                modelo.IdPaciente = atendido.IdPaciente;
            }

            var medicamentos = await ArmarMedicamentosAsync(modelo);
            if (medicamentos is null)
            {
                await CargarFormularioAsync(modelo);
                return View(modelo);
            }

            var creada = await _recetas.CrearAsync(new RecetaNueva(
                modelo.IdPaciente!.Value,
                idDoctor.Value,
                modelo.IdTurno,
                modelo.Fecha!.Value,
                modelo.Vigencia!.Value,
                string.IsNullOrWhiteSpace(modelo.Detalles) ? null : modelo.Detalles.Trim(),
                medicamentos));

            TempData["Exito"] = creada is null
                ? "Receta emitida correctamente."
                : $"Receta #{creada.IdReceta} emitida correctamente.";

            // Desde un turno se vuelve al turno.
            if (atendido is not null)
                return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = atendido.IdTurno });

            return RedirectToAction(nameof(Index), new { paciente = modelo.IdPaciente });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarFormularioAsync(modelo);
            return View(modelo);
        }
    }

    /// <summary>
    /// Editar una receta. Solo la ve el doctor que la firmó: para cualquier
    /// otro, la receta "no existe".
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Editar(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, paciente }));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        try
        {
            var receta = await BuscarRecetaPropiaAsync(id, paciente);
            if (receta is null)
                return NoEncontrado(id, "Receta no encontrada", "receta");

            var modelo = RecetaCrearViewModel.DesdeReceta(receta, paciente);
            await CargarFormularioAsync(modelo);
            return View(nameof(Crear), modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Index), new { paciente });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(RecetaCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        if (modelo.IdReceta is null || modelo.IdPaciente is null)
            return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
        {
            await CargarFormularioAsync(modelo);
            return View(nameof(Crear), modelo);
        }

        try
        {
            // Se vuelve a revisar que sea suya: el formulario se puede armar a mano.
            var receta = await BuscarRecetaPropiaAsync(modelo.IdReceta.Value, modelo.IdPaciente.Value);
            if (receta is null)
                return NoEncontrado(modelo.IdReceta.Value, "Receta no encontrada", "receta");

            var medicamentos = await ArmarMedicamentosAsync(modelo);
            if (medicamentos is null)
            {
                await CargarFormularioAsync(modelo);
                return View(nameof(Crear), modelo);
            }

            // El paciente, el doctor y el turno no cambian al editar: van los
            // que ya tenía la receta.
            await _recetas.ActualizarAsync(receta.IdReceta, new RecetaNueva(
                modelo.IdPaciente.Value,
                receta.Doctor!.IdDoctor,
                receta.IdTurno,
                modelo.Fecha!.Value,
                modelo.Vigencia!.Value,
                string.IsNullOrWhiteSpace(modelo.Detalles) ? null : modelo.Detalles.Trim(),
                medicamentos));

            TempData["Exito"] = $"Receta #{receta.IdReceta} actualizada.";
            return RedirectToAction(nameof(Detalle), new { id = receta.IdReceta, paciente = modelo.IdPaciente });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarFormularioAsync(modelo);
            return View(nameof(Crear), modelo);
        }
    }

    /// <summary>
    /// Los medicamentos de las filas cargadas, como los espera la API. "Otro..."
    /// viaja como RecetaCrearViewModel.IdOtro y la API necesita el id del
    /// medicamento marcador. Devuelve null si falta el marcador (el error ya
    /// quedó en el formulario).
    /// </summary>
    private async Task<List<Services.RecetaMedicamento>?> ArmarMedicamentosAsync(RecetaCrearViewModel modelo)
    {
        int? idMarcador = null;
        if (modelo.MedicamentosCargados.Any(m => m.EsOtro))
        {
            idMarcador = await _medicamentos.ObtenerIdMarcadorOtroAsync();
            if (idMarcador is null)
            {
                ModelState.AddModelError(string.Empty,
                    "La opción \"Otro...\" no está disponible todavía: falta una configuración del sistema. Avisale a administración.");
                return null;
            }
        }

        return modelo.MedicamentosCargados
            .Select(m => new Services.RecetaMedicamento(
                m.EsOtro ? idMarcador!.Value : m.IdMedicamento!.Value,
                m.Dosis!.Trim(),
                m.Frecuencia!.Trim(),
                string.IsNullOrWhiteSpace(m.Duracion) ? null : m.Duracion.Trim(),
                m.EsOtro
                    ? IndicacionesDeOtro.Componer(m.NombreOtro, m.Indicaciones)
                    : string.IsNullOrWhiteSpace(m.Indicaciones) ? null : m.Indicaciones.Trim()))
            .ToList();
    }

    /// <summary>
    /// Botón "+" de una fila: agrega una vacía debajo y vuelve a mostrar el
    /// formulario con todo lo cargado. No valida ni emite nada.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarFila(RecetaCrearViewModel modelo, int indice)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        if (modelo.Medicamentos.Count < RecetaCrearViewModel.MaximoDeFilas)
        {
            modelo.Medicamentos.Insert(
                Math.Clamp(indice + 1, 0, modelo.Medicamentos.Count),
                new RecetaMedicamentoCampoViewModel());
        }

        return await RepintarFormularioAsync(modelo);
    }

    /// <summary>
    /// Botón "-" de una fila: la quita, salvo que sea la única.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarFila(RecetaCrearViewModel modelo, int indice)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        if (modelo.Medicamentos.Count > RecetaCrearViewModel.MinimoDeFilas &&
            indice >= 0 && indice < modelo.Medicamentos.Count)
        {
            modelo.Medicamentos.RemoveAt(indice);
        }

        return await RepintarFormularioAsync(modelo);
    }

    private async Task<IActionResult> RepintarFormularioAsync(RecetaCrearViewModel modelo)
    {
        // Los tag helpers prefieren el valor posteado al del modelo, y lo buscan
        // por índice: sin esto, al quitar una fila del medio las siguientes
        // mostrarían los datos de la fila que ocupaba antes su lugar. De paso
        // se van los errores de validación, que acá no corresponden.
        ModelState.Clear();

        await CargarFormularioAsync(modelo);
        return View(nameof(Crear), modelo);
    }

    /// <summary>
    /// Un paciente solo ve sus recetas: el id se ignora y se usa el de su perfil.
    /// Doctor y administrador pueden mirar las de cualquiera.
    /// </summary>
    private async Task<(int? idPaciente, string? aviso)> ResolverPacienteAsync(int? pedido)
    {
        if (_auth.PuedeElegirPaciente)
            return (pedido is > 0 ? pedido : null, null);

        if (_auth.SesionActual?.Rol == "paciente")
        {
            var propio = await _perfil.IdPerfilAsync(esDoctor: false);
            return propio is null ? (null, AvisoSinPerfil) : (propio, null);
        }

        return (null, "No hay recetas para mostrar con tu rol.");
    }

    private async Task<string?> NombreDePacienteAsync(int idPaciente)
    {
        // Solo doctor y administrador pueden pedir la ficha del paciente.
        if (!_auth.PuedeVerPacientes)
            return _auth.SesionActual?.Nombre;

        var paciente = await _pacientes.ObtenerPorIdAsync(idPaciente);
        return paciente is null ? null : $"{paciente.Nombre} {paciente.Apellido}".Trim();
    }

    /// <summary>
    /// La API no tiene GET /recetas/{id}: el único listado es por paciente, así
    /// que la receta sale de esa lista. Devuelve null si no hay paciente o si
    /// la receta no es de él.
    /// </summary>
    private async Task<(int? idPaciente, Receta? receta)> BuscarRecetaAsync(int id, int paciente)
    {
        var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);
        if (aviso is not null || idPaciente is null)
            return (null, null);

        var receta = (await _recetas.ObtenerDePacienteAsync(idPaciente.Value))
            .FirstOrDefault(r => r.IdReceta == id);

        return (idPaciente, receta);
    }

    /// <summary>
    /// Nombre y documento del paciente para la hoja. El personal pide la ficha
    /// por id; el paciente, la suya. Si falla, la hoja sale igual, con
    /// "Sin datos" en el paciente.
    /// </summary>
    private async Task<PacienteDetalle?> FichaParaImprimirAsync(int idPaciente)
    {
        try
        {
            return _auth.PuedeVerPacientes
                ? await _pacientes.ObtenerPorIdAsync(idPaciente)
                : await _pacientes.ObtenerMiPerfilAsync();
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return null;
        }
    }

    /// <summary>
    /// Una receta del paciente firmada por el doctor que pregunta. Null si no
    /// existe, si es de otro paciente o si la firmó otro doctor.
    /// </summary>
    private async Task<Receta?> BuscarRecetaPropiaAsync(int id, int paciente)
    {
        var idDoctorPropio = await IdDoctorPropioAsync();
        if (idDoctorPropio is null)
            return null;

        var (_, receta) = await BuscarRecetaAsync(id, paciente);
        if (receta?.Doctor is null || receta.Doctor.IdDoctor != idDoctorPropio)
            return null;

        return receta;
    }

    /// <summary>El perfil de doctor de quien está logueado, o null si no es doctor.</summary>
    private async Task<int?> IdDoctorPropioAsync() =>
        _auth.EsDoctor ? await _perfil.IdPerfilAsync(esDoctor: true) : null;

    /// <summary>
    /// El turno, si quien pregunta lo puede ver. Para un doctor, la API solo
    /// devuelve los de su agenda: uno ajeno llega como null.
    /// </summary>
    private async Task<TurnoAtendidoViewModel?> TurnoParaAtenderAsync(int idTurno)
    {
        var turno = await _turnos.ObtenerPorIdAsync(idTurno);
        return turno is null ? null : TurnoAtendidoViewModel.Desde(turno);
    }

    private async Task<IReadOnlyList<SelectListItem>> OpcionesDePacienteAsync(int? seleccionado)
    {
        var pacientes = await _pacientes.ObtenerTodosAsync();
        return pacientes
            .Select(p => new SelectListItem(
                $"{p.Nombre} {p.Apellido}".Trim(),
                p.IdPaciente.ToString(),
                p.IdPaciente == seleccionado))
            .ToList();
    }

    private async Task CargarFormularioAsync(RecetaCrearViewModel modelo)
    {
        while (modelo.Medicamentos.Count < RecetaCrearViewModel.MinimoDeFilas)
            modelo.Medicamentos.Add(new RecetaMedicamentoCampoViewModel());

        if (modelo.Medicamentos.Count > RecetaCrearViewModel.MaximoDeFilas)
        {
            modelo.Medicamentos.RemoveRange(
                RecetaCrearViewModel.MaximoDeFilas,
                modelo.Medicamentos.Count - RecetaCrearViewModel.MaximoDeFilas);
        }

        try
        {
            // Si el paciente ya está decidido (desde un turno o al editar) se
            // muestra su nombre en vez de la lista.
            if (!modelo.PacienteFijo)
                modelo.Pacientes = await OpcionesDePacienteAsync(modelo.IdPaciente);
            else if (modelo.IdPaciente is not null)
                modelo.NombrePaciente = await NombreDePacienteAsync(modelo.IdPaciente.Value);

            if (modelo.IdTurno is not null && modelo.Turno is null)
                modelo.Turno = await TurnoParaAtenderAsync(modelo.IdTurno.Value);

            // El marcador de "Otro..." no es un medicamento elegible: sale de la
            // lista y, si existe, habilita la opción propia del desplegable.
            var vademecum = await _medicamentos.ObtenerTodosAsync();
            modelo.OtroDisponible = vademecum.Any(MedicamentoService.EsMarcadorOtro);
            modelo.Vademecum = OpcionesDeVademecum(
                vademecum.Where(m => !MedicamentoService.EsMarcadorOtro(m)).ToList());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Las listas quedan vacías, pero no se pierde lo que el usuario cargó.
            ModelState.AddModelError(string.Empty, $"No se pudieron cargar las listas: {error.Message}");
        }
    }

    /// <summary>
    /// Opciones del desplegable de medicamentos. Si dos quedan con la misma
    /// etiqueta, a esas se les agrega la forma farmacéutica para distinguirlas.
    /// </summary>
    private static IReadOnlyList<SelectListItem> OpcionesDeVademecum(IReadOnlyList<Medicamento> vademecum)
    {
        var opciones = vademecum
            .OrderBy(m => m.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(m => m.Concentracion, StringComparer.CurrentCultureIgnoreCase)
            .Select(m => (Medicamento: m, Etiqueta: EtiquetaDeMedicamento(m)))
            .ToList();

        var repetidas = opciones
            .GroupBy(o => o.Etiqueta, StringComparer.CurrentCultureIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        return opciones
            .Select(o => new SelectListItem(
                repetidas.Contains(o.Etiqueta) && !string.IsNullOrWhiteSpace(o.Medicamento.FormaFarmaceutica)
                    ? $"{o.Etiqueta} - {o.Medicamento.FormaFarmaceutica.Trim().ToLower(TurnosIndexViewModel.Cultura)}"
                    : o.Etiqueta,
                o.Medicamento.IdMedicamento.ToString()))
            .ToList();
    }

    /// <summary>
    /// "NOMBRE COMERCIAL (nombre genérico) concentración". El genérico se corta
    /// porque las combinaciones de drogas no entran en el desplegable; lo que
    /// falte (medicamentos cargados a mano) se omite.
    /// </summary>
    private static string EtiquetaDeMedicamento(Medicamento m)
    {
        var etiqueta = m.Nombre.Trim();

        if (!string.IsNullOrWhiteSpace(m.NombreGenerico))
        {
            var generico = m.NombreGenerico.Trim().ToLower(TurnosIndexViewModel.Cultura);
            if (generico.Length > LargoMaximoDeGenerico)
                generico = generico[..LargoMaximoDeGenerico].TrimEnd() + "…";

            etiqueta += $" ({generico})";
        }

        if (!string.IsNullOrWhiteSpace(m.Concentracion))
            etiqueta += $" {m.Concentracion.Trim()}";

        return etiqueta;
    }

    private IActionResult SinPermisoDeEmision() => SinPermiso(
        "No podés emitir recetas con tu rol",
        "Solo los doctores pueden emitir recetas.");
}
