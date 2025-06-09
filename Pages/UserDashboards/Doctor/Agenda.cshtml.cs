using CitasEPS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.Extensions.Logging;
using CitasEPS.Services;
using CitasEPS.Services.Modules.Common;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")] // Solo los doctores pueden acceder
    public class AgendaModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AgendaModel> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAppointmentEmailService _appointmentEmailService;

        public AgendaModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<AgendaModel> logger, INotificationService notificationService, IAppointmentEmailService appointmentEmailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
            _appointmentEmailService = appointmentEmailService;
        }

        public IList<Appointment> Appointments { get; set; } = default!;
        public DoctorModel DoctorInfo { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // Encontrar el registro Doctor asociado con el User logueado (por UserId)
            DoctorInfo = await _context.Doctors
                                        .Include(d => d.User)
                                        .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (DoctorInfo == null)
            {
                // Considerar qué hacer si un User con rol Doctor no tiene un registro Doctor asociado
                 return NotFound("Registro de Doctor no encontrado para este usuario.");
                 // O quizás redirigir a una página de error o perfil incompleto
            }

            // Obtener las citas asignadas a este Doctor, incluyendo datos del paciente
            IQueryable<Appointment> query = _context.Appointments.AsQueryable();

            // Filter by current Doctor
            query = query.Where(a => a.DoctorId == DoctorInfo.Id);

            if (SelectedDate.HasValue)
            {
                _logger.LogInformation($"Filtering agenda for Doctor {DoctorInfo.Id} for date {SelectedDate.Value.ToShortDateString()}");
                DateTime startDate = SelectedDate.Value.Date;
                DateTime endDate = startDate.AddDays(1);
                query = query.Where(a => a.AppointmentDateTime >= startDate && a.AppointmentDateTime < endDate);
            }
            else
            {
                _logger.LogInformation($"Loading all appointments for Doctor {DoctorInfo.Id}");
            }

            Appointments = await query
                .Include(a => a.Patient) // Include Patient data here
                .Include(a => a.Rating) // Include Rating data for Patient ratings
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

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (currentDoctor == null)
            {
                TempData["ErrorMessage"] = "Perfil de Doctor no encontrado.";
                return RedirectToPage();
            }

            var appointmentToCancel = await _context.Appointments
                                                .Include(a => a.Patient).ThenInclude(p => p.User)
                                                .FirstOrDefaultAsync(a => a.Id == appointmentId);

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
                _logger.LogInformation($"Doctor {user.Email} cancelled Appointment ID {appointmentToCancel.Id}.");

                if (appointmentToCancel.Patient?.User != null)
                {
                    var doctorName = currentDoctor?.FullName ?? user.Email;
                    
                    // Usar el servicio de zona horaria para formatear la fecha en hora de Colombia
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointmentToCancel.AppointmentDateTime, "dd/MM/yyyy 'a las' hh:mm tt");
                    
                    var patientMessage = $"Su cita con el Dr. {doctorName} programada para el {appointmentFormatted} ha sido cancelada.";
                    
                    _logger.LogInformation($"[DEBUG] Attempting to create notification for patient {appointmentToCancel.Patient.User.Email} - Message: {patientMessage}");
                    
                    try
                    {
                        await _notificationService.CreateNotificationAsync(appointmentToCancel.Patient.User.Id, patientMessage, NotificationType.AppointmentCancelled, appointmentToCancel.Id);
                        _logger.LogInformation($"[DEBUG] Notification created successfully for patient {appointmentToCancel.Patient.User.Email}");
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, "[DEBUG] Failed to create notification for patient {PatientEmail}", appointmentToCancel.Patient.User.Email);
                    }
                    
                    // Enviar correos de confirmación de cancelación
                    try
                    {
                        _logger.LogInformation($"Sending cancellation email to patient {appointmentToCancel.Patient.User.Email} and doctor {user.Email}");
                        await _appointmentEmailService.SendAppointmentCancelledEmailAsync(appointmentToCancel, appointmentToCancel.Patient.User, user);
                        _logger.LogInformation($"Cancellation email sent successfully to both patient and doctor");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending cancellation email for appointment {AppointmentId}", appointmentToCancel.Id);
                    }
                }
                else
                {
                     _logger.LogWarning($"No se pudo notificar al paciente sobre la cancelación de la cita {appointmentToCancel.Id} por el Doctor {user.Email} porque el usuario del paciente no fue encontrado.");
                }
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





