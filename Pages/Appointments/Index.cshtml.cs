using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Appointments
{
    [Authorize] // Ensure only logged-in users can access
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<Appointment> Appointment { get; set; } = new List<Appointment>();
        public string UserRole { get; set; } = string.Empty; // To know which view to tailor

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found.");
                return Challenge(); // Or redirect to login
            }

            _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) accessing Appointment Index.");

            if (await _userManager.IsInRoleAsync(user, "Patient"))
            {
                UserRole = "Patient";
                _logger.LogInformation($"Loading appointments for Patient: {user.UserName} (ID: {user.Id})");

                // --- FIX: Find Patient record using Email ---
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email); // Link User and Patient via Email
                if (patient != null)
                {
                    _logger.LogInformation($"Found matching Patient record (ID: {patient.Id}) for User {user.UserName} using email.");
                    Appointment = await _context.Appointments
                        .Where(a => a.PatientId == patient.Id) // Filter by the Patient's own ID
                        .Include(a => a.Doctor)
                            .ThenInclude(d => d.Specialty) // Include Specialty for display
                        .OrderBy(a => a.AppointmentDateTime)
                        .ToListAsync();
                    _logger.LogInformation($"Found {Appointment.Count} appointments for Patient {user.UserName} (Patient ID: {patient.Id}).");
                }
                else
                {
                    _logger.LogWarning($"User {user.UserName} (ID: {user.Id}) has role Patient but no associated Patient record found.");
                    Appointment = new List<Appointment>(); // Ensure list is initialized empty
                    // Optionally add a message for the user
                    // TempData["ErrorMessage"] = "No se pudo encontrar su registro de paciente asociado.";
                }
                // --- END FIX ---
            }
            else if (await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                // Doctors should primarily use their Agenda page. Redirect them there.
                _logger.LogInformation($"User {user.UserName} is a Doctor. Redirecting to Doctor/Agenda.");
                return RedirectToPage("/Doctor/Agenda");
            }
            else if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                UserRole = "Admin";
                _logger.LogInformation($"Loading all appointments for Admin: {user.UserName}");
                // Admins might see all appointments
                Appointment = await _context.Appointments
                    .Include(a => a.Patient) // Include Patient info for Admin view
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.Specialty) // Include Doctor and Specialty info
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();
                _logger.LogInformation($"Found {Appointment.Count} total appointments for Admin.");
            }
            else
            {
                _logger.LogWarning($"User {user.UserName} has an unrecognized role.");
                // Handle other roles or lack of roles if necessary
                Appointment = new List<Appointment>();
            }

            return Page();
        }
    }
}
