using ChronoSaludWeb.Filters;
using ChronoSaludWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    // Traduce los errores de la API a redirects: 401 al login,
    // el resto a TempData para que el layout muestre el mensaje.
    opciones.Filters.Add<ApiExceptionFilter>();
});

// Los services necesitan el HttpContext para leer y escribir la sesión.
builder.Services.AddHttpContextAccessor();

// Sesión del servidor: acá vive el JWT, así nunca llega al navegador.
// El almacenamiento en memoria alcanza para el TP; con varias instancias
// habría que reemplazarlo por un cache distribuido de verdad.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.Cookie.Name = "ChronoSalud.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    // El token de la API dura 8 horas: no tiene sentido que la sesión dure más.
    opciones.IdleTimeout = TimeSpan.FromHours(8);
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
