using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using CitasEPS.Services;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Paciente")]
    public class ReviewDoctorProposalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ReviewDoctorProposalModel> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAppointmentEmailService _appointmentEmailService;

        public ReviewDoctorProposalModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ReviewDoctorProposalModel> logger, INotificationService notificationService, IAppointmentEmailService appointmentEmailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
            _appointmentEmailService = appointmentEmailService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Appointment ID

        public Appointment? AppointmentToReview { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public string CurrentDateTime { get; set; } = default!;
        public string ProposedDateTime { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            AppointmentToReview = await _context.Appointments
                                        .Include(a => a.Doctor)
                                        .Include(a => a.Patient) // Verify ownership
                                        .FirstOrDefaultAsync(a => a.Id == Id);

            if (AppointmentToReview == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage("./Index");
            }

            // Verify ownership
            if (AppointmentToReview.Patient?.Email != user.Email)
            {
                TempData["ErrorMessage"] = "No tiene permiso para revisar esta propuesta.";
                return RedirectToPage("./Index");
            }

            // Verify eligibility
            if (!AppointmentToReview.DoctorProposedReschedule || !AppointmentToReview.ProposedNewDateTime.HasValue || AppointmentToReview.IsCompleted || AppointmentToReview.IsCancelled)
            {
                TempData["ErrorMessage"] = "Esta cita no tiene una propuesta de reagendamiento válida del doctor para revisar.";
                return RedirectToPage("./Index");
            }

            DoctorName = AppointmentToReview.Doctor?.FullName ?? "Doctor Desconocido";
            CurrentDateTime = AppointmentToReview.AppointmentDateTime.ToLocalTime().ToString("dd/MM/yyyy h:mm tt"); // Format for display
            ProposedDateTime = AppointmentToReview.ProposedNewDateTime.Value.ToLocalTime().ToString("dd/MM/yyyy h:mm tt"); // Format for display

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appointment = await _context.Appointments
                                    .Include(a => a.Patient)
                                    .ThenInclude(p => p.User)
                                    .Include(a => a.Doctor)
                                    .ThenInclude(d => d.User)
                                    .FirstOrDefaultAsync(a => a.Id == Id);

            if (appointment == null || appointment.Patient?.Email != user.Email || !appointment.DoctorProposedReschedule || !appointment.ProposedNewDateTime.HasValue || appointment.IsCompleted || appointment.IsCancelled)
            {
                TempData["ErrorMessage"] = "No se pudo confirmar la propuesta. La cita ya no es válida o no tiene permiso.";
                return RedirectToPage("./Index");
            }

            // Update Appointment - La cita queda automáticamente confirmada cuando el paciente acepta la propuesta del doctor
            appointment.AppointmentDateTime = appointment.ProposedNewDateTime.Value; // Already UTC
            appointment.IsConfirmed = true; // Automáticamente confirmada según el requerimiento
            appointment.DoctorProposedReschedule = false;
            appointment.ProposedNewDateTime = null;

            _context.Attach(appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient {user.Email} confirmed doctor's reschedule proposal for Appointment {Id} to {appointment.AppointmentDateTime}.");
                TempData["SuccessMessage"] = "Nuevo horario confirmado exitosamente. Su cita está confirmada.";

                // --- Enviar notificaciones y correos ---
                if (appointment.Doctor?.User != null && appointment.Patient?.User != null)
                {
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointment.AppointmentDateTime, "dd/MM/yyyy 'a las' hh:mm tt");
                    
                    // Notificación al doctor de que su propuesta fue aceptada
                    var doctorMessage = $"El paciente {appointment.Patient.FullName} ha aceptado su propuesta de reagendamiento. La cita está confirmada para el {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointment.Doctor.User.Id, doctorMessage, NotificationType.RescheduleAcceptedByPatient, appointment.Id);
                    
                    // Notificación al paciente de confirmación
                    var patientMessage = $"Ha confirmado el reagendamiento propuesto por el Dr. {appointment.Doctor.FullName}. Su cita está confirmada para el {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, patientMessage, NotificationType.AppointmentConfirmed, appointment.Id);
                    
                    // Enviar correo de confirmación al paciente con el nuevo horario
                    try
                    {
                        await _appointmentEmailService.SendAppointmentConfirmedEmailAsync(appointment, appointment.Patient.User, appointment.Doctor.User);
                        _logger.LogInformation($"Confirmation email sent to patient {appointment.Patient.User.Email} for confirmed rescheduled appointment");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending confirmation email to patient {PatientEmail}", appointment.Patient.User.Email);
                    }
                }
                // --- Fin notificaciones y correos ---
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency error confirming doctor proposal for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Error de concurrencia. La cita pudo haber sido modificada. Intente de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming doctor proposal for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al confirmar el nuevo horario.";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostRejectAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appointment = await _context.Appointments
                                    .Include(a => a.Patient)
                                    .ThenInclude(p => p.User)
                                    .Include(a => a.Doctor)
                                    .ThenInclude(d => d.User)
                                    .FirstOrDefaultAsync(a => a.Id == Id);

             if (appointment == null || appointment.Patient?.Email != user.Email || !appointment.DoctorProposedReschedule || !appointment.ProposedNewDateTime.HasValue || appointment.IsCompleted || appointment.IsCancelled)
            {
                TempData["ErrorMessage"] = "No se pudo rechazar la propuesta. La cita ya no es válida o no tiene permiso.";
                return RedirectToPage("./Index");
            }

            // Clear proposal flags, keep original time, IsConfirmed remains false
            appointment.DoctorProposedReschedule = false;
            appointment.ProposedNewDateTime = null;
            // Keep IsConfirmed = false

            _context.Attach(appointment).State = EntityState.Modified;

             try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient {user.Email} rejected doctor's reschedule proposal for Appointment {Id}.");
                TempData["InfoMessage"] = "Propuesta de reagendamiento del doctor rechazada. La cita permanece sin confirmar con su horario original.";

                // --- Enviar notificaciones ---
                if (appointment.Doctor?.User != null && appointment.Patient?.User != null)
                {
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointment.AppointmentDateTime, "dd/MM/yyyy 'a las' hh:mm tt");
                    
                    // Notificación al doctor de que su propuesta fue rechazada
                    var doctorMessage = $"El paciente {appointment.Patient.FullName} ha rechazado su propuesta de reagendamiento. La cita mantiene el horario original: {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointment.Doctor.User.Id, doctorMessage, NotificationType.RescheduleRejectedByPatient, appointment.Id);
                    
                    // Notificación al paciente de confirmación del rechazo
                    var patientMessage = $"Ha rechazado la propuesta de reagendamiento del Dr. {appointment.Doctor.FullName}. Su cita mantiene el horario original: {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, patientMessage, NotificationType.RescheduleRejectedByPatient, appointment.Id);
                }
                // --- Fin notificaciones ---
            }
            catch (DbUpdateConcurrencyException ex)
            {
                 _logger.LogWarning(ex, "Concurrency error rejecting doctor proposal for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Error de concurrencia. La cita pudo haber sido modificada. Intente de nuevo.";
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error rejecting doctor proposal for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al rechazar la propuesta.";
            }

            return RedirectToPage("./Index");
        }
    }
} 




