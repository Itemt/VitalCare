using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Data
{
    public class ModularSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ModularSeeder(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAllModulesAsync()
        {
            // Seed in order of dependencies
            await SeedRolesModule();
            await SeedMedicalModule();
            await SeedUsersModule();
            await SeedAppointmentsModule();
        }

        private async Task SeedRolesModule()
        {
            var roles = new[]
            {
                "Admin",
                "Doctor", 
                "Paciente"
            };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedMedicalModule()
        {
            // Seed Specialties
            if (!_context.Specialties.Any())
            {
                var specialties = new[]
                {
                    new Specialty { Name = "Medicina General", Description = "Atención médica general y preventiva" },
                    new Specialty { Name = "Cardiología", Description = "Especialidad del corazón y sistema circulatorio" },
                    new Specialty { Name = "Neurología", Description = "Especialidad del sistema nervioso" },
                    new Specialty { Name = "Pediatría", Description = "Medicina infantil y adolescente" },
                    new Specialty { Name = "Ginecología", Description = "Salud de la mujer y sistema reproductivo" },
                    new Specialty { Name = "Dermatología", Description = "Enfermedades de la piel" },
                    new Specialty { Name = "Oftalmología", Description = "Cuidado de los ojos y la visión" },
                    new Specialty { Name = "Ortopedia", Description = "Huesos, articulaciones y músculos" }
                };

                _context.Specialties.AddRange(specialties);
                await _context.SaveChangesAsync();
            }

            // Seed Medications
            if (!_context.Medications.Any())
            {
                var medications = new[]
                {
                    new Medication { Name = "Acetaminofén", Description = "Analgésico y antipirético" },
                    new Medication { Name = "Ibuprofeno", Description = "Antiinflamatorio no esteroideo" },
                    new Medication { Name = "Amoxicilina", Description = "Antibiótico de amplio espectro" },
                    new Medication { Name = "Metformina", Description = "Medicamento para diabetes tipo 2" },
                    new Medication { Name = "Losartán", Description = "Antihipertensivo" },
                    new Medication { Name = "Omeprazol", Description = "Inhibidor de la bomba de protones" },
                    new Medication { Name = "Salbutamol", Description = "Broncodilatador para asma" },
                    new Medication { Name = "Atorvastatina", Description = "Reductor de colesterol" }
                };

                _context.Medications.AddRange(medications);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedUsersModule()
        {
            // Seed Admin User
            var adminEmail = "admin@vitalcare.com";
            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Administrator",
                    LastName = "VitalCare",
                    PhoneNumber = "3001234567"
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Sample Doctors
            await SeedSampleDoctors();

            // Seed Sample Patients
            await SeedSamplePatients();
        }

        private async Task SeedSampleDoctors()
        {
            var specialties = await _context.Specialties.ToListAsync();
            
            var sampleDoctors = new[]
            {
                new { Email = "dr.martinez@vitalcare.com", FirstName = "Carlos", LastName = "Martínez", SpecialtyName = "Medicina General" },
                new { Email = "dr.rodriguez@vitalcare.com", FirstName = "Ana", LastName = "Rodríguez", SpecialtyName = "Cardiología" },
                new { Email = "dr.gonzalez@vitalcare.com", FirstName = "Luis", LastName = "González", SpecialtyName = "Pediatría" },
                new { Email = "dr.lopez@vitalcare.com", FirstName = "María", LastName = "López", SpecialtyName = "Ginecología" },
                new { Email = "dr.hernandez@vitalcare.com", FirstName = "José", LastName = "Hernández", SpecialtyName = "Neurología" }
            };

            foreach (var doctorData in sampleDoctors)
            {
                if (await _userManager.FindByEmailAsync(doctorData.Email) == null)
                {
                    var specialty = specialties.FirstOrDefault(s => s.Name == doctorData.SpecialtyName);
                    
                    var doctor = new User
                    {
                        UserName = doctorData.Email,
                        Email = doctorData.Email,
                        EmailConfirmed = true,
                        FirstName = doctorData.FirstName,
                        LastName = doctorData.LastName,
                        PhoneNumber = $"300{Random.Shared.Next(1000000, 9999999)}"
                    };

                    var result = await _userManager.CreateAsync(doctor, "Doctor123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(doctor, "Doctor");

                        // Create Doctor entity
                        var doctorEntity = new Doctor
                        {
                            UserId = doctor.Id,
                            FirstName = doctorData.FirstName,
                            LastName = doctorData.LastName,
                            SpecialtyId = specialty?.Id ?? specialties.First().Id,
                            LicenseNumber = $"DOC{Random.Shared.Next(10000, 99999)}",
        
                            IsAvailable = true
                        };

                        _context.Doctors.Add(doctorEntity);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSamplePatients()
        {
            var samplePatients = new[]
            {
                new { Email = "juan.perez@email.com", FirstName = "Juan", LastName = "Pérez" },
                new { Email = "maria.garcia@email.com", FirstName = "María", LastName = "García" },
                new { Email = "carlos.silva@email.com", FirstName = "Carlos", LastName = "Silva" },
                new { Email = "ana.torres@email.com", FirstName = "Ana", LastName = "Torres" },
                new { Email = "luis.morales@email.com", FirstName = "Luis", LastName = "Morales" }
            };

            foreach (var patientData in samplePatients)
            {
                if (await _userManager.FindByEmailAsync(patientData.Email) == null)
                {
                    var patient = new User
                    {
                        UserName = patientData.Email,
                        Email = patientData.Email,
                        EmailConfirmed = true,
                        FirstName = patientData.FirstName,
                        LastName = patientData.LastName,
                        PhoneNumber = $"310{Random.Shared.Next(1000000, 9999999)}"
                    };

                    var result = await _userManager.CreateAsync(patient, "Patient123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(patient, "Paciente");

                        // Create Patient entity
                        var patientEntity = new Patient
                        {
                            UserId = patient.Id,
                            FirstName = patientData.FirstName,
                            LastName = patientData.LastName,
                            Email = patientData.Email,
                            PhoneNumber = patient.PhoneNumber,
                            DateOfBirth = DateTime.Now.AddYears(-Random.Shared.Next(18, 80)),
                            DocumentId = $"DOC{Random.Shared.Next(10000000, 99999999)}",
                            Gender = Random.Shared.Next(2) == 0 ? Gender.Masculino : Gender.Femenino
                        };

                        _context.Patients.Add(patientEntity);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedAppointmentsModule()
        {
            if (!_context.Appointments.Any())
            {
                var doctors = await _context.Doctors.Include(d => d.User).ToListAsync();
                var patients = await _context.Patients.Include(p => p.User).ToListAsync();

                if (doctors.Any() && patients.Any())
                {
                    var random = new Random();
                    var appointmentCount = 10;

                    for (int i = 0; i < appointmentCount; i++)
                    {
                        var doctor = doctors[random.Next(doctors.Count)];
                        var patient = patients[random.Next(patients.Count)];
                        
                        var appointment = new Appointment
                        {
                            DoctorId = doctor.Id,
                            PatientId = patient.Id,
                            AppointmentDateTime = DateTime.Now.AddDays(random.Next(-30, 30)).AddHours(random.Next(8, 18)),
                            Notes = $"Cita de ejemplo {i + 1}",
                            IsConfirmed = random.Next(2) == 1,
                            IsCompleted = random.Next(3) == 1,
                            IsCancelled = false
                        };

                        _context.Appointments.Add(appointment);
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    // Extension method for easy registration
    public static class ModularSeederExtensions
    {
        public static async Task SeedModularDataAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var seeder = new ModularSeeder(context, userManager, roleManager);
            await seeder.SeedAllModulesAsync();
        }
    }
} 



