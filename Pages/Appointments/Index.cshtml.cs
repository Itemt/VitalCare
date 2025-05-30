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
        private readonly INotificationService _notificationService;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger, IAppointmentPolicyService appointmentPolicyService, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _appointmentPolicyService = appointmentPolicyService;
            _notificationService = notificationService;
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

                // --- FIX: Find Patient record using UserId ---
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id); // Link User and Patient via UserId
                if (patient != null)
                {
                    _logger.LogInformation($"Found matching Patient record (ID: {patient.Id}) for User {user.UserName} using UserId.");
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

                    // Attempt to create missing Patient record automatically
                    try
                    {
                        _logger.LogInformation($"Attempting to create missing Patient record for user {user.Email} (ID: {user.Id}) on Index page");
                        
                        var newPatient = new Patient
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName ?? "Sin nombre",
                            LastName = user.LastName ?? "Sin apellido", 
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber,
                            DateOfBirth = user.DateOfBirth,
                            DocumentId = user.DocumentId,
                            Gender = user.Gender ?? Models.Enums.Gender.Otro
                        };
                        
                        _context.Patients.Add(newPatient);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Successfully created Patient record for user {user.Email} (ID: {user.Id}) on Index page");
                        
                        // Now load appointments for the newly created patient
                        Appointment = await _context.Appointments
                            .Where(a => a.PatientId == newPatient.Id)
                            .Include(a => a.Doctor)
                                .ThenInclude(d => d.Specialty)
                            .OrderBy(a => a.AppointmentDateTime)
                            .ToListAsync();
                        
                        PatientWeeklyStats = _appointmentPolicyService.GetPatientWeeklyAppointmentStats(newPatient.Id, DateTime.Today);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to auto-create Patient record for user {user.Email} (ID: {user.Id}) on Index page");
                        Appointment = new List<Appointment>(); // Ensure list is initialized empty
                        TempData["ErrorMessage"] = "No se pudo encontrar su registro de paciente asociado. Por favor, contacte a soporte.";
                    }
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

            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage();
            }

            // Verify the appointment belongs to the current patient
            var patientRecord = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
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

                // --- START: Notify Doctor of Reschedule Request ---
                if (appointment.Doctor?.User != null)
                {
                    var patientName = patientRecord?.FullName ?? user.Email;
                    var doctorMessage = $"El paciente {patientName} ha solicitado reagendar la cita del {appointment.AppointmentDateTime:dd/MM/yyyy HH:mm}.";
                    await _notificationService.CreateNotificationAsync(appointment.Doctor.User.Id, doctorMessage, NotificationType.RescheduleRequestedByPatient, appointment.Id);
                }
                else
                {
                     _logger.LogWarning($"No se pudo notificar al doctor sobre la solicitud de reagendamiento para la cita {appointment.Id} porque el usuario del doctor no fue encontrado.");
                }
                // --- END: Notify Doctor of Reschedule Request ---
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

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
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

            var appointmentToCancel = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User) // Ensure Doctor and User are loaded for notification
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

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
                TempData["SuccessMessage"] = $"La cita con ID {appointmentToCancel.Id} ha sido cancelada exitosamente.";
                _logger.LogInformation($"Patient {patient.Email} (ID: {patient.Id}) cancelled Appointment ID {appointmentToCancel.Id}.");

                // --- START: Notify Doctor of Cancellation by Patient ---
                if (appointmentToCancel.Doctor?.User != null)
                {
                    var patientName = patient?.FullName ?? user.Email; // Use patient from current context
                    var doctorMessage = $"La cita con el paciente {patientName} programada para el {appointmentToCancel.AppointmentDateTime:dd/MM/yyyy HH:mm} ha sido cancelada por el paciente.";
                    await _notificationService.CreateNotificationAsync(appointmentToCancel.Doctor.User.Id, doctorMessage, NotificationType.AppointmentCancelled, appointmentToCancel.Id);
                }
                else
                {
                    _logger.LogWarning($"No se pudo notificar al doctor sobre la cancelación (por paciente) de la cita {appointmentToCancel.Id} porque el usuario del doctor no fue encontrado.");
                }
                // --- END: Notify Doctor of Cancellation by Patient ---

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
