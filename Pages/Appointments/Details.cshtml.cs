using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CitasEPS.Services;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Paciente,Admin,Doctor")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IAppointmentPolicyService _appointmentPolicyService;
        private readonly IAppointmentEmailService _appointmentEmailService;
        private readonly INotificationService _notificationService;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger, UserManager<User> userManager, IAppointmentPolicyService appointmentPolicyService, IAppointmentEmailService appointmentEmailService, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _appointmentPolicyService = appointmentPolicyService;
            _appointmentEmailService = appointmentEmailService;
            _notificationService = notificationService;
        }

        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Intento de ver detalles de cita sin ID.");
                return NotFound();
            }

            // Obtener la cita incluyendo Paciente, Médico, Especialidad y Prescripciones
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialty)
                .Include(a => a.Patient)
                .Include(a => a.Prescriptions)
                    .ThenInclude(p => p.Medication)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                 _logger.LogWarning("No se encontró cita con ID: {AppointmentId} para ver detalles.", id);
                return NotFound();
            }

            Appointment = appointment;
            _logger.LogInformation("Mostrando detalles para cita ID: {AppointmentId}", id);
            return Page();
        }

        // Handler to save clinical notes
        public async Task<IActionResult> OnPostSaveNotesAsync(int id, string? clinicalNotes)
        {
             _logger.LogInformation("Intentando guardar notas clínicas para cita ID: {AppointmentId}", id);
            var appointmentToUpdate = await _context.Appointments.FindAsync(id);

            if (appointmentToUpdate == null)
            {
                _logger.LogWarning("No se encontró cita con ID: {AppointmentId} al intentar guardar notas.", id);
                return NotFound();
            }

            // Basic authorization check (should be redundant with [Authorize] but good practice)
            // Optional: Verify the logged-in doctor is the assigned doctor if needed

            appointmentToUpdate.ClinicalNotes = clinicalNotes?.Trim(); // Trim whitespace

            try
            {   
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notas clínicas guardadas exitosamente para cita ID: {AppointmentId}", id);
                TempData["SuccessMessage"] = "Notas clínicas guardadas exitosamente.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al guardar notas para cita ID: {AppointmentId}", id);
                 TempData["ErrorMessage"] = "Error: La cita fue modificada por otro usuario. Intente de nuevo.";
                 // Optionally reload data or handle differently
            }
            catch (Exception ex)
            {   
                _logger.LogError(ex, "Error guardando notas clínicas para cita ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al guardar las notas clínicas.";
            }

            return RedirectToPage(new { id = id }); // Redirect back to details
        }

        // Nuevo método para confirmar la cita
        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var appointmentToUpdate = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointmentToUpdate == null)
            {
                return NotFound();
            }

            // Solo permitir confirmar si no está ya confirmada o completada
            if (!appointmentToUpdate.IsConfirmed && !appointmentToUpdate.IsCompleted)
            {
                appointmentToUpdate.IsConfirmed = true;
                await _context.SaveChangesAsync();

                try
                {
                    // Enviar notificación al paciente
                    if (appointmentToUpdate.Patient?.User != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            appointmentToUpdate.Patient.User.Id,
                            $"Su cita para el {appointmentToUpdate.AppointmentDateTime:dd/MM/yyyy 'a las' HH:mm} ha sido confirmada por el médico.",
                            NotificationType.AppointmentConfirmed,
                            appointmentToUpdate.Id
                        );

                        // Enviar correo de confirmación al paciente
                        if (appointmentToUpdate.Doctor?.User != null)
                        {
                            _logger.LogInformation($"Enviando correo de confirmación al paciente {appointmentToUpdate.Patient.User.Email}");
                            await _appointmentEmailService.SendAppointmentConfirmedEmailAsync(
                                appointmentToUpdate, 
                                appointmentToUpdate.Patient.User, 
                                appointmentToUpdate.Doctor.User
                            );
                            _logger.LogInformation($"Correo de confirmación enviado exitosamente al paciente {appointmentToUpdate.Patient.User.Email}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar notificación o correo de confirmación para la cita {AppointmentId}.", id);
                }

                TempData["SuccessMessage"] = "Cita confirmada exitosamente.";
            }
            else
            {
                TempData["WarningMessage"] = "La cita ya está confirmada o completada.";
            }

            return RedirectToPage(new { id = id }); // Recargar la misma página
        }

        // Nuevo método para completar la cita
        public async Task<IActionResult> OnPostCompleteAsync(int id)
        {
            var appointmentToUpdate = await _context.Appointments.FindAsync(id);

            if (appointmentToUpdate == null)
            {
                return NotFound();
            }

            // Solo permitir completar si está confirmada pero no completada
            if (appointmentToUpdate.IsConfirmed && !appointmentToUpdate.IsCompleted)
            {
                appointmentToUpdate.IsCompleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita marcada como completada exitosamente.";
            }
            else if (!appointmentToUpdate.IsConfirmed)
            {
                 TempData["WarningMessage"] = "Debe confirmar la cita antes de completarla.";
            }
            else // Ya está completada
            {
                TempData["WarningMessage"] = "La cita ya está marcada como completada.";
            }


            return RedirectToPage(new { id = id }); // Recargar la misma página
        }

        // NUEVO: Método para solicitar reagendamiento por el paciente
        public async Task<IActionResult> OnPostRequestRescheduleAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            // Solo un paciente puede solicitar reagendamiento y si la cita no está completada
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Si el usuario no se encuentra (aunque [Authorize] debería prevenir esto)
                return Challenge(); // O manejar como error apropiado
            }
            if (currentUser.Id == appointment.PatientId && !appointment.IsCompleted)
            {
                appointment.RescheduleRequested = true;
                appointment.IsConfirmed = false; // Requiere nueva confirmación del doctor
                await _context.SaveChangesAsync();
            }
            // Si no es paciente o la cita está completada, no hace nada, solo redirige

            return RedirectToPage(new { id = id });
        }

        // NUEVO: Handler para marcar como que el paciente no se presentó
        public async Task<IActionResult> OnPostMarkNoShowAsync(int id)
        {
            var appointmentToUpdate = await _context.Appointments.FindAsync(id);
            if (appointmentToUpdate == null)
            {
                return NotFound();
            }

            // Verificar autorización (si es necesario verificar que es el doctor de la cita)
            var user = await _userManager.GetUserAsync(User);
            var doctorRecord = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctorRecord == null || appointmentToUpdate.DoctorId != doctorRecord.Id)
            {
                TempData["ErrorMessage"] = "Acción no autorizada.";
                return RedirectToPage(new { id = id }); 
            }

            // Solo marcar si la fecha ya pasó, está confirmada, pero no completada ni ya marcada como NoShow
            if (appointmentToUpdate.AppointmentDateTime < DateTime.Now && 
                appointmentToUpdate.IsConfirmed && 
                !appointmentToUpdate.IsCompleted && 
                !appointmentToUpdate.WasNoShow)
            {
                appointmentToUpdate.WasNoShow = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita marcada como 'No Presentó'.";
            }
            else
            {
                TempData["WarningMessage"] = "La cita no cumple los requisitos para ser marcada como 'No Presentó'.";
            }

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int id)
        {
            _logger.LogInformation($"Attempting to cancel appointment ID {id} from Details page.");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // <<< START: Cancellation Limit Check for Patients >>>
            if (User.IsInRole("Paciente"))
            {
                var patientRecord = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patientRecord != null)
                {
                    if (!_appointmentPolicyService.CanPatientCancelAppointment(patientRecord.Id, out string reason))
                    {
                        TempData["ErrorMessage"] = reason;
                        // Redirect to the same page to show the message, or to Index
                        return RedirectToPage(new { id = id }); 
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No se encontró su registro de paciente.";
                    return RedirectToPage("./Index");
                }
            }
            // <<< END: Cancellation Limit Check for Patients >>>

            var appointmentToCancel = await _context.Appointments
                                                .Include(a => a.Patient) // Needed for patient check
                                                .Include(a => a.Doctor)  // Needed for doctor check
                                                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointmentToCancel == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage("./Index"); // Redirect to index if appointment doesn't exist
            }

            // Authorization Check: Allow Patient or Doctor assigned to the appointment (or Admin)
            bool isAuthorized = false;
            if (User.IsInRole("Admin"))
            {
                isAuthorized = true;
                _logger.LogInformation($"Admin '{user.Email}' authorized to cancel appointment {id}.");
            }
            else if (User.IsInRole("Paciente"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
                if (patient != null && appointmentToCancel.PatientId == patient.Id)
                {
                    isAuthorized = true;
                     _logger.LogInformation($"Patient '{user.Email}' authorized to cancel their appointment {id}.");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (doctor != null && appointmentToCancel.DoctorId == doctor.Id)
                {
                    isAuthorized = true;
                     _logger.LogInformation($"Doctor '{user.Email}' authorized to cancel their assigned appointment {id}.");
                }
            }

            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "No está autorizado para cancelar esta cita.";
                 _logger.LogWarning($"User '{user.Email}' failed authorization to cancel appointment {id}.");
                // Redirect back to details page if they could view it, otherwise index.
                return RedirectToPage(new { id = id });
            }

            // Eligibility checks
            if (appointmentToCancel.IsCompleted)
            {
                TempData["ErrorMessage"] = "No se puede cancelar una cita que ya ha sido completada.";
                return RedirectToPage(new { id = id });
            }
            // Allow cancelling past appointments from details? Maybe, unlike the index page. Let's allow it for now.
            // if (appointmentToCancel.AppointmentDateTime < DateTime.Now) { ... }
            if (appointmentToCancel.IsCancelled)
            {
                TempData["InfoMessage"] = "Esta cita ya se encuentra cancelada.";
                return RedirectToPage(new { id = id });
            }

            // Perform cancellation
            appointmentToCancel.IsCancelled = true;
            appointmentToCancel.IsConfirmed = false;
            appointmentToCancel.RescheduleRequested = false;
            appointmentToCancel.DoctorProposedReschedule = false;
            appointmentToCancel.ProposedNewDateTime = null;
            // appointmentToCancel.WasNoShow = false; // Optional: Clear NoShow if cancelled

            try
            {
                _context.Appointments.Update(appointmentToCancel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"La cita ID {appointmentToCancel.Id} ha sido cancelada exitosamente.";
                 _logger.LogInformation($"Appointment ID {appointmentToCancel.Id} cancelled successfully by user {user.Email}.");
                // After cancelling from details, redirect to the appropriate index/agenda
                if (User.IsInRole("Doctor")) return RedirectToPage("/Doctor/Agenda");
                else return RedirectToPage("./Index");

            }
            catch (DbUpdateConcurrencyException ex)
            {
                 _logger.LogWarning(ex, "Concurrency error cancelling Appointment ID {AppointmentId} from Details.", id);
                TempData["ErrorMessage"] = "Error de concurrencia al intentar cancelar la cita. Por favor, intente de nuevo.";
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error cancelling Appointment ID {AppointmentId} from Details.", id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cancelar la cita.";
            }

            // Redirect back to details if save failed
            return RedirectToPage(new { id = id });
        }
    }
}




