using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Services;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Admin")] // Or specific policy like "Admin"
    public class DeleteModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(CitasEPS.Data.ApplicationDbContext context, 
                           INotificationService notificationService, 
                           ILogger<DeleteModel> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [BindProperty]
        public Appointment? Appointment { get; set; } = default!;
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the appointment including related data to display confirmation details
            Appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsNoTracking() // No need to track for deletion confirmation
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Appointment == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ErrorMessage = String.Format("Error al eliminar la cita {0}. Intente de nuevo.", id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointmentToDelete = await _context.Appointments.FindAsync(id);

            if (appointmentToDelete == null)
            {
                // Appointment already deleted or never existed
                 TempData["SuccessMessage"] = "La cita ya no existe o fue eliminada.";
                 return RedirectToPage("./Index");
            }

            // Fetch full appointment details for notification purposes BEFORE deleting
            var appointmentToNotify = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointmentToNotify == null)
            {
                // This case should ideally be covered by appointmentToDelete == null check, but good for robustness
                TempData["ErrorMessage"] = "No se pudo encontrar la cita para enviar notificaciones.";
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Appointments.Remove(appointmentToDelete);
                await _context.SaveChangesAsync();

                // --- START: Create Notifications for Cancellation ---
                try
                {
                    string patientName = appointmentToNotify.Patient?.FullName ?? "Paciente Desconocido";
                    string doctorName = appointmentToNotify.Doctor?.FullName ?? "Doctor Desconocido";
                    string appointmentDateTime = appointmentToNotify.AppointmentDateTime.ToString("dd/MM/yyyy HH:mm");

                    // Notification for the Patient
                    if (appointmentToNotify.Patient?.User != null)
                    {
                        var patientMessage = $"Su cita con el Dr. {doctorName} para el {appointmentDateTime} ha sido cancelada por un administrador.";
                        await _notificationService.CreateNotificationAsync(appointmentToNotify.Patient.User.Id, patientMessage, NotificationType.AppointmentCancelled, appointmentToNotify.Id);
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo notificar al paciente para la cita cancelada {appointmentToNotify.Id} porque el usuario del paciente no fue encontrado.");
                    }

                    // Notification for the Doctor
                    if (appointmentToNotify.Doctor?.User != null)
                    {
                        var doctorMessage = $"La cita del paciente {patientName} para el {appointmentDateTime} ha sido cancelada por un administrador.";
                        await _notificationService.CreateNotificationAsync(appointmentToNotify.Doctor.User.Id, doctorMessage, NotificationType.AppointmentCancelled, appointmentToNotify.Id);
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo notificar al doctor para la cita cancelada {appointmentToNotify.Id} porque el usuario del doctor no fue encontrado.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear notificaciones para la cancelación de la cita {AppointmentId}.", appointmentToNotify.Id);
                }
                // --- END: Create Notifications for Cancellation ---

                TempData["SuccessMessage"] = "Cita eliminada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                 return RedirectToAction("./Delete", new { id = id, saveChangesError = true });
            }
        }
    }
} 




