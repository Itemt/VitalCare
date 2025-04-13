using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PrescribeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PrescribeModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Prescription Prescription { get; set; } = new(); // Initialize to avoid null issues

        // Nullable Appointment for display purposes if ID provided
        public Appointment? Appointment { get; set; } 
        public SelectList? Medications { get; set; }
        // Add Patients SelectList for when no appointment is provided
        public SelectList? Patients { get; set; }

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Not logged in

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (currentDoctor == null) return Forbid("El usuario actual no es un doctor registrado.");

            // Prepare default prescription details
            Prescription.DoctorId = currentDoctor.Id;
            Prescription.PrescriptionDate = DateTime.UtcNow; // Use UtcNow for consistency

            if (appointmentId.HasValue)
            {
                // --- Scenario: Prescribing for a specific appointment --- 
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor) // Redundant but good practice
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);

                if (Appointment == null)
                {
                    return NotFound("Cita no encontrada.");
                }

                if (Appointment.DoctorId != currentDoctor.Id)
                {
                    return Forbid("No tiene permiso para prescribir en esta cita.");
                }

                Prescription.AppointmentId = Appointment.Id;
                Prescription.PatientId = Appointment.PatientId;
            }
            else
            {
                // --- Scenario: Prescribing without a specific appointment --- 
                // Load all patients for selection
                 var patientsList = await _context.Patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
                 Patients = new SelectList(patientsList, "Id", "FullName");
                 // PatientId will be bound from the form on POST
            }

            // Load medications dropdown (needed in both scenarios)
            var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
            Medications = new SelectList(medicationsList, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (currentDoctor == null) return Forbid();

            // Always assign DoctorId and Date from server-side
            Prescription.DoctorId = currentDoctor.Id;
            Prescription.PrescriptionDate = DateTime.UtcNow;
            
            // Remove navigation properties from model state validation
            ModelState.Remove("Prescription.Appointment");
            ModelState.Remove("Prescription.Medication");
            ModelState.Remove("Prescription.Doctor");
            ModelState.Remove("Prescription.Patient");
            ModelState.Remove("Appointment"); // Remove Appointment model used only for display

            // Validate based on whether it's tied to an appointment or not
            if (Prescription.AppointmentId.HasValue)
            {
                 // --- Scenario: Posting for a specific appointment ---
                Appointment = await _context.Appointments // Reload appointment for validation
                    .AsNoTracking() // No need to track for validation check
                    .FirstOrDefaultAsync(a => a.Id == Prescription.AppointmentId.Value);

                if (Appointment == null)
                {   // Should not happen if OnGet worked, but check anyway
                    ModelState.AddModelError("Prescription.AppointmentId", "La cita asociada ya no existe.");
                }
                else if (Appointment.DoctorId != currentDoctor.Id)
                {   // Security check
                    ModelState.AddModelError("", "No tiene permiso para añadir una prescripción a esta cita.");
                }
                 else
                {   // Ensure PatientId matches the appointment's patient if ID was provided
                    Prescription.PatientId = Appointment.PatientId;
                    // No need to validate PatientId separately here as it comes from the Appointment
                }
            }
            else 
            {   
                // --- Scenario: Posting without a specific appointment ---
                // PatientId is already marked as [Required] in the model, 
                // so ModelState.IsValid will check if it was selected from the dropdown.
                 ModelState.Remove("Prescription.AppointmentId"); // No AppointmentId to validate
            }

            if (!ModelState.IsValid)
            {
                // Reload dropdowns needed for the form
                var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
                Medications = new SelectList(medicationsList, "Id", "Name", Prescription.MedicationId);

                if (!Prescription.AppointmentId.HasValue)
                { // Reload patients only if it wasn't tied to an appointment
                     var patientsList = await _context.Patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
                     Patients = new SelectList(patientsList, "Id", "FullName", Prescription.PatientId);
                }
                 else if (Appointment == null && Prescription.AppointmentId.HasValue) {
                     // If the original appointment is now gone, we might need to handle this state
                     // maybe clear the AppointmentId? For now, just let validation error show.
                 }

                // If originally linked to an appointment, reload it for display
                if (Appointment != null) { /* Appointment already loaded or reloaded above */ }
                 else if(Prescription.AppointmentId.HasValue) {
                      // Attempt to reload if needed for display (e.g., show patient name from appt)
                      Appointment = await _context.Appointments.Include(a=>a.Patient).AsNoTracking().FirstOrDefaultAsync(a=> a.Id == Prescription.AppointmentId.Value);
                 }

                return Page(); // Return page with validation errors
            }

            _context.Prescriptions.Add(Prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescripción creada exitosamente.";

            // Redirect intelligently: back to appointment details if came from there, otherwise to agenda
            if (Prescription.AppointmentId.HasValue)
            {
                return RedirectToPage("/Appointments/Details", new { id = Prescription.AppointmentId.Value });
            }
            else
            {
                return RedirectToPage("./Agenda");
            }
        }
    }
}