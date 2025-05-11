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
using CitasEPS.Services;

namespace CitasEPS.Pages.Appointments
{
    [Authorize] // Ensure only logged-in users can access
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppointmentPolicyService _appointmentPolicyService;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger, IAppointmentPolicyService appointmentPolicyService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _appointmentPolicyService = appointmentPolicyService;
        }

        public IList<Appointment> Appointment { get; set; } = new List<Appointment>();
        public string UserRole { get; set; } = string.Empty; // To know which view to tailor
        public AppointmentStats? PatientWeeklyStats { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found.");
                return Challenge(); // Or redirect to login
            }

            _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) accessing Appointment Index.");

            if (await _userManager.IsInRoleAsync(user, "Paciente"))
            {
                UserRole = "Paciente";
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

                    PatientWeeklyStats = _appointmentPolicyService.GetPatientWeeklyAppointmentStats(patient.Id, DateTime.Today);
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

        public async Task<IActionResult> OnPostRequestRescheduleAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !(await _userManager.IsInRoleAsync(user, "Paciente")))
            {
                TempData["ErrorMessage"] = "Acción no autorizada.";
                return RedirectToPage();
            }

            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage();
            }

            // Verify the appointment belongs to the current patient
            var patientRecord = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (patientRecord == null || appointment.PatientId != patientRecord.Id)
            {
                TempData["ErrorMessage"] = "No tiene permiso para modificar esta cita.";
                return RedirectToPage();
            }

            if (appointment.IsCompleted || appointment.AppointmentDateTime < DateTime.Now || appointment.RescheduleRequested)
            {
                TempData["ErrorMessage"] = "Esta cita no puede ser reagendada en este momento.";
                return RedirectToPage();
            }

            appointment.RescheduleRequested = true;
            _context.Attach(appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Solicitud de reagendamiento enviada correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.Id))
                {
                    TempData["ErrorMessage"] = "Error: La cita ya no existe.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error de concurrencia al guardar la solicitud.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la solicitud de reagendamiento para la cita {AppointmentId}", appointment.Id);
                TempData["ErrorMessage"] = "Ocurrió un error al procesar su solicitud.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Ensure the user is a patient for this action
            if (!await _userManager.IsInRoleAsync(user, "Paciente"))
            {
                TempData["ErrorMessage"] = "Acción no autorizada.";
                return RedirectToPage();
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Perfil de paciente no encontrado.";
                return RedirectToPage();
            }

            // <<< START: Cancellation Limit Check for Patients >>>
            if (!_appointmentPolicyService.CanPatientCancelAppointment(patient.Id, out string reason))
            {
                TempData["ErrorMessage"] = reason;
                return RedirectToPage(); 
            }
            // <<< END: Cancellation Limit Check for Patients >>>

            var appointmentToCancel = await _context.Appointments.FindAsync(appointmentId);

            if (appointmentToCancel == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage();
            }

            // Authorization: Ensure the appointment belongs to this patient
            if (appointmentToCancel.PatientId != patient.Id)
            {
                TempData["ErrorMessage"] = "No está autorizado para cancelar esta cita.";
                return RedirectToPage();
            }

            // Eligibility checks
            if (appointmentToCancel.IsCompleted)
            {
                TempData["ErrorMessage"] = "No se puede cancelar una cita que ya ha sido completada.";
                return RedirectToPage();
            }
            if (appointmentToCancel.AppointmentDateTime < DateTime.Now)
            {
                 TempData["ErrorMessage"] = "No se puede cancelar una cita que ya ha pasado."; // Patients likely shouldn't cancel past appointments
                 return RedirectToPage();
            }
            if (appointmentToCancel.IsCancelled)
            {
                TempData["InfoMessage"] = "Esta cita ya se encuentra cancelada.";
                return RedirectToPage();
            }

            // Perform cancellation
            appointmentToCancel.IsCancelled = true;
            appointmentToCancel.IsConfirmed = false;
            appointmentToCancel.RescheduleRequested = false;
            appointmentToCancel.DoctorProposedReschedule = false;
            appointmentToCancel.ProposedNewDateTime = null;

            try
            {
                _context.Appointments.Update(appointmentToCancel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"La cita ID {appointmentToCancel.Id} ha sido cancelada exitosamente.";
                _logger.LogInformation($"Patient {user.Email} cancelled Appointment ID {appointmentToCancel.Id}.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency error cancelling Appointment ID {AppointmentId}", appointmentToCancel.Id);
                TempData["ErrorMessage"] = "Error de concurrencia al intentar cancelar la cita. Por favor, intente de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling Appointment ID {AppointmentId}", appointmentToCancel.Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cancelar la cita.";
            }

            return RedirectToPage();
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}
