using CitasEPS.Data;
using CitasEPS.Models;
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
    [Authorize(Roles = "Patient")]
    public class ReviewDoctorProposalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ReviewDoctorProposalModel> _logger;

        public ReviewDoctorProposalModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ReviewDoctorProposalModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Appointment ID

        public Appointment AppointmentToReview { get; set; } = default!;
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
                                    .FirstOrDefaultAsync(a => a.Id == Id);

            if (appointment == null || appointment.Patient?.Email != user.Email || !appointment.DoctorProposedReschedule || !appointment.ProposedNewDateTime.HasValue || appointment.IsCompleted || appointment.IsCancelled)
            {
                TempData["ErrorMessage"] = "No se pudo confirmar la propuesta. La cita ya no es válida o no tiene permiso.";
                return RedirectToPage("./Index");
            }

            // Update Appointment
            appointment.AppointmentDateTime = appointment.ProposedNewDateTime.Value; // Already UTC
            appointment.IsConfirmed = true;
            appointment.DoctorProposedReschedule = false;
            appointment.ProposedNewDateTime = null;

            _context.Attach(appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient {user.Email} confirmed doctor's reschedule proposal for Appointment {Id} to {appointment.AppointmentDateTime}.");
                TempData["SuccessMessage"] = "Nuevo horario confirmado exitosamente.";
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