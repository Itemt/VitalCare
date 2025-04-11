using CitasEPS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CitasEPS.Data
{
    public static class SeedData
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider,
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            ILogger logger)
        {
            logger.LogInformation("Attempting to seed data...");

            // Ensure database is created (should be done by MigrateAsync already)
            // context.Database.EnsureCreated(); // Not needed if MigrateAsync is used

            // Seed Roles
            await SeedRolesAsync(roleManager, logger);

            // Seed Admin User
            await SeedAdminUserAsync(userManager, logger);

            // Seed Specialties
            await SeedSpecialtiesAsync(context, logger);

            // Seed Doctors
            await SeedDoctorsAsync(context, logger);

            // Seed Patients
            await SeedPatientsAsync(context, userManager, logger);

            // Seed Appointments
            await SeedAppointmentsAsync(context, logger);

            logger.LogInformation("Data seeding complete.");
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
        {
            string[] roleNames = { "Admin", "Patient" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    logger.LogInformation($"Role '{roleName}' created.");
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<User> userManager, ILogger logger)
        {
            // Ensure admin user exists
            var adminEmail = "admin@vitalcare.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsAdmin = true,
                    EmailConfirmed = true // Typically confirm admin emails manually or via a setup process
                };
                // IMPORTANT: Set a strong password for the admin user in a real application!
                // Consider using configuration or secrets management for the initial password.
                var result = await userManager.CreateAsync(adminUser, "AdminPass123!");

                if (result.Succeeded)
                {
                    logger.LogInformation($"Admin user '{adminEmail}' created successfully.");
                    // Assign the 'Admin' role
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    // Add IsAdmin claim (redundant if checking role, but follows previous logic)
                    await userManager.AddClaimAsync(adminUser, new Claim("IsAdmin", "true"));
                    logger.LogInformation($"Admin user '{adminEmail}' assigned to Admin role and given IsAdmin claim.");
                }
                else
                {
                    logger.LogError($"Error creating admin user '{adminEmail}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                 logger.LogInformation($"Admin user '{adminEmail}' already exists.");
                 // Ensure user is in Admin role and has claim, even if they existed before
                 if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                 {
                     await userManager.AddToRoleAsync(adminUser, "Admin");
                     logger.LogInformation($"Existing admin user '{adminEmail}' added to Admin role.");
                 }
                 var claims = await userManager.GetClaimsAsync(adminUser);
                 if (!claims.Any(c => c.Type == "IsAdmin" && c.Value == "true"))
                 {
                     await userManager.AddClaimAsync(adminUser, new Claim("IsAdmin", "true"));
                      logger.LogInformation($"Existing admin user '{adminEmail}' given IsAdmin claim.");
                 }
            }
        }

        private static async Task SeedSpecialtiesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Specialties.AnyAsync()) return; // DB has been seeded

            var specialties = new List<Specialty>
            {
                new Specialty { Name = "Cardiología" },
                new Specialty { Name = "Dermatología" },
                new Specialty { Name = "Medicina General" },
                new Specialty { Name = "Neurología" },
                new Specialty { Name = "Pediatría" }
            };

            await context.Specialties.AddRangeAsync(specialties);
            await context.SaveChangesAsync();
            logger.LogInformation($"{specialties.Count} specialties seeded.");
        }

        private static async Task SeedDoctorsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Doctors.AnyAsync()) return;
            if (!await context.Specialties.AnyAsync()) {
                logger.LogWarning("Cannot seed doctors because no specialties exist.");
                 return; // Need specialties first
            }

            var generalMed = await context.Specialties.FirstAsync(s => s.Name == "Medicina General");
            var cardiology = await context.Specialties.FirstAsync(s => s.Name == "Cardiología");
            var dermatology = await context.Specialties.FirstAsync(s => s.Name == "Dermatología");

            var doctors = new List<Doctor>
            {
                new Doctor { FirstName = "Alejandro", LastName = "Guerra", SpecialtyId = generalMed.Id, MedicalLicenseNumber = "MG1001", Email="aguerra@vitalcare.com", PhoneNumber="3101234567" },
                new Doctor { FirstName = "Sofia", LastName = "Ramirez", SpecialtyId = cardiology.Id, MedicalLicenseNumber = "CA2002", Email="sramirez@vitalcare.com", PhoneNumber="3119876543" },
                new Doctor { FirstName = "Carlos", LastName = "Vega", SpecialtyId = dermatology.Id, MedicalLicenseNumber = "DE3003", Email="cvega@vitalcare.com", PhoneNumber="3125551122" },
                new Doctor { FirstName = "Laura", LastName = "Mendez", SpecialtyId = generalMed.Id, MedicalLicenseNumber = "MG1004", Email="lmendez@vitalcare.com", PhoneNumber="3134443322" }
            };

            await context.Doctors.AddRangeAsync(doctors);
            await context.SaveChangesAsync();
             logger.LogInformation($"{doctors.Count} doctors seeded.");
        }

        private static async Task SeedPatientsAsync(ApplicationDbContext context, UserManager<User> userManager, ILogger logger)
        {
            if (await context.Patients.AnyAsync()) return;

            // Patient 1
            var patientUser1Email = "juan.perez@email.com";
            var patientUser1 = await userManager.FindByEmailAsync(patientUser1Email);
            if (patientUser1 == null)
            {
                patientUser1 = new User { UserName = patientUser1Email, Email = patientUser1Email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(patientUser1, "PatientPass1!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(patientUser1, "Patient");
                     logger.LogInformation($"Patient user '{patientUser1Email}' created.");
                    var patient1 = new Patient { FirstName = "Juan", LastName = "Perez", DocumentId = "11223344", DateOfBirth = new DateTime(1985, 5, 15, 0, 0, 0, DateTimeKind.Utc), Email = patientUser1Email, PhoneNumber = "3001112233" };
                    await context.Patients.AddAsync(patient1);
                    await context.SaveChangesAsync();
                     logger.LogInformation($"Patient record for '{patientUser1Email}' created.");
                }
                 else
                {
                     logger.LogError($"Error creating patient user '{patientUser1Email}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Patient 2
            var patientUser2Email = "maria.gomez@email.com";
            var patientUser2 = await userManager.FindByEmailAsync(patientUser2Email);
            if (patientUser2 == null)
            {
                patientUser2 = new User { UserName = patientUser2Email, Email = patientUser2Email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(patientUser2, "PatientPass2!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(patientUser2, "Patient");
                    logger.LogInformation($"Patient user '{patientUser2Email}' created.");
                    var patient2 = new Patient { FirstName = "Maria", LastName = "Gomez", DocumentId = "55667788", DateOfBirth = new DateTime(1992, 11, 20, 0, 0, 0, DateTimeKind.Utc), Email = patientUser2Email, PhoneNumber = "3019998877" };
                    await context.Patients.AddAsync(patient2);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Patient record for '{patientUser2Email}' created.");
                }
                else
                {
                     logger.LogError($"Error creating patient user '{patientUser2Email}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static async Task SeedAppointmentsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Appointments.AnyAsync()) return;
            if (!await context.Patients.AnyAsync() || !await context.Doctors.AnyAsync()) {
                logger.LogWarning("Cannot seed appointments because no patients or doctors exist.");
                return; // Need patients and doctors first
            }

            var patientJuan = await context.Patients.FirstOrDefaultAsync(p => p.DocumentId == "11223344");
            var patientMaria = await context.Patients.FirstOrDefaultAsync(p => p.DocumentId == "55667788");
            var drGuerra = await context.Doctors.FirstOrDefaultAsync(d => d.MedicalLicenseNumber == "MG1001");
            var drRamirez = await context.Doctors.FirstOrDefaultAsync(d => d.MedicalLicenseNumber == "CA2002");

            if (patientJuan == null || patientMaria == null || drGuerra == null || drRamirez == null) {
                logger.LogWarning("Could not find specific patients or doctors required for appointment seeding.");
                 return;
            }

            var appointments = new List<Appointment>
            {
                new Appointment {
                    PatientId = patientJuan.Id,
                    DoctorId = drGuerra.Id,
                    AppointmentDateTime = DateTime.UtcNow.AddDays(7).Date.AddHours(9), // Next week at 9 AM UTC
                    IsConfirmed = true,
                    Notes = "Consulta General - Chequeo anual"
                },
                new Appointment {
                    PatientId = patientMaria.Id,
                    DoctorId = drRamirez.Id,
                    AppointmentDateTime = DateTime.UtcNow.AddDays(10).Date.AddHours(14), // 10 days from now at 2 PM UTC
                    IsConfirmed = false,
                    Notes = "Evaluación Cardiológica"
                }
            };

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
            logger.LogInformation($"{appointments.Count} appointments seeded.");
        }
    }
} 