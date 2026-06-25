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

// ── 6. CORS (opcional para desarrollo) ────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

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

app.Run();
