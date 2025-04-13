using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient,Admin,Doctor")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly UserManager<User> _userManager;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger, UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
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
        [Authorize(Roles = "Doctor")]
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
            var appointmentToUpdate = await _context.Appointments.FindAsync(id);

            if (appointmentToUpdate == null)
            {
                return NotFound();
            }

            // Solo permitir confirmar si no está ya confirmada o completada
            if (!appointmentToUpdate.IsConfirmed && !appointmentToUpdate.IsCompleted)
            {
                appointmentToUpdate.IsConfirmed = true;
                await _context.SaveChangesAsync();
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
    }
}