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
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;

    public RecetasController(
        RecetaService recetas,
        MedicamentoService medicamentos,
        PacienteService pacientes,
        AuthService auth,
        PerfilService perfil)
    {
        _recetas = recetas;
        _medicamentos = medicamentos;
        _pacientes = pacientes;
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

            // RecetaMedicamentoDto solo trae el id: los nombres se resuelven acá,
            // con un solo pedido para toda la pantalla.
            var vademecum = recetas.Any(r => r.Medicamentos.Count > 0)
                ? await _medicamentos.ObtenerPorIdAsync()
                : new Dictionary<int, Medicamento>();

            return View(new RecetasIndexViewModel
            {
                IdPaciente = idPaciente,
                NombrePaciente = await NombreDePacienteAsync(idPaciente.Value),
                PuedeElegirPaciente = _auth.PuedeElegirPaciente,
                Pacientes = _auth.PuedeElegirPaciente
                    ? await OpcionesDePacienteAsync(idPaciente)
                    : Array.Empty<SelectListItem>(),
                PuedeCrear = _auth.PuedeEmitirRecetas,
                Recetas = recetas.Select(r => Mapear(r, vademecum))
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
    /// La API no tiene GET /recetas/{id}: el único listado es por paciente, así
    /// que el detalle sale de esa misma lista y necesita saber de quién es.
    /// </summary>
    public async Task<IActionResult> Detalle(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id, paciente }));

        try
        {
            var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);

            if (aviso is not null || idPaciente is null)
                return NoEncontrado(id, "Receta no encontrada", "receta");

            var receta = (await _recetas.ObtenerDePacienteAsync(idPaciente.Value))
                .FirstOrDefault(r => r.IdReceta == id);

            // No existe, o es de otro paciente: se responde lo mismo.
            if (receta is null)
                return NoEncontrado(id, "Receta no encontrada", "receta");

            var vademecum = receta.Medicamentos.Count > 0
                ? await _medicamentos.ObtenerPorIdAsync()
                : new Dictionary<int, Medicamento>();

            return View(new RecetaDetalleViewModel
            {
                Receta = Mapear(receta, vademecum),
                IdPaciente = idPaciente.Value,
                NombrePaciente = await NombreDePacienteAsync(idPaciente.Value)
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new RecetaDetalleViewModel { IdPaciente = paciente, Error = error.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int? paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear), new { paciente }));

        if (!_auth.PuedeEmitirRecetas)
            return SinPermisoDeEmision();

        var modelo = new RecetaCrearViewModel
        {
            IdPaciente = paciente,
            Fecha = DateTime.Today,
            Vigencia = DateTime.Today.AddDays(30)
        };

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
                "Tu usuario tiene rol doctor pero no tiene un perfil de doctor cargado " +
                "(matrícula, especialidad), así que la API no puede registrar quién firma la receta.");
            await CargarFormularioAsync(modelo);
            return View(modelo);
        }

        try
        {
            // "Otro..." viaja como RecetaCrearViewModel.IdOtro; la API necesita
            // el id del medicamento marcador.
            int? idMarcador = null;
            if (modelo.MedicamentosCargados.Any(m => m.EsOtro))
            {
                idMarcador = await _medicamentos.ObtenerIdMarcadorOtroAsync();
                if (idMarcador is null)
                {
                    ModelState.AddModelError(string.Empty,
                        "La opción \"Otro...\" no está disponible: falta cargar el medicamento marcador en la API.");
                    await CargarFormularioAsync(modelo);
                    return View(modelo);
                }
            }

            var creada = await _recetas.CrearAsync(new RecetaNueva(
                modelo.IdPaciente!.Value,
                idDoctor.Value,
                // La API acepta vincular la receta a un turno; todavía no lo pedimos.
                IdTurno: null,
                modelo.Fecha!.Value,
                modelo.Vigencia!.Value,
                string.IsNullOrWhiteSpace(modelo.Detalles) ? null : modelo.Detalles.Trim(),
                modelo.MedicamentosCargados
                    .Select(m => new Services.RecetaMedicamento(
                        m.EsOtro ? idMarcador!.Value : m.IdMedicamento!.Value,
                        m.Dosis!.Trim(),
                        m.Frecuencia!.Trim(),
                        string.IsNullOrWhiteSpace(m.Duracion) ? null : m.Duracion.Trim(),
                        m.EsOtro
                            ? IndicacionesDeOtro.Componer(m.NombreOtro, m.Indicaciones)
                            : string.IsNullOrWhiteSpace(m.Indicaciones) ? null : m.Indicaciones.Trim()))
                    .ToList()));

            TempData["Exito"] = creada is null
                ? "Receta emitida correctamente."
                : $"Receta #{creada.IdReceta} emitida correctamente.";

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
            modelo.Pacientes = await OpcionesDePacienteAsync(modelo.IdPaciente);

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
        "La API reserva la emisión de recetas al rol doctor.");

    private static RecetaFilaViewModel Mapear(
        Services.Receta receta,
        IReadOnlyDictionary<int, Medicamento> vademecum) => new()
    {
        IdReceta = receta.IdReceta,
        Fecha = receta.Fecha,
        Vigencia = receta.Vigencia,
        Detalles = receta.Detalles,
        Medicamentos = receta.Medicamentos
            .Select(m =>
            {
                vademecum.TryGetValue(m.IdMedicamento, out var med);

                return new RecetaMedicamentoViewModel
                {
                    IdMedicamento = m.IdMedicamento,
                    Nombre = med?.Nombre,
                    NombreGenerico = med?.NombreGenerico,
                    Concentracion = med?.Concentracion,
                    FormaFarmaceutica = med?.FormaFarmaceutica,
                    EsOtro = med is not null && MedicamentoService.EsMarcadorOtro(med),
                    Dosis = m.Dosis,
                    Frecuencia = m.Frecuencia,
                    Duracion = m.Duracion,
                    Indicaciones = m.Indicaciones
                };
            })
            .ToList()
    };
}
