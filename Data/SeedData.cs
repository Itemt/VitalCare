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
            if (await context.Specialties.AnyAsync()) {
                 logger.LogInformation("Specialties already seeded.");
                 return; // DB has been seeded
            }

            var specialties = new List<Specialty>
            {
                new Specialty { Name = "Cardiología" },
                new Specialty { Name = "Dermatología" },
                new Specialty { Name = "Medicina General" },
                new Specialty { Name = "Neurología" },
                new Specialty { Name = "Pediatría" },
                new Specialty { Name = "Ginecología" },
                new Specialty { Name = "Ortopedia" },
                new Specialty { Name = "Oftalmología" },
                new Specialty { Name = "Psicología" },
                new Specialty { Name = "Endocrinología" }
            };

            await context.Specialties.AddRangeAsync(specialties);
            await context.SaveChangesAsync();
            logger.LogInformation($"{specialties.Count} specialties seeded.");
        }

        private static async Task SeedDoctorsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Doctors.AnyAsync()) {
                logger.LogInformation("Doctors already seeded.");
                return;
            }
            if (!await context.Specialties.AnyAsync()) {
                logger.LogWarning("Cannot seed doctors because no specialties exist.");
                 return; // Need specialties first
            }

            // Get IDs for various specialties
            var specialties = await context.Specialties.ToDictionaryAsync(s => s.Name, s => s.Id);

            var doctors = new List<Doctor>
            {
                new Doctor { FirstName = "Alejandro", LastName = "Guerra", SpecialtyId = specialties["Medicina General"], MedicalLicenseNumber = "MG1001", Email="aguerra@vitalcare.com", PhoneNumber="3101234567" },
                new Doctor { FirstName = "Sofia", LastName = "Ramirez", SpecialtyId = specialties["Cardiología"], MedicalLicenseNumber = "CA2002", Email="sramirez@vitalcare.com", PhoneNumber="3119876543" },
                new Doctor { FirstName = "Carlos", LastName = "Vega", SpecialtyId = specialties["Dermatología"], MedicalLicenseNumber = "DE3003", Email="cvega@vitalcare.com", PhoneNumber="3125551122" },
                new Doctor { FirstName = "Laura", LastName = "Mendez", SpecialtyId = specialties["Medicina General"], MedicalLicenseNumber = "MG1004", Email="lmendez@vitalcare.com", PhoneNumber="3134443322" },
                new Doctor { FirstName = "Ricardo", LastName = "Perez", SpecialtyId = specialties["Pediatría"], MedicalLicenseNumber = "PE4001", Email="rperez@vitalcare.com", PhoneNumber="3141112233" },
                new Doctor { FirstName = "Ana", LastName = "Martinez", SpecialtyId = specialties["Ginecología"], MedicalLicenseNumber = "GI5002", Email="amartinez@vitalcare.com", PhoneNumber="3153334455" },
                new Doctor { FirstName = "Jorge", LastName = "Linares", SpecialtyId = specialties["Ortopedia"], MedicalLicenseNumber = "OR6003", Email="jlinares@vitalcare.com", PhoneNumber="3167778899" },
                new Doctor { FirstName = "Beatriz", LastName = "Alvarez", SpecialtyId = specialties["Neurología"], MedicalLicenseNumber = "NE7004", Email="balvarez@vitalcare.com", PhoneNumber="3176665544" },
                new Doctor { FirstName = "David", LastName = "Suarez", SpecialtyId = specialties["Oftalmología"], MedicalLicenseNumber = "OF8005", Email="dsuarez@vitalcare.com", PhoneNumber="3189990011" },
                new Doctor { FirstName = "Elena", LastName = "Rojas", SpecialtyId = specialties["Psicología"], MedicalLicenseNumber = "PS9006", Email="erojas@vitalcare.com", PhoneNumber="3192223344" },
                new Doctor { FirstName = "Mario", LastName = "Benavides", SpecialtyId = specialties["Endocrinología"], MedicalLicenseNumber = "EN1007", Email="mbenavides@vitalcare.com", PhoneNumber="3205556677" },
                new Doctor { FirstName = "Lucia", LastName = "Castro", SpecialtyId = specialties["Medicina General"], MedicalLicenseNumber = "MG1008", Email="lcastro@vitalcare.com", PhoneNumber="3218887766" }

            };

            await context.Doctors.AddRangeAsync(doctors);
            await context.SaveChangesAsync();
             logger.LogInformation($"{doctors.Count} doctors seeded.");
        }

        private static async Task SeedPatientsAsync(ApplicationDbContext context, UserManager<User> userManager, ILogger logger)
        {
            // Check if base patients exist to prevent re-seeding everything
            if (await context.Patients.AnyAsync(p => p.DocumentId == "11223344" || p.DocumentId == "55667788")) {
                 logger.LogInformation("Base patients already seeded.");
                 // Optionally check for the others and add if missing, or just return
                 // return;
            }

            var patientsToAdd = new List<(string FirstName, string LastName, string DocumentId, DateTime Dob, string Email, string Phone, string Password)>
            {
                ("Juan", "Perez", "11223344", new DateTime(1985, 5, 15, 0, 0, 0, DateTimeKind.Utc), "juan.perez@email.com", "3001112233", "PatientPass1!"),
                ("Maria", "Gomez", "55667788", new DateTime(1992, 11, 20, 0, 0, 0, DateTimeKind.Utc), "maria.gomez@email.com", "3019998877", "PatientPass2!"),
                ("Carlos", "Rodriguez", "99887766", new DateTime(1978, 2, 10, 0, 0, 0, DateTimeKind.Utc), "carlos.rodriguez@email.com", "3025556677", "PatientPass3!"),
                ("Ana", "Lopez", "12312312", new DateTime(2001, 8, 25, 0, 0, 0, DateTimeKind.Utc), "ana.lopez@email.com", "3031231234", "PatientPass4!"),
                ("Luis", "Martinez", "45645645", new DateTime(1995, 12, 1, 0, 0, 0, DateTimeKind.Utc), "luis.martinez@email.com", "3044564567", "PatientPass5!"),
                ("Elena", "Sanchez", "78978978", new DateTime(1988, 6, 30, 0, 0, 0, DateTimeKind.Utc), "elena.sanchez@email.com", "3057897890", "PatientPass6!"),
                ("Miguel", "Ramirez", "10101010", new DateTime(1999, 4, 18, 0, 0, 0, DateTimeKind.Utc), "miguel.ramirez@email.com", "3061010101", "PatientPass7!"),
                ("Sofia", "Torres", "20202020", new DateTime(1991, 9, 5, 0, 0, 0, DateTimeKind.Utc), "sofia.torres@email.com", "3072020202", "PatientPass8!"),
                ("Andres", "Diaz", "30303030", new DateTime(1982, 1, 12, 0, 0, 0, DateTimeKind.Utc), "andres.diaz@email.com", "3083030303", "PatientPass9!"),
                ("Paula", "Vargas", "40404040", new DateTime(2003, 3, 22, 0, 0, 0, DateTimeKind.Utc), "paula.vargas@email.com", "3094040404", "PatientPass10!")
            };

            int patientsCreatedCount = 0;
            foreach (var pData in patientsToAdd)
            {
                // Check if user or patient already exists before attempting creation
                var userExists = await userManager.FindByEmailAsync(pData.Email);
                var patientExists = await context.Patients.AnyAsync(p => p.DocumentId == pData.DocumentId || p.Email == pData.Email);

                if (userExists == null && !patientExists)
                {
                    var newUser = new User { UserName = pData.Email, Email = pData.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(newUser, pData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, "Patient");
                        logger.LogInformation($"Patient user '{pData.Email}' created.");
                        var newPatient = new Patient
                        {
                            FirstName = pData.FirstName,
                            LastName = pData.LastName,
                            DocumentId = pData.DocumentId,
                            DateOfBirth = pData.Dob,
                            Email = pData.Email,
                            PhoneNumber = pData.Phone
                        };
                        await context.Patients.AddAsync(newPatient);
                        await context.SaveChangesAsync(); // Save each patient to get ID if needed later, or batch save outside loop
                        logger.LogInformation($"Patient record for '{pData.Email}' created.");
                        patientsCreatedCount++;
                    }
                    else
                    {
                        logger.LogError($"Error creating patient user '{pData.Email}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else {
                     logger.LogWarning($"Skipping seeding for patient '{pData.Email}' / '{pData.DocumentId}' as user or patient record already exists.");
                }
            }
             logger.LogInformation($"Attempted to seed {patientsToAdd.Count} patients, {patientsCreatedCount} new records created.");
        }

        private static async Task SeedAppointmentsAsync(ApplicationDbContext context, ILogger logger)
        {
             if (await context.Appointments.AnyAsync()) {
                 logger.LogInformation("Appointments already seeded.");
                 return;
             }
             if (!await context.Patients.AnyAsync() || !await context.Doctors.AnyAsync()) {
                logger.LogWarning("Cannot seed appointments because no patients or doctors exist.");
                return; // Need patients and doctors first
            }

            // Get all patients and doctors to randomly assign appointments
            var allPatients = await context.Patients.ToListAsync();
            var allDoctors = await context.Doctors.ToListAsync();
            var random = new Random();

            if (allPatients.Count < 5 || allDoctors.Count < 5) {
                 logger.LogWarning("Not enough patients or doctors available for diverse appointment seeding.");
                 return;
            }

            var appointments = new List<Appointment>();
            var startDate = DateTime.UtcNow.Date;

            // Create ~20 appointments spread over the next 2 weeks
            for (int i = 0; i < 20; i++)
            {
                var patient = allPatients[random.Next(allPatients.Count)];
                var doctor = allDoctors[random.Next(allDoctors.Count)];
                var appointmentDay = startDate.AddDays(random.Next(1, 15)); // Within next 14 days
                var appointmentHour = random.Next(8, 17); // Between 8 AM and 4 PM (16:xx)
                var appointmentTime = appointmentDay.AddHours(appointmentHour).AddMinutes(random.Next(0, 4) * 15); // On 15-min intervals
                bool isConfirmed = random.Next(0, 2) == 1; // 50% chance confirmed

                appointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime = appointmentTime,
                    IsConfirmed = isConfirmed,
                    Notes = $"Cita {i+1}: {patient.FirstName} con Dr(a). {doctor.LastName}. Motivo: {(i % 3 == 0 ? "Control" : (i % 3 == 1 ? "Consulta General" : "Evaluación"))}"
                });
            }

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
            logger.LogInformation($"{appointments.Count} sample appointments seeded.");
        }
    }
} 