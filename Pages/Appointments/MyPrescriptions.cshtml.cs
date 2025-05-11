using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Policy = "RequirePatientRole")] // Ensure only patients can access
    public class MyPrescriptionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MyPrescriptionsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Prescription> Prescriptions { get;set; } = new List<Prescription>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Email == null)
            {
                // Need user and email to find patient record
                TempData["ErrorMessage"] = "No se pudo encontrar la información del usuario.";
                return RedirectToPage("/Index"); // Or appropriate error/login page
            }

            // Find the patient associated with the logged-in user's ID
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null)
            {
                TempData["ErrorMessage"] = "No se encontró un registro de paciente asociado a su cuenta.";
                // Consider if a patient record *should* always exist for a logged-in patient user.
                // If so, this might indicate an data inconsistency issue.
                return RedirectToPage("/Index");
            }

            Prescriptions = await _context.Prescriptions
                .Where(p => p.PatientId == patient.Id)
                .Include(p => p.Medication) // Include Medication details
                .Include(p => p.Doctor)     // Include Doctor details
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return Page();
        }
    }
} 