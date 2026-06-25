using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Datos;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Doctor> Doctores => Set<Doctor>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Cobertura> Coberturas => Set<Cobertura>();
    public DbSet<PacienteCobertura> PacienteCoberturas => Set<PacienteCobertura>();
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaMedicamento> RecetaMedicamentos => Set<RecetaMedicamento>();
    public DbSet<Estudio> Estudios => Set<Estudio>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Usuario - email único
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Doctor - matricula única
        modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Matricula)
            .IsUnique();

        // Usuario -> Paciente (1 a 1)
        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.Usuario)
            .WithOne(u => u.Paciente)
            .HasForeignKey<Paciente>(p => p.IdUsuario);

        // Usuario -> Doctor (1 a 1)
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Usuario)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.IdUsuario);

        // Turno -> Paciente
        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Paciente)
            .WithMany(p => p.Turnos)
            .HasForeignKey(t => t.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        // Turno -> Doctor
        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Doctor)
            .WithMany(d => d.Turnos)
            .HasForeignKey(t => t.IdDoctor)
            .OnDelete(DeleteBehavior.Restrict);

        // PacienteCobertura -> Paciente
        modelBuilder.Entity<PacienteCobertura>()
            .HasOne(pc => pc.Paciente)
            .WithMany(p => p.PacienteCoberturas)
            .HasForeignKey(pc => pc.IdPaciente);

        // PacienteCobertura -> Cobertura
        modelBuilder.Entity<PacienteCobertura>()
            .HasOne(pc => pc.Cobertura)
            .WithMany(c => c.PacienteCoberturas)
            .HasForeignKey(pc => pc.IdCobertura);

        // HistorialClinico -> Paciente
        modelBuilder.Entity<HistorialClinico>()
            .HasOne(h => h.Paciente)
            .WithMany(p => p.HistorialesClinicos)
            .HasForeignKey(h => h.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        // HistorialClinico -> Turno (opcional)
        modelBuilder.Entity<HistorialClinico>()
            .HasOne(h => h.Turno)
            .WithMany()
            .HasForeignKey(h => h.IdTurno)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Receta -> Paciente
        modelBuilder.Entity<Receta>()
            .HasOne(r => r.Paciente)
            .WithMany(p => p.Recetas)
            .HasForeignKey(r => r.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        // Receta -> Doctor
        modelBuilder.Entity<Receta>()
            .HasOne(r => r.Doctor)
            .WithMany(d => d.Recetas)
            .HasForeignKey(r => r.IdDoctor)
            .OnDelete(DeleteBehavior.Restrict);

        // RecetaMedicamento -> Receta
        modelBuilder.Entity<RecetaMedicamento>()
            .HasOne(rm => rm.Receta)
            .WithMany(r => r.RecetaMedicamentos)
            .HasForeignKey(rm => rm.IdReceta);

        // RecetaMedicamento -> Medicamento
        modelBuilder.Entity<RecetaMedicamento>()
            .HasOne(rm => rm.Medicamento)
            .WithMany(m => m.RecetaMedicamentos)
            .HasForeignKey(rm => rm.IdMedicamento);

        // Estudio -> Paciente
        modelBuilder.Entity<Estudio>()
            .HasOne(e => e.Paciente)
            .WithMany(p => p.Estudios)
            .HasForeignKey(e => e.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        // Estudio -> Turno (opcional)
        modelBuilder.Entity<Estudio>()
            .HasOne(e => e.Turno)
            .WithMany()
            .HasForeignKey(e => e.IdTurno)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Notificacion -> Usuario
        modelBuilder.Entity<Notificacion>()
            .HasOne(n => n.Usuario)
            .WithMany(u => u.Notificaciones)
            .HasForeignKey(n => n.IdUsuario);
    }
}
