using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Models;

namespace CitasEPS.Data
{
    // Hereda de IdentityDbContext, especificando el Usuario, Rol (usando IdentityRole<int> por defecto) y el tipo de clave (int)
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; } = default!;
        public DbSet<Patient> Patients { get; set; } = default!;
        public DbSet<Doctor> Doctors { get; set; } = default!;
        public DbSet<Specialty> Specialties { get; set; } = default!;
        public DbSet<Medication> Medications { get; set; } = default!;
        public DbSet<Prescription> Prescriptions { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; } = default!;
        // DbSet<User> se hereda de IdentityDbContext

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar relaciones y restricciones si es necesario
            // Ejemplo: Asegurar que DocumentId sea único para los Pacientes
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.DocumentId)
                .IsUnique();

            // Configurar la relación entre Notification y Appointment para eliminación en cascada
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Appointment) // Una notificación tiene una cita opcional
                .WithMany() // Una cita puede tener muchas notificaciones (no se necesita propiedad de navegación inversa explícita en Appointment para esto)
                .HasForeignKey(n => n.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade); // Al eliminar una cita, eliminar sus notificaciones

            // La inicialización de usuarios (seeding) se ha eliminado, se maneja mediante la configuración de Identity o el registro manual/asignación de roles posteriormente.

            // Personalizar las relaciones del modelo de ASP.NET Identity si es necesario aquí
            // Ejemplo: Cambiar nombres de tablas
            // modelBuilder.Entity<User>(entity => { entity.ToTable(name: "Users"); });
            // modelBuilder.Entity<IdentityRole<int>>(entity => { entity.ToTable(name: "Roles"); });
            // modelBuilder.Entity<IdentityUserRole<int>>(entity => { entity.ToTable("UserRoles"); });
            // modelBuilder.Entity<IdentityUserClaim<int>>(entity => { entity.ToTable("UserClaims"); });
            // modelBuilder.Entity<IdentityUserLogin<int>>(entity => { entity.ToTable("UserLogins"); });
            // modelBuilder.Entity<IdentityRoleClaim<int>>(entity => { entity.ToTable("RoleClaims"); });
            // modelBuilder.Entity<IdentityUserToken<int>>(entity => { entity.ToTable("UserTokens"); });
        }
    }
}