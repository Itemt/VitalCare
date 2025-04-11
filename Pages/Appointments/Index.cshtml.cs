using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization; // Add for authorization
using Microsoft.AspNetCore.Identity; // Required for UserManager
using System.Security.Claims; // Required for ClaimsPrincipal extension methods if needed

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient")] // Require Patient role
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

        public IList<Appointment> Appointment { get;set; } = default!;
        public string PatientName { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Not logged in
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (patient == null)
            {
                 _logger.LogWarning($"No Patient record found for user {user.Email}.");
                 // Optionally set an error message
                 TempData["ErrorMessage"] = "No se encontró su registro de paciente.";
                 Appointment = new List<Appointment>(); // Assign empty list
                 return Page(); // Show the page but with no data
            }

            PatientName = patient.FullName; // Store patient name if needed in the view

            // Filter appointments for the logged-in patient
            Appointment = await _context.Appointments
                .Where(a => a.PatientId == patient.Id)
                .Include(a => a.Doctor) // Include Doctor details
                    .ThenInclude(d => d.Specialty) // Include Doctor's Specialty
                // .Include(a => a.Patient) // No longer needed as we filter by patient ID
                .OrderBy(a => a.AppointmentDateTime) // Order by date
                .ToListAsync();

             _logger.LogInformation($"Loaded {Appointment.Count} appointments for patient {patient.FullName} (ID: {patient.Id}).");

            return Page();
        }
    }
} 