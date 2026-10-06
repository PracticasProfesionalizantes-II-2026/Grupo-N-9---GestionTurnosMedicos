using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Filters;
using ChronoSaludWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    // Traduce los errores de la API a redirects: 401 al login,
    // el resto a TempData para que el layout muestre el mensaje.
    opciones.Filters.Add<ApiExceptionFilter>();

    // Todo POST valida el token antifalsificación, lleve o no el atributo
    // [ValidateAntiForgeryToken]: una acción nueva queda cubierta sola.
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Los services necesitan el HttpContext para leer y escribir la sesión.
builder.Services.AddHttpContextAccessor();

// Sesión del servidor: acá vive el JWT, así nunca llega al navegador.
// El almacenamiento en memoria alcanza para el TP; con varias instancias
// habría que reemplazarlo por un cache distribuido de verdad.
builder.Services.AddDistributedMemoryCache();

// Fuera de Development la cookie de sesión viaja solo por HTTPS. En Development
// acompaña al pedido, porque levantar.ps1 sirve el front por http://localhost.
var cookieSoloHttps = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.AddSession(opciones =>
{
    opciones.Cookie.Name = "ChronoSalud.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = cookieSoloHttps;
    // Lax: la cookie no viaja en un POST que venga de otro sitio.
    opciones.Cookie.SameSite = SameSiteMode.Lax;
    opciones.Cookie.IsEssential = true;
    // El token de la API dura 8 horas: no tiene sentido que la sesión dure más.
    opciones.IdleTimeout = TimeSpan.FromHours(8);
});

// Cookie del token antifalsificación. Va con SameAsRequest y no con Always:
// con Always el framework rechaza toda página con formulario si no ve el
// pedido como HTTPS, y SameAsRequest ya la marca Secure cuando llega por HTTPS.
builder.Services.AddAntiforgery(opciones =>
{
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
});

// Cliente tipado contra la API. La URL sale de appsettings.json.
var urlApi = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta la clave \"Api:BaseUrl\" en appsettings.json.");

builder.Services.AddHttpClient<ApiClient>(cliente =>
{
    cliente.BaseAddress = new Uri(urlApi.TrimEnd('/') + "/");
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TurnoService>();
builder.Services.AddScoped<PacienteService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<RecetaService>();
builder.Services.AddScoped<MedicamentoService>();
builder.Services.AddScoped<HistorialService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<CoberturaService>();
builder.Services.AddScoped<UsuarioService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
