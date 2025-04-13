using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class DeletePrescriptionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DeletePrescriptionModel> _logger;

        public DeletePrescriptionModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<DeletePrescriptionModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public Prescription Prescription { get; set; } = default!;
        // Change type to int? to match nullable Prescription.AppointmentId
        public int? OriginalAppointmentId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound("ID de prescripción no proporcionado.");
            }

            Prescription = await _context.Prescriptions
                                .Include(p => p.Medication) // Include Medication for display
                                .Include(p => p.Appointment) // Needed for Doctor verification
                                .FirstOrDefaultAsync(m => m.Id == id);

            if (Prescription == null)
            {
                return NotFound("Prescripción no encontrada.");
            }

            // Verify Doctor permission
            var user = await _userManager.GetUserAsync(User);
            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);

            if (user == null || currentDoctor == null || Prescription.DoctorId != currentDoctor.Id)
            {
                 return Forbid("No tiene permiso para eliminar esta prescripción.");
            }
            
            OriginalAppointmentId = Prescription.AppointmentId; // Store for redirect

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescriptionToDelete = await _context.Prescriptions
                                                .Include(p => p.Appointment) // Needed for Doctor verification and redirect
                                                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescriptionToDelete == null)
            {
                // Already deleted or doesn't exist
                 return RedirectToPage("/Doctor/Agenda"); // Redirect somewhere sensible
            }

             // Verify Doctor permission again on POST
            var user = await _userManager.GetUserAsync(User);
            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);

            if (user == null || currentDoctor == null || prescriptionToDelete.DoctorId != currentDoctor.Id)
            {
                 return Forbid("No tiene permiso para eliminar esta prescripción.");
            }

            // Change type to int? to match nullable AppointmentId
            int? appointmentId = prescriptionToDelete.AppointmentId; // Store nullable ID before deleting

            try
            {
                _context.Prescriptions.Remove(prescriptionToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Prescripción ID {id} eliminada por Doctor ID {currentDoctor.Id}.");
                TempData["SuccessMessage"] = "Prescripción eliminada exitosamente.";
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, $"Error eliminando prescripción ID {id}.");
                 // Add error message to TempData or ModelState if needed
                 TempData["ErrorMessage"] = "Error al eliminar la prescripción.";
                 // Redirect back to appointment details to show error?
                 return RedirectToPage("/Appointments/Details", new { id = appointmentId }); 
            }
            
            // Redirect back to the original appointment's details page
            return RedirectToPage("/Appointments/Details", new { id = appointmentId });
        }
    }
} 