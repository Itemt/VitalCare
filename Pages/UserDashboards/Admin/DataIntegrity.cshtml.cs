using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.UserDashboards.Admin
{
    [Authorize(Roles = "Admin")]
    public class DataIntegrityModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DataIntegrityModel> _logger;

        public DataIntegrityModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<DataIntegrityModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public DataIntegrityStats? IntegrityStats { get; set; }
        public List<ValidationIssue> ValidationResults { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostValidatePatientIntegrityAsync()
        {
            try
            {
                _logger.LogInformation("Manual validation of Patient integrity initiated by admin.");

                // Get all users with "Paciente" role
                var patientsRole = await _userManager.GetUsersInRoleAsync("Paciente");
                var usersWithoutPatientRecord = new List<User>();
                var usersWithPatientRecord = new List<User>();

                foreach (var user in patientsRole)
                {
                    var hasPatientRecord = await _context.Patients.AnyAsync(p => p.UserId == user.Id);
                    if (!hasPatientRecord)
                    {
                        usersWithoutPatientRecord.Add(user);
                        ValidationResults.Add(new ValidationIssue
                        {
                            Type = "Usuario sin Paciente",
                            UserId = user.Id,
                            Email = user.Email ?? "Sin email",
                            Description = $"Usuario con rol Paciente pero sin registro de Paciente correspondiente"
                        });
                    }
                    else
                    {
                        usersWithPatientRecord.Add(user);
                    }
                }

                // Check for orphaned Patient records
                var orphanedPatients = await _context.Patients
                    .Where(p => !_context.Users.Any(u => u.Id == p.UserId))
                    .ToListAsync();

                foreach (var patient in orphanedPatients)
                {
                    ValidationResults.Add(new ValidationIssue
                    {
                        Type = "Paciente huérfano",
                        UserId = patient.UserId,
                        Email = patient.Email ?? "Sin email",
                        Description = $"Registro de Paciente sin usuario correspondiente (Paciente ID: {patient.Id})"
                    });
                }

                IntegrityStats = new DataIntegrityStats
                {
                    TotalUsersWithPatientRole = patientsRole.Count,
                    UsersWithPatientRecord = usersWithPatientRecord.Count,
                    UsersWithoutPatientRecord = usersWithoutPatientRecord.Count,
                    OrphanedPatientRecords = orphanedPatients.Count
                };

                if (ValidationResults.Any())
                {
                    TempData["InfoMessage"] = $"Validación completada. Se encontraron {ValidationResults.Count} problemas de integridad.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Validación completada. No se encontraron problemas de integridad.";
                }

                _logger.LogInformation($"Validation completed: {usersWithoutPatientRecord.Count} users missing Patient records, {orphanedPatients.Count} orphaned Patient records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual Patient integrity validation.");
                TempData["ErrorMessage"] = "Error durante la validación: " + ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostFixMissingPatientsAsync()
        {
            try
            {
                _logger.LogInformation("Manual fix of missing Patient records initiated by admin.");

                await MigrationHelpers.FixMissingPatientRecordsAsync(_context, _userManager, _logger);

                TempData["SuccessMessage"] = "Corrección de registros de pacientes completada. Revise los logs para más detalles.";
                
                // Refresh validation results to show the current state
                return await OnPostValidatePatientIntegrityAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual fix of missing Patient records.");
                TempData["ErrorMessage"] = "Error durante la corrección: " + ex.Message;
                return Page();
            }
        }

        public class DataIntegrityStats
        {
            public int TotalUsersWithPatientRole { get; set; }
            public int UsersWithPatientRecord { get; set; }
            public int UsersWithoutPatientRecord { get; set; }
            public int OrphanedPatientRecords { get; set; }
        }

        public class ValidationIssue
        {
            public string Type { get; set; } = "";
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public string Description { get; set; } = "";
        }
    }
} 




