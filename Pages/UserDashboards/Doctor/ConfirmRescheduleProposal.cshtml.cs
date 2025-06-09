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
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class ConfirmRescheduleProposalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ConfirmRescheduleProposalModel> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAppointmentEmailService _appointmentEmailService;

        public ConfirmRescheduleProposalModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ConfirmRescheduleProposalModel> logger, INotificationService notificationService, IAppointmentEmailService appointmentEmailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
            _appointmentEmailService = appointmentEmailService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Appointment ID

        public Appointment? AppointmentToConfirm { get; set; }
        public string PatientName { get; set; } = "Paciente";
        public string CurrentDateTime { get; set; } = "No disponible";
        public string ProposedDateTime { get; set; } = "No disponible";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            AppointmentToConfirm = await _context.Appointments
                                        .Include(a => a.Patient)
                                        .FirstOrDefaultAsync(a => a.Id == Id);

            if (AppointmentToConfirm == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage("./Agenda");
            }
            
            // Verify Doctor ownership if necessary (e.g., using user email to find Doctor record)
            var doctorRecord = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if(doctorRecord == null || AppointmentToConfirm.DoctorId != doctorRecord.Id)
            {
                TempData["ErrorMessage"] = "No está autorizado para gestionar esta cita.";
                return RedirectToPage("./Agenda");
            }

            // Check if eligible for confirmation (Original date must be in the future)
            if (!AppointmentToConfirm.RescheduleRequested || !AppointmentToConfirm.ProposedNewDateTime.HasValue || AppointmentToConfirm.IsCompleted || AppointmentToConfirm.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Esta cita no está pendiente de confirmación de reagendamiento.";
                return RedirectToPage("./Agenda");
            }

            PatientName = AppointmentToConfirm.Patient?.FullName ?? "Paciente Desconocido";
            
            // Mostrar fechas en hora de Colombia (UTC-5) con formato AM/PM
            var currentColombia = ColombiaTimeZoneService.ConvertUtcToColombia(AppointmentToConfirm.AppointmentDateTime);
            var proposedColombia = ColombiaTimeZoneService.ConvertUtcToColombia(AppointmentToConfirm.ProposedNewDateTime.Value);
            CurrentDateTime = currentColombia.ToString("dd/MM/yyyy hh:mm tt");
            ProposedDateTime = proposedColombia.ToString("dd/MM/yyyy hh:mm tt");

            return Page();
        }

        // Handler for Confirming the proposal
        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var appointmentToUpdate = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == Id);
            if (appointmentToUpdate == null) return NotFound();

            // Re-verify eligibility and ownership before confirming
             var user = await _userManager.GetUserAsync(User);
             if (user == null) return Challenge();
             var doctorRecord = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
             if(doctorRecord == null || appointmentToUpdate.DoctorId != doctorRecord.Id) return Forbid();

             if (!appointmentToUpdate.RescheduleRequested || !appointmentToUpdate.ProposedNewDateTime.HasValue || appointmentToUpdate.IsCompleted || appointmentToUpdate.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "La cita ya no se puede confirmar.";
                return RedirectToPage("./Agenda");
            }

            // Apply changes
            appointmentToUpdate.AppointmentDateTime = appointmentToUpdate.ProposedNewDateTime.Value;
            appointmentToUpdate.IsConfirmed = true;
            appointmentToUpdate.RescheduleRequested = false;
            appointmentToUpdate.ProposedNewDateTime = null;

             _context.Attach(appointmentToUpdate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Doctor {user.Email} confirmed reschedule for Appointment {Id} to {appointmentToUpdate.AppointmentDateTime}");
                TempData["SuccessMessage"] = "Reagendamiento confirmado exitosamente.";

                // --- Notificar al paciente que su propuesta fue aceptada ---
                if (appointmentToUpdate.Patient?.User != null)
                {
                    var doctorName = doctorRecord?.FullName ?? user.Email;
                    
                    // Usar el servicio de zona horaria para formatear la fecha en hora de Colombia
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointmentToUpdate.AppointmentDateTime, "dd/MM/yyyy 'a las' HH:mm");
                    
                    var patientMessage = $"El Dr. {doctorName} ha aceptado su propuesta de reagendamiento. Su nueva cita es el {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointmentToUpdate.Patient.User.Id, patientMessage, NotificationType.RescheduleAcceptedByDoctor, appointmentToUpdate.Id);
                    
                    // Enviar correo de aprobación al paciente
                    try
                    {
                        _logger.LogInformation($"Sending reschedule approved email to patient {appointmentToUpdate.Patient.User.Email}");
                        await _appointmentEmailService.SendRescheduleApprovedEmailAsync(appointmentToUpdate, appointmentToUpdate.Patient.User, user);
                        _logger.LogInformation($"Reschedule approved email sent successfully to patient {appointmentToUpdate.Patient.User.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending reschedule approved email to patient {PatientEmail}", appointmentToUpdate.Patient.User.Email);
                    }
                }
                // --- Fin notificación al paciente ---
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming reschedule for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al confirmar el reagendamiento.";
            }
            return RedirectToPage("./Agenda");
        }

        // Handler for Rejecting the proposal
        public async Task<IActionResult> OnPostRejectAsync()
        {
            var appointmentToUpdate = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == Id);
            if (appointmentToUpdate == null) return NotFound();
            
            // Re-verify eligibility and ownership before rejecting
             var user = await _userManager.GetUserAsync(User);
             if (user == null) return Challenge();
             var doctorRecord = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
             if(doctorRecord == null || appointmentToUpdate.DoctorId != doctorRecord.Id) return Forbid();

             if (!appointmentToUpdate.RescheduleRequested || !appointmentToUpdate.ProposedNewDateTime.HasValue || appointmentToUpdate.IsCompleted || appointmentToUpdate.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "La cita ya no se puede rechazar.";
                return RedirectToPage("./Agenda");
            }

            // Clear flags, keep original time
            appointmentToUpdate.IsConfirmed = false; // Remains unconfirmed
            appointmentToUpdate.RescheduleRequested = false;
            appointmentToUpdate.ProposedNewDateTime = null;

            _context.Attach(appointmentToUpdate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                 _logger.LogInformation($"Doctor {user.Email} rejected reschedule proposal for Appointment {Id}");
                TempData["SuccessMessage"] = "Propuesta de reagendamiento rechazada. La cita permanece con su horario original y estado pendiente.";

                // --- Notificar al paciente que su propuesta fue rechazada ---
                if (appointmentToUpdate.Patient?.User != null)
                {
                    var doctorName = doctorRecord?.FullName ?? user.Email;
                    
                    // Usar el servicio de zona horaria para formatear la fecha en hora de Colombia
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointmentToUpdate.AppointmentDateTime, "dd/MM/yyyy 'a las' HH:mm");
                    
                    var patientMessage = $"El Dr. {doctorName} ha rechazado su propuesta de reagendamiento. Su cita mantiene el horario original: {appointmentFormatted}.";
                    await _notificationService.CreateNotificationAsync(appointmentToUpdate.Patient.User.Id, patientMessage, NotificationType.RescheduleRejectedByDoctor, appointmentToUpdate.Id);
                    
                    // Enviar correo de rechazo al paciente
                    try
                    {
                        _logger.LogInformation($"Sending reschedule rejected email to patient {appointmentToUpdate.Patient.User.Email}");
                        await _appointmentEmailService.SendRescheduleRejectedEmailAsync(appointmentToUpdate, appointmentToUpdate.Patient.User, user, appointmentToUpdate.AppointmentDateTime);
                        _logger.LogInformation($"Reschedule rejected email sent successfully to patient {appointmentToUpdate.Patient.User.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending reschedule rejected email to patient {PatientEmail}", appointmentToUpdate.Patient.User.Email);
                    }
                }
                // --- Fin notificación al paciente ---
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting reschedule for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al rechazar la propuesta.";
            }
            return RedirectToPage("./Agenda");
        }
    }
} 




