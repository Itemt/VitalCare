using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Data
{
    public static class MigrationHelpers
    {
        public static async Task FixMissingPatientRecordsAsync(
            ApplicationDbContext context, 
            UserManager<User> userManager, 
            ILogger logger)
        {
            logger.LogInformation("Starting check for users missing Patient records...");

            // Get all users with "Paciente" role
            var patientsRole = await userManager.GetUsersInRoleAsync("Paciente");
            var usersWithoutPatientRecord = new List<User>();

            foreach (var user in patientsRole)
            {
                var hasPatientRecord = await context.Patients
                    .AnyAsync(p => p.UserId == user.Id);
                
                if (!hasPatientRecord)
                {
                    usersWithoutPatientRecord.Add(user);
                    logger.LogWarning($"User {user.Email} (ID: {user.Id}) has Paciente role but no Patient record.");
                }
            }

            if (usersWithoutPatientRecord.Count == 0)
            {
                logger.LogInformation("All users with Paciente role have corresponding Patient records.");
                return;
            }

            logger.LogInformation($"Found {usersWithoutPatientRecord.Count} users missing Patient records. Creating them now...");

            foreach (var user in usersWithoutPatientRecord)
            {
                try
                {
                    var patient = new Patient
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName ?? "Sin nombre",
                        LastName = user.LastName ?? "Sin apellido",
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        DocumentId = user.DocumentId,
                        Gender = user.Gender ?? CitasEPS.Models.Modules.Common.Gender.Otro
                    };

                    context.Patients.Add(patient);
                    await context.SaveChangesAsync();
                    
                    logger.LogInformation($"Created Patient record for user {user.Email} (ID: {user.Id})");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to create Patient record for user {user.Email} (ID: {user.Id})");
                }
            }

            logger.LogInformation("Finished fixing missing Patient records.");
        }

        public static async Task ValidateUserPatientIntegrityAsync(
            ApplicationDbContext context, 
            UserManager<User> userManager, 
            ILogger logger)
        {
            logger.LogInformation("Starting User-Patient integrity validation...");

            // Check for orphaned Patient records (Patient without User)
            var orphanedPatients = await context.Patients
                .Where(p => !context.Users.Any(u => u.Id == p.UserId))
                .ToListAsync();

            if (orphanedPatients.Any())
            {
                logger.LogWarning($"Found {orphanedPatients.Count} orphaned Patient records (no corresponding User):");
                foreach (var patient in orphanedPatients)
                {
                    logger.LogWarning($"- Patient ID: {patient.Id}, Email: {patient.Email}, UserId: {patient.UserId}");
                }
            }

            // Check for Users with Paciente role but no Patient record
            var patientsRole = await userManager.GetUsersInRoleAsync("Paciente");
            var usersWithoutPatient = new List<User>();

            foreach (var user in patientsRole)
            {
                var hasPatientRecord = await context.Patients.AnyAsync(p => p.UserId == user.Id);
                if (!hasPatientRecord)
                {
                    usersWithoutPatient.Add(user);
                }
            }

            if (usersWithoutPatient.Any())
            {
                logger.LogWarning($"Found {usersWithoutPatient.Count} users with Paciente role but no Patient record:");
                foreach (var user in usersWithoutPatient)
                {
                    logger.LogWarning($"- User ID: {user.Id}, Email: {user.Email}");
                }
            }

            if (!orphanedPatients.Any() && !usersWithoutPatient.Any())
            {
                logger.LogInformation("User-Patient integrity validation passed. No issues found.");
            }

            logger.LogInformation("User-Patient integrity validation completed.");
        }
    }
} 




