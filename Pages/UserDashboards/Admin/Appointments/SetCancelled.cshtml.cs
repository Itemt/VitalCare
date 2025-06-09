using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using CitasEPS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Admin.Appointments
{
    [Authorize(Roles = "Admin")]
    public class SetCancelledModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<SetCancelledModel> _logger;

        public SetCancelledModel(
            ApplicationDbContext context,
            INotificationService notificationService,
            UserManager<User> userManager,
            ILogger<SetCancelledModel> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public Appointment? Appointment { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            _logger.LogInformation("Admin: Cargando confirmación para MARCAR COMO CANCELADA la Cita ID {AppointmentId}.", id);

            Appointment = await _context.Appointments
                                    .Include(a => a.Patient)
                                    .Include(a => a.Doctor).ThenInclude(d => d.Specialty)
                                    .FirstOrDefaultAsync(m => m.Id == id);

            if (Appointment == null)
            {
                _logger.LogWarning("Admin: Intento de marcar como cancelada Cita ID {AppointmentId} no encontrada (GET).", id);
                TempData["WarningMessage"] = $"La cita con ID {id} no fue encontrada.";
                return RedirectToPage("../ManageAppointments");
            }

            if (Appointment.IsCancelled)
            {
                _logger.LogInformation("Admin: Cita ID {AppointmentId} ya está marcada como cancelada (GET).", id);
                TempData["InfoMessage"] = $"La cita con ID {id} ya se encuentra cancelada.";
                return RedirectToPage("../ManageAppointments");
            }
            
            // Allow viewing confirmation even for past appointments, decision to cancel is on POST
            // if (Appointment.AppointmentDateTime < DateTime.Now)
            // {
            //     _logger.LogWarning("Admin: Intento de marcar como cancelada una cita pasada (ID: {AppointmentId}) desde la página de confirmación (GET).", id);
            //     TempData["ErrorMessage"] = "No se pueden marcar como canceladas citas que ya han ocurrido.";
            //     return RedirectToPage("../ManageAppointments");
            // }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var appointmentToMarkAsCancelled = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User) // Include for notifications
                .Include(a => a.Doctor).ThenInclude(d => d.User)   // Include for notifications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointmentToMarkAsCancelled == null)
            {
                _logger.LogWarning("Admin: Intento de marcar como cancelada Cita ID {AppointmentId} no encontrada (POST).", id);
                TempData["WarningMessage"] = $"La cita con ID {id} ya no existe.";
                return RedirectToPage("../ManageAppointments");
            }

            if (appointmentToMarkAsCancelled.IsCancelled)
            {
                 _logger.LogInformation("Admin: Cita ID {AppointmentId} ya está marcada como cancelada (POST).", id);
                TempData["InfoMessage"] = $"La cita con ID {id} ya se encuentra cancelada.";
                return RedirectToPage("../ManageAppointments");
            }

            // Allow admin to cancel past appointments if necessary (e.g., no-show that needs to be formally cancelled)
            // if (appointmentToMarkAsCancelled.AppointmentDateTime < DateTime.Now)
            // {
            //     _logger.LogWarning("Admin: Intento de confirmar la marcación como cancelada de una cita pasada (ID: {AppointmentId}).", id);
            //     TempData["ErrorMessage"] = "Esta cita ya ha ocurrido y no puede ser marcada como cancelada de esta forma.";
            //     return RedirectToPage("../ManageAppointments");
            // }

            appointmentToMarkAsCancelled.IsCancelled = true;
            appointmentToMarkAsCancelled.IsConfirmed = false; // Ensure it's not confirmed
            // Optionally clear reschedule flags if any
            appointmentToMarkAsCancelled.RescheduleRequested = false;
            appointmentToMarkAsCancelled.DoctorProposedReschedule = false;
            appointmentToMarkAsCancelled.ProposedNewDateTime = null;
            appointmentToMarkAsCancelled.DoctorRescheduleReason = null;


            try
            {
                _context.Appointments.Update(appointmentToMarkAsCancelled);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin: Cita ID {AppointmentId} marcada como CANCELADA exitosamente.", id);
                TempData["SuccessMessage"] = $"Cita con ID {id} marcada como CANCELADA exitosamente.";

                // --- Create Notifications ---
                var patientUser = appointmentToMarkAsCancelled.Patient?.User;
                var doctorUser = appointmentToMarkAsCancelled.Doctor?.User;

                string patientName = appointmentToMarkAsCancelled.Patient?.FullName ?? "Paciente Desconocido";
                string doctorName = appointmentToMarkAsCancelled.Doctor?.FullName ?? "Doctor Desconocido";
                string appointmentDateTime = appointmentToMarkAsCancelled.AppointmentDateTime.ToString("dd/MM/yyyy hh:mm tt");

                if (patientUser != null)
                {
                    var patientMessage = $"Su cita con el Dr. {doctorName} para el {appointmentDateTime} ha sido CANCELADA por un administrador.";
                    await _notificationService.CreateNotificationAsync(patientUser.Id, patientMessage, NotificationType.AppointmentCancelled, appointmentToMarkAsCancelled.Id);
                }
                else
                {
                     _logger.LogWarning("Admin: No se pudo notificar al paciente para la cita ({AppointmentId}) marcada como cancelada porque el usuario del paciente no fue encontrado.", id);
                }

                if (doctorUser != null)
                {
                    var doctorMessage = $"La cita del paciente {patientName} para el {appointmentDateTime} ha sido CANCELADA por un administrador.";
                    await _notificationService.CreateNotificationAsync(doctorUser.Id, doctorMessage, NotificationType.AppointmentCancelled, appointmentToMarkAsCancelled.Id);
                }
                else
                {
                    _logger.LogWarning("Admin: No se pudo notificar al Doctor para la cita ({AppointmentId}) marcada como cancelada porque el usuario del Doctor no fue encontrado.", id);
                }
                // --- End Notifications ---

                return RedirectToPage("../ManageAppointments");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Admin: Error al marcar como CANCELADA la cita ID {AppointmentId}.", id);
                TempData["ErrorMessage"] = "Ocurrió un error al marcar la cita como cancelada. Por favor, inténtelo de nuevo.";
                return RedirectToPage("./Details", new { id = id }); 
            }
        }
    }
} 




