using CitasEPS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitasEPS.Models;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")] // Solo los doctores pueden acceder
    public class AgendaModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AgendaModel> _logger;

        public AgendaModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<AgendaModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<Models.Appointment> Appointments { get; set; } = new List<Models.Appointment>();
        public Models.Doctor? CurrentDoctor { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // Encontrar el registro Doctor asociado con el User logueado (por email)
            CurrentDoctor = await _context.Doctors
                                        .FirstOrDefaultAsync(d => d.Email == user.Email);

            if (CurrentDoctor == null)
            {
                // Considerar qué hacer si un User con rol Doctor no tiene un registro Doctor asociado
                 return NotFound("Registro de Doctor no encontrado para este usuario.");
                 // O quizás redirigir a una página de error o perfil incompleto
            }

            // Obtener las citas asignadas a este doctor, incluyendo datos del paciente
            IQueryable<Models.Appointment> query = _context.Appointments.AsQueryable();

            // Filter by current doctor
            query = query.Where(a => a.DoctorId == CurrentDoctor.Id);

            if (SelectedDate.HasValue)
            {
                _logger.LogInformation($"Filtering agenda for doctor {CurrentDoctor.Id} for date {SelectedDate.Value.ToShortDateString()}");
                DateTime startDate = SelectedDate.Value.Date;
                DateTime endDate = startDate.AddDays(1);
                query = query.Where(a => a.AppointmentDateTime >= startDate && a.AppointmentDateTime < endDate);
            }
            else
            {
                _logger.LogInformation($"Loading all appointments for doctor {CurrentDoctor.Id}");
            }

            Appointments = await query
                .Include(a => a.Patient) // Include Patient data here
                .OrderBy(a => a.AppointmentDateTime) // Ordenar por fecha/hora
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuario no autenticado.";
                return RedirectToPage();
            }

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (currentDoctor == null)
            {
                TempData["ErrorMessage"] = "Perfil de doctor no encontrado.";
                return RedirectToPage();
            }

            var appointmentToCancel = await _context.Appointments.FindAsync(appointmentId);

            if (appointmentToCancel == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage();
            }

            if (appointmentToCancel.DoctorId != currentDoctor.Id)
            {
                TempData["ErrorMessage"] = "No está autorizado para cancelar esta cita.";
                return RedirectToPage();
            }

            if (appointmentToCancel.IsCompleted)
            {
                TempData["ErrorMessage"] = "No se puede cancelar una cita que ya ha sido completada.";
                return RedirectToPage();
            }

            if (appointmentToCancel.IsCancelled)
            {
                TempData["InfoMessage"] = "Esta cita ya se encuentra cancelada.";
                return RedirectToPage();
            }
            
            appointmentToCancel.IsCancelled = true;
            appointmentToCancel.IsConfirmed = false; 
            appointmentToCancel.RescheduleRequested = false; 
            appointmentToCancel.ProposedNewDateTime = null; 

            try
            {
                _context.Appointments.Update(appointmentToCancel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"La cita ID {appointmentToCancel.Id} ha sido cancelada exitosamente.";
                _logger.LogInformation($"Doctor {currentDoctor.Email} cancelled Appointment ID {appointmentToCancel.Id}.");
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
    }
} 