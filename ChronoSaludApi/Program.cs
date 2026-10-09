using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Endpoints;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Repositorios;

var builder = WebApplication.CreateBuilder(args);

// ── 1. OpenAPI / Scalar ────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── 2. Entity Framework + SQL Server ──────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 3. Autenticación JWT ───────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── 4. Repositorios (Scoped) ───────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,        UsuarioRepository>();
builder.Services.AddScoped<IPacienteRepository,       PacienteRepository>();
builder.Services.AddScoped<IDoctorRepository,         DoctorRepository>();
builder.Services.AddScoped<ITurnoRepository,          TurnoRepository>();
builder.Services.AddScoped<ICoberturaRepository,      CoberturaRepository>();
builder.Services.AddScoped<IHistorialClinicoRepository, HistorialClinicoRepository>();
builder.Services.AddScoped<IMedicamentoRepository,    MedicamentoRepository>();
builder.Services.AddScoped<IRecetaRepository,         RecetaRepository>();
builder.Services.AddScoped<IEstudioRepository,        EstudioRepository>();
builder.Services.AddScoped<INotificacionRepository,   NotificacionRepository>();
builder.Services.AddScoped<IHorarioLaboralRepository, HorarioLaboralRepository>();
builder.Services.AddScoped<IUsuarioFotoRepository,    UsuarioFotoRepository>();
builder.Services.AddScoped<IMovimientoRepository,     MovimientoRepository>();

// ── 5. Lógica de negocio (Scoped) ─────────────────────────────────────────
builder.Services.AddScoped<IUsuarioLogica,         UsuarioLogica>();
builder.Services.AddScoped<IPacienteLogica,        PacienteLogica>();
builder.Services.AddScoped<IDoctorLogica,          DoctorLogica>();
builder.Services.AddScoped<ITurnoLogica,           TurnoLogica>();
builder.Services.AddScoped<ICoberturaLogica,       CoberturaLogica>();
builder.Services.AddScoped<IHistorialClinicoLogica, HistorialClinicoLogica>();
builder.Services.AddScoped<IMedicamentoLogica,     MedicamentoLogica>();
builder.Services.AddScoped<IRecetaLogica,          RecetaLogica>();
builder.Services.AddScoped<IEstudioLogica,         EstudioLogica>();
builder.Services.AddScoped<INotificacionLogica,    NotificacionLogica>();
builder.Services.AddScoped<IHorarioLaboralLogica,  HorarioLaboralLogica>();
builder.Services.AddScoped<IUsuarioFotoLogica,     UsuarioFotoLogica>();
builder.Services.AddScoped<IMovimientoLogica,      MovimientoLogica>();
builder.Services.AddScoped<IRegistroMovimientos,   RegistroMovimientos>();

// La hora de Argentina. Uno solo para toda la aplicación: no guarda nada.
builder.Services.AddSingleton<IReloj, RelojArgentina>();

// Tope de intentos de login y de cambio de contraseña. Uno solo para toda la
// aplicación: es el que guarda los contadores.
builder.Services.AddSingleton<LimiteIntentos>();

// ── 6. CORS (opcional para desarrollo) ────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Solo en desarrollo: avisa en la consola si a la base local le falta una migración.
if (app.Environment.IsDevelopment())
{
    AvisoDeMigraciones.Revisar(app);
}

// ── 7. Pipeline ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ChronoSalud API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseCors("DevPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ── 8. Registrar todos los endpoints ──────────────────────────────────────
app.MapUsuarioEndpoints();
app.MapUsuarioFotoEndpoints();
app.MapPacienteEndpoints();
app.MapDoctorEndpoints();
app.MapTurnoEndpoints();
app.MapCoberturaEndpoints();
app.MapHistorialClinicoEndpoints();
app.MapMedicamentoEndpoints();
app.MapRecetaEndpoints();
app.MapEstudioEndpoints();
app.MapReporteEndpoints();
app.MapNotificacionEndpoints();
app.MapHorarioLaboralEndpoints();
app.MapMovimientoEndpoints();
app.MapGet("/", () => "ChronoSalud API está funcionando ✅");
app.Run();
