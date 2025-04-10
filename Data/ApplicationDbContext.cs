using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Models;

namespace CitasEPS.Data
{
    // Inherit from IdentityDbContext, specifying the User, Role (using default IdentityRole<int>), and key type (int)
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; } = default!;
        public DbSet<Patient> Patients { get; set; } = default!;
        public DbSet<Doctor> Doctors { get; set; } = default!;
        // DbSet<User> is inherited from IdentityDbContext

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints if needed
            // Example: Ensure DocumentId is unique for Patients
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.DocumentId)
                .IsUnique();

            // User seeding is removed, handled by Identity setup or manual registration/role assignment later.

            // Customize the ASP.NET Identity model relations if needed here
            // Example: Change table names
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