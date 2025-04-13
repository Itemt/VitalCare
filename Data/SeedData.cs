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
            logger.LogInformation("Intentando inicializar datos (seed)...");

            // Asegurar que la base de datos esté creada (debería hacerlo MigrateAsync)
            // context.Database.EnsureCreated(); // No es necesario si se usa MigrateAsync

            // Inicializar Roles
            await SeedRolesAsync(roleManager, logger);

            // Inicializar Usuario Admin
            await SeedAdminUserAsync(userManager, logger);

            // Inicializar Especialidades
            await SeedSpecialtiesAsync(context, logger);

            // Inicializar Médicos
            await SeedDoctorsAsync(context, userManager, logger);

            // Inicializar Pacientes
            await SeedPatientsAsync(context, userManager, logger);

            // Inicializar Medicamentos
            await SeedMedicationsAsync(context, logger);

            // Inicializar Citas
            await SeedAppointmentsAsync(context, logger);

            logger.LogInformation("Inicialización de datos (seed) completada.");
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
        {
            string[] roleNames = { "Admin", "Patient", "Doctor" }; // Añadido Rol Doctor
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    logger.LogInformation($"Rol '{roleName}' creado.");
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<User> userManager, ILogger logger)
        {
            // Asegurar que el usuario admin exista
            var adminEmail = "admin@vitalcare.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsAdmin = true,
                    EmailConfirmed = true // Normalmente confirmar emails admin manualmente
                };
                // IMPORTANTE: ¡Establecer una contraseña segura para el admin en una aplicación real!
                // Considerar usar configuración o gestión de secretos para la contraseña inicial.
                var result = await userManager.CreateAsync(adminUser, "AdminPass123!");

                if (result.Succeeded)
                {
                    logger.LogInformation($"Usuario admin '{adminEmail}' creado exitosamente.");
                    // Asignar rol 'Admin'
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    // Añadir claim IsAdmin (redundante si se chequea el rol, pero sigue lógica anterior)
                    await userManager.AddClaimAsync(adminUser, new Claim("IsAdmin", "true"));
                    logger.LogInformation($"Usuario admin '{adminEmail}' asignado al rol Admin y se le dio el claim IsAdmin.");
                }
                else
                {
                    logger.LogError($"Error creando usuario admin '{adminEmail}'. Errores: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                 logger.LogInformation($"Usuario admin '{adminEmail}' ya existe.");
                 // Asegurar que el usuario tenga rol Admin y claim, incluso si ya existía
                 if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                 {
                     await userManager.AddToRoleAsync(adminUser, "Admin");
                     logger.LogInformation($"Usuario admin existente '{adminEmail}' añadido al rol Admin.");
                 }
                 var claims = await userManager.GetClaimsAsync(adminUser);
                 if (!claims.Any(c => c.Type == "IsAdmin" && c.Value == "true"))
                 {
                     await userManager.AddClaimAsync(adminUser, new Claim("IsAdmin", "true"));
                      logger.LogInformation($"Usuario admin existente '{adminEmail}' recibió el claim IsAdmin.");
                 }
            }
        }

        private static async Task SeedSpecialtiesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Specialties.AnyAsync()) {
                 logger.LogInformation("Especialidades ya inicializadas (seed).");
                 return; // Base de datos ya inicializada
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
            logger.LogInformation($"{specialties.Count} especialidades inicializadas (seed).");
        }

        private static async Task SeedDoctorsAsync(ApplicationDbContext context, UserManager<User> userManager, ILogger logger)
        {
            if (await context.Doctors.AnyAsync()) {
                logger.LogInformation("Médicos ya inicializados (seed).");
                return;
            }
            if (!await context.Specialties.AnyAsync()) {
                logger.LogWarning("No se pueden inicializar médicos porque no existen especialidades.");
                 return; // Se necesitan especialidades primero
            }

            // Obtener IDs para varias especialidades
            var specialties = await context.Specialties.ToDictionaryAsync(s => s.Name, s => s.Id);

            var doctorsData = new List<(string FirstName, string LastName, int SpecialtyId, string MedicalLicense, string Email, string Phone, string Password)>() {
                ("Alejandro", "Guerra", specialties["Medicina General"], "MG1001", "aguerra@vitalcare.com", "3101234567", "DoctorPass1!"),
                ("Sofia", "Ramirez", specialties["Cardiología"], "CA2002", "sramirez@vitalcare.com", "3119876543", "DoctorPass2!"),
                ("Carlos", "Vega", specialties["Dermatología"], "DE3003", "cvega@vitalcare.com", "3125551122", "DoctorPass3!"),
                ("Laura", "Mendez", specialties["Medicina General"], "MG1004", "lmendez@vitalcare.com", "3134443322", "DoctorPass4!"),
                ("Ricardo", "Perez", specialties["Pediatría"], "PE4001", "rperez@vitalcare.com", "3141112233", "DoctorPass5!"),
                ("Ana", "Martinez", specialties["Ginecología"], "GI5002", "amartinez@vitalcare.com", "3153334455", "DoctorPass6!"),
                ("Jorge", "Linares", specialties["Ortopedia"], "OR6003", "jlinares@vitalcare.com", "3167778899", "DoctorPass7!"),
                ("Beatriz", "Alvarez", specialties["Neurología"], "NE7004", "balvarez@vitalcare.com", "3176665544", "DoctorPass8!"),
                ("David", "Suarez", specialties["Oftalmología"], "OF8005", "dsuarez@vitalcare.com", "3189990011", "DoctorPass9!"),
                ("Elena", "Rojas", specialties["Psicología"], "PS9006", "erojas@vitalcare.com", "3192223344", "DoctorPass10!"),
                ("Mario", "Benavides", specialties["Endocrinología"], "EN1007", "mbenavides@vitalcare.com", "3205556677", "DoctorPass11!"),
                ("Lucia", "Castro", specialties["Medicina General"], "MG1008", "lcastro@vitalcare.com", "3218887766", "DoctorPass12!")
            };

            int doctorsCreatedCount = 0;
            foreach (var docData in doctorsData)
            {
                // Crear el registro Doctor primero
                var newDoctor = new Doctor
                {
                    FirstName = docData.FirstName,
                    LastName = docData.LastName,
                    SpecialtyId = docData.SpecialtyId,
                    MedicalLicenseNumber = docData.MedicalLicense,
                    Email = docData.Email,
                    PhoneNumber = docData.Phone
                };
                context.Doctors.Add(newDoctor); // Add to context, but don't save yet

                // Ahora crear el User asociado
                var userExists = await userManager.FindByEmailAsync(docData.Email);
                if (userExists == null)
                {
                    var newUser = new User { UserName = docData.Email, Email = docData.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(newUser, docData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, "Doctor");
                        logger.LogInformation($"Usuario Doctor '{docData.Email}' creado y añadido al rol Doctor.");
                        doctorsCreatedCount++;
                    }
                    else
                    {
                        logger.LogError($"Error creando usuario Doctor '{docData.Email}'. Errores: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        // Consider removing the Doctor record if User creation fails?
                        context.Doctors.Remove(newDoctor);
                    }
                }
                else
                {
                     logger.LogWarning($"Usuario '{docData.Email}' ya existe. Se asume que es el Doctor correcto. Asegurando rol Doctor.");
                      if (!await userManager.IsInRoleAsync(userExists, "Doctor"))
                      {
                          await userManager.AddToRoleAsync(userExists, "Doctor"); // Ensure role
                      }
                }
            }

            await context.SaveChangesAsync(); // Save all doctors (and potentially removed ones)
            logger.LogInformation($"Se intentaron inicializar {doctorsData.Count} doctores, {doctorsCreatedCount} nuevos usuarios Doctor creados.");
        }

        private static async Task SeedPatientsAsync(ApplicationDbContext context, UserManager<User> userManager, ILogger logger)
        {
            // Verificar si existen pacientes base para prevenir reinicialización completa
            if (await context.Patients.AnyAsync(p => p.DocumentId == "11223344" || p.DocumentId == "55667788")) {
                 logger.LogInformation("Pacientes base ya inicializados (seed).");
                 // Opcional: verificar los demás y añadir si faltan, o solo retornar
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
                // Verificar si el usuario o paciente ya existen antes de intentar crear
                var userExists = await userManager.FindByEmailAsync(pData.Email);
                var patientExists = await context.Patients.AnyAsync(p => p.DocumentId == pData.DocumentId || p.Email == pData.Email);

                if (userExists == null && !patientExists)
                {
                    var newUser = new User { UserName = pData.Email, Email = pData.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(newUser, pData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, "Patient");
                        logger.LogInformation($"Usuario paciente '{pData.Email}' creado.");
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
                        await context.SaveChangesAsync(); // Guardar cada paciente o guardar en lote fuera del bucle
                        logger.LogInformation($"Registro de paciente para '{pData.Email}' creado.");
                        patientsCreatedCount++;
                    }
                    else
                    {
                        logger.LogError($"Error creando usuario paciente '{pData.Email}'. Errores: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else {
                     logger.LogWarning($"Omitiendo inicialización para paciente '{pData.Email}' / '{pData.DocumentId}' porque el usuario o registro de paciente ya existe.");
                }
            }
             logger.LogInformation($"Se intentaron inicializar {patientsToAdd.Count} pacientes, {patientsCreatedCount} nuevos registros creados.");
        }

        private static async Task SeedMedicationsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Medications.AnyAsync())
            {
                logger.LogInformation("Medicamentos ya inicializados (seed).");
                return; // Base de datos ya inicializada
            }

            var medications = new List<Medication>
            {
                new Medication { Name = "Acetaminofén 500mg", Description = "Analgésico y antipirético común." },
                new Medication { Name = "Ibuprofeno 400mg", Description = "Antiinflamatorio no esteroideo (AINE)." },
                new Medication { Name = "Amoxicilina 500mg", Description = "Antibiótico betalactámico." },
                new Medication { Name = "Loratadina 10mg", Description = "Antihistamínico para alergias." },
                new Medication { Name = "Omeprazol 20mg", Description = "Inhibidor de la bomba de protones para el reflujo ácido." },
                new Medication { Name = "Salbutamol Inhalador", Description = "Broncodilatador para el asma." },
                new Medication { Name = "Metformina 500mg", Description = "Antidiabético oral." },
                new Medication { Name = "Atorvastatina 20mg", Description = "Estatina para reducir el colesterol." },
                new Medication { Name = "Losartán 50mg", Description = "Antagonista del receptor de angiotensina II para la hipertensión." },
                new Medication { Name = "Sertralina 50mg", Description = "Inhibidor selectivo de la recaptación de serotonina (ISRS) para la depresión/ansiedad." }
            };

            await context.Medications.AddRangeAsync(medications);
            await context.SaveChangesAsync();
            logger.LogInformation($"{medications.Count} medicamentos inicializados (seed).");
        }

        private static async Task SeedAppointmentsAsync(ApplicationDbContext context, ILogger logger)
        {
             if (await context.Appointments.AnyAsync()) {
                 logger.LogInformation("Citas ya inicializadas (seed).");
                 return;
             }
             if (!await context.Patients.AnyAsync() || !await context.Doctors.AnyAsync()) {
                logger.LogWarning("No se pueden inicializar citas porque no existen pacientes o médicos.");
                return; // Se necesitan pacientes y médicos primero
            }

            // Obtener todos los pacientes y médicos para asignar citas aleatoriamente
            var allPatients = await context.Patients.ToListAsync();
            var allDoctors = await context.Doctors.ToListAsync();
            var random = new Random();

            if (allPatients.Count < 5 || allDoctors.Count < 5) {
                 logger.LogWarning("No hay suficientes pacientes o médicos para inicialización diversa de citas.");
                 return;
            }

            var appointments = new List<Appointment>();
            var startDate = DateTime.UtcNow.Date;

            // Crear ~20 citas distribuidas en las próximas 2 semanas
            for (int i = 0; i < 20; i++)
            {
                var patient = allPatients[random.Next(allPatients.Count)];
                var doctor = allDoctors[random.Next(allDoctors.Count)];
                var appointmentDay = startDate.AddDays(random.Next(1, 15)); // Dentro de los próximos 14 días
                var appointmentHour = random.Next(8, 17); // Entre 8 AM y 4 PM (16:xx)
                var appointmentTime = appointmentDay.AddHours(appointmentHour).AddMinutes(random.Next(0, 4) * 15); // En intervalos de 15 min
                bool isConfirmed = random.Next(0, 2) == 1; // 50% de probabilidad de confirmada

                appointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime = appointmentTime,
                    IsConfirmed = isConfirmed,
                    Notes = $"Cita {i+1}: {patient.FirstName} con Dr(a). {doctor.LastName}. Motivo: {(i % 3 == 0 ? "Control" : (i % 3 == 1 ? "Consulta General" : "Evaluación"))}" // Notas de ejemplo
                });
            }

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
            logger.LogInformation($"{appointments.Count} citas de ejemplo inicializadas (seed).");
        }
    }
} 