using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Doctor
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

        public async Task<IActionResult> OnGetAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Not logged in

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (currentDoctor == null) return Forbid("El usuario actual no es un Doctor registrado.");

            // Load the appointment
            Appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (Appointment == null)
            {
                return NotFound("Cita no encontrada.");
            }

            if (Appointment.DoctorId != currentDoctor.Id)
            {
                return Forbid("No tiene permiso para prescribir en esta cita.");
            }

            // Validar que la cita no esté completada
            if (Appointment.IsCompleted)
            {
                TempData["ErrorMessage"] = "No se pueden crear prescripciones para citas completadas.";
                return RedirectToPage("/Appointments/Details", new { id = appointmentId });
            }

            // Validar que la cita no esté expirada (pasó la fecha/hora)
            if (Appointment.AppointmentDateTime < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "No se pueden crear prescripciones para citas expiradas.";
                return RedirectToPage("/Appointments/Details", new { id = appointmentId });
            }

            // Validar que la cita no esté cancelada
            if (Appointment.IsCancelled)
            {
                TempData["ErrorMessage"] = "No se pueden crear prescripciones para citas canceladas.";
                return RedirectToPage("/Appointments/Details", new { id = appointmentId });
            }

            // Prepare default prescription details
            Prescription.DoctorId = currentDoctor.Id;
            Prescription.PrescriptionDate = DateTime.UtcNow;
            Prescription.AppointmentId = Appointment.Id;
            Prescription.PatientId = Appointment.PatientId;

            // Load medications dropdown
            var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
            Medications = new SelectList(medicationsList, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (currentDoctor == null) return Forbid();

            // Validar que la cita sigue siendo válida
            Appointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == Prescription.AppointmentId);

            if (Appointment == null)
            {
                ModelState.AddModelError("", "La cita asociada ya no existe.");
            }
            else if (Appointment.DoctorId != currentDoctor.Id)
            {
                ModelState.AddModelError("", "No tiene permiso para añadir una prescripción a esta cita.");
            }
            else if (Appointment.IsCompleted)
            {
                ModelState.AddModelError("", "No se pueden crear prescripciones para citas completadas.");
            }
            else if (Appointment.AppointmentDateTime < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "No se pueden crear prescripciones para citas expiradas.");
            }
            else if (Appointment.IsCancelled)
            {
                ModelState.AddModelError("", "No se pueden crear prescripciones para citas canceladas.");
            }

            // Always assign DoctorId, Date and PatientId from server-side
            Prescription.DoctorId = currentDoctor.Id;
            Prescription.PrescriptionDate = DateTime.UtcNow;
            if (Appointment != null)
            {
                Prescription.PatientId = Appointment.PatientId;
            }
            
            // Remove navigation properties from model state validation
            ModelState.Remove("Prescription.Appointment");
            ModelState.Remove("Prescription.Medication");
            ModelState.Remove("Prescription.Doctor");
            ModelState.Remove("Prescription.Patient");
            ModelState.Remove("Appointment");

            if (!ModelState.IsValid)
            {
                // Reload dropdown and appointment info for the form
                var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
                Medications = new SelectList(medicationsList, "Id", "Name", Prescription.MedicationId);

                if (Appointment == null && Prescription.AppointmentId.HasValue)
                {
                    Appointment = await _context.Appointments
                        .Include(a => a.Patient)
                        .Include(a => a.Doctor)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == Prescription.AppointmentId.Value);
                }

                return Page();
            }

            _context.Prescriptions.Add(Prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescripción creada exitosamente.";
            return RedirectToPage("/Appointments/Details", new { id = Prescription.AppointmentId.Value });
        }
    }
}




