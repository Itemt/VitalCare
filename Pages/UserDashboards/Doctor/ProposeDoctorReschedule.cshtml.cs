using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Models.Modules.Common;
using CitasEPS.Services;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class ProposeDoctorRescheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProposeDoctorRescheduleModel> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAppointmentPolicyService _appointmentPolicyService;
        private readonly IAppointmentEmailService _appointmentEmailService;

        public ProposeDoctorRescheduleModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ProposeDoctorRescheduleModel> logger, INotificationService notificationService, IAppointmentPolicyService appointmentPolicyService, IAppointmentEmailService appointmentEmailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
            _appointmentPolicyService = appointmentPolicyService;
            _appointmentEmailService = appointmentEmailService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Appointment ID

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public Appointment? CurrentAppointment { get; set; } = default!;
        public string PatientName { get; set; } = default!;
        public string CurrentDateTime { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "La nueva fecha y hora propuesta es requerida.")]
            [Display(Name = "Nueva Fecha y Hora Propuesta")]
            public DateTime ProposedNewDateTime { get; set; }

            [Required(ErrorMessage = "La razón del reagendamiento es requerida.")]
            [Display(Name = "Razón del Reagendamiento")]
            public string? Reason { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // TODO: Load Appointment, verify doctor ownership, check eligibility (future, not completed, not cancelled, no pending patient proposal)
            // TODO: Populate view data (PatientName, CurrentDateTime)
            // TODO: Set initial Input.ProposedNewDateTime suggestion

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor == null) return NotFound("Doctor profile not found.");

            CurrentAppointment = await _context.Appointments
                                        .Include(a => a.Patient)
                                        .FirstOrDefaultAsync(a => a.Id == Id && a.DoctorId == doctor.Id);

            if (CurrentAppointment == null) return NotFound("Appointment not found or not assigned to you.");
            
             // Add eligibility checks here...
             if (CurrentAppointment.IsCompleted || CurrentAppointment.IsCancelled || CurrentAppointment.AppointmentDateTime < DateTime.Now) {
                 TempData["ErrorMessage"] = "No se puede proponer reagendamiento para esta cita.";
                 return RedirectToPage("./Agenda");
             }
             if (CurrentAppointment.RescheduleRequested) { // Patient already requested
                  TempData["ErrorMessage"] = "El paciente ya ha propuesto un reagendamiento para esta cita. Por favor, revise esa propuesta primero.";
                 return RedirectToPage("./Agenda");
             }

             // Verificar límites de reagendamiento
             if (!_appointmentPolicyService.CanRescheduleAppointment(CurrentAppointment.Id, out string appointmentReason))
             {
                 TempData["ErrorMessage"] = appointmentReason;
                 return RedirectToPage("./Agenda");
             }


            PatientName = CurrentAppointment.Patient?.FullName ?? "Paciente Desconocido";
            
            // Mostrar la fecha en hora de Colombia (UTC-5) con formato AM/PM
            var appointmentColombia = ColombiaTimeZoneService.ConvertUtcToColombia(CurrentAppointment.AppointmentDateTime);
            CurrentDateTime = appointmentColombia.ToString("dd/MM/yyyy hh:mm tt");
            
            // Sugerir el día siguiente en hora de Colombia
            var suggestedColombia = appointmentColombia.AddDays(1);
            Input = new InputModel { ProposedNewDateTime = suggestedColombia };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation($"[DEBUG] ProposeDoctorReschedule OnPostAsync started - Appointment ID: {Id}, ProposedNewDateTime: {Input.ProposedNewDateTime}, Reason: {Input.Reason}");
            
            // TODO: Load Appointment, verify doctor ownership, check eligibility
            // TODO: Validate Input.ProposedNewDateTime (past, working hours, patient availability, etc.) - use UTC
            // TODO: Set Appointment.ProposedNewDateTime, DoctorProposedReschedule=true, IsConfirmed=false
            // TODO: Save changes

            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogError($"[DEBUG] User not found in OnPostAsync");
                return Challenge();
            }
            
            _logger.LogInformation($"[DEBUG] User found: {user.Email}");
            
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor == null) 
            {
                _logger.LogError($"[DEBUG] Doctor profile not found for user {user.Email}");
                return NotFound("Doctor profile not found.");
            }
            
            _logger.LogInformation($"[DEBUG] Doctor found: {doctor.FullName} (ID: {doctor.Id})");

            var appointmentToUpdate = await _context.Appointments
                                            .Include(a => a.Patient) // Needed for validation checks potentially
                                                .ThenInclude(p => p.User) // Para notificaciones
                                            .FirstOrDefaultAsync(a => a.Id == Id && a.DoctorId == doctor.Id);

            if (appointmentToUpdate == null) return NotFound("Appointment not found or not assigned to you.");

            // Add eligibility checks again...
             if (appointmentToUpdate.IsCompleted || appointmentToUpdate.IsCancelled || appointmentToUpdate.AppointmentDateTime < DateTime.Now) {
                 TempData["ErrorMessage"] = "No se puede proponer reagendamiento para esta cita.";
                 return RedirectToPage("./Agenda");
             }
             if (appointmentToUpdate.RescheduleRequested) { 
                  TempData["ErrorMessage"] = "El paciente ya ha propuesto un reagendamiento para esta cita.";
                 return RedirectToPage("./Agenda");
             }

            // --- START: Validations ---
            if (appointmentToUpdate.IsCompleted || appointmentToUpdate.IsCancelled)
            {
                TempData["ErrorMessage"] = "No se puede reagendar una cita completada o cancelada.";
                return RedirectToPage("./Agenda");
            }

            // Verificar que aún no se ha propuesto reagendamiento
            if (appointmentToUpdate.RescheduleRequested || appointmentToUpdate.DoctorProposedReschedule)
            {
                TempData["ErrorMessage"] = "Ya existe una propuesta de reagendamiento pendiente para esta cita.";
                return RedirectToPage("./Agenda");
            }

            // Verificar límite de reagendamientos del doctor (máximo 2)
            if (appointmentToUpdate.DoctorRescheduleCount >= 2)
            {
                TempData["ErrorMessage"] = "Ha alcanzado el límite máximo de reagendamientos como doctor para esta cita (2).";
                return RedirectToPage("./Agenda");
            }

            // Verificar que la cita es futura
            if (appointmentToUpdate.AppointmentDateTime <= DateTime.UtcNow.AddHours(1))
            {
                TempData["ErrorMessage"] = "Solo se pueden reagendar citas futuras con al menos 1 hora de anticipación.";
                return RedirectToPage("./Agenda");
            }
            // --- END VALIDATIONS --- 

            // --- START: Convert Input.ProposedNewDateTime from Colombia time to UTC ---
            var originalProposedDateTime = Input.ProposedNewDateTime; // Keep original for validation
            
            // Asumir que la entrada del formulario está en hora de Colombia
            DateTime utcProposedDateTime;
            if (originalProposedDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Tratar como hora de Colombia y convertir a UTC
                utcProposedDateTime = ColombiaTimeZoneService.ConvertColombiaToUtc(originalProposedDateTime);
            }
            else
            {
                // Si ya tiene Kind específico, usar conversión estándar
                utcProposedDateTime = originalProposedDateTime.Kind == DateTimeKind.Local 
                    ? originalProposedDateTime.ToUniversalTime() 
                    : originalProposedDateTime;
            }
            
            _logger.LogInformation($"Input Colombia Time: {originalProposedDateTime:dd/MM/yyyy HH:mm}, Converted UTC: {utcProposedDateTime:dd/MM/yyyy HH:mm}");
            // --- END: Convert Input.ProposedNewDateTime from Colombia time to UTC ---

            // --- START: Additional Validations ---
            if (utcProposedDateTime < DateTime.UtcNow) 
            {
                 ModelState.AddModelError("Input.ProposedNewDateTime", "No puede proponer una fecha u hora en el pasado.");
            }

             // Working Hours Validation (using local time)
            var localAppTime = originalProposedDateTime.TimeOfDay;
            var localAppDay = originalProposedDateTime.DayOfWeek;
            if (localAppDay == DayOfWeek.Saturday || localAppDay == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "Las citas solo pueden agendarse de Lunes a Viernes.");
            }
            else if (localAppTime < new TimeSpan(8, 0, 0) || localAppTime >= new TimeSpan(18, 0, 0)) 
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "Las citas solo pueden agendarse entre las 8:00 AM y las 6:00 PM (la hora de las 6:00 PM es el cierre).");
            }

             // Patient Weekly Limit Validation (using UTC date)
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
            DateTime utcStartDate = utcProposedDateTime.Date;
            while (utcStartDate.DayOfWeek != firstDayOfWeek) { utcStartDate = utcStartDate.AddDays(-1); }
            DateTime utcEndDate = utcStartDate.AddDays(7);
            var appointmentsInWeek = await _context.Appointments
                .Where(a => a.PatientId == appointmentToUpdate.PatientId && 
                            a.Id != Id && 
                            !a.IsCancelled && 
                            a.AppointmentDateTime >= utcStartDate && 
                            a.AppointmentDateTime < utcEndDate)
                .CountAsync();
            if (appointmentsInWeek >= 2)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "El paciente ya tiene 2 citas agendadas para la semana seleccionada (excluyendo citas canceladas).");
            }

            // Doctor Availability Validation (Self - check if already booked at proposed UTC time)
             var doctorHasSlot = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctor.Id && 
                               a.Id != Id && 
                               !a.IsCancelled && 
                               a.AppointmentDateTime == utcProposedDateTime);
            if (doctorHasSlot)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "Usted ya tiene otra cita programada para esta fecha y hora exactas.");
            }
            // --- END: Additional Validations ---

            if (!ModelState.IsValid)
            {
                // Load appointment for return view
                CurrentAppointment = appointmentToUpdate;
                PatientName = appointmentToUpdate.Patient?.FullName ?? "Paciente Desconocido";
                return Page();
            }

            // Update Appointment
            appointmentToUpdate.ProposedNewDateTime = utcProposedDateTime; // Save UTC version
            appointmentToUpdate.DoctorProposedReschedule = true;
            appointmentToUpdate.RescheduleRequested = false; // Clear any previous patient request
            appointmentToUpdate.DoctorRescheduleReason = Input.Reason;
            appointmentToUpdate.DoctorRescheduleCount += 1; // Incrementar contador de reagendamientos del doctor

            _context.Attach(appointmentToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Doctor {user.Email} proposed reschedule for Appointment {Id} to {Input.ProposedNewDateTime}");
                TempData["SuccessMessage"] = "Propuesta de reagendamiento enviada al paciente exitosamente.";

                // --- Notificar al paciente sobre la propuesta del doctor ---
                if (appointmentToUpdate.Patient?.User != null)
                {
                    var doctorName = doctor?.FullName ?? user.Email;
                    
                    // Usar el servicio de zona horaria para formatear las fechas en hora de Colombia
                    var currentAppointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointmentToUpdate.AppointmentDateTime);
                    var proposedAppointmentFormatted = ColombiaTimeZoneService.FormatInColombia(Input.ProposedNewDateTime);
                    
                    var patientMessage = $"El Dr. {doctorName} ha propuesto reagendar su cita del {currentAppointmentFormatted} para el {proposedAppointmentFormatted}. Por favor revise y confirme.";
                    await _notificationService.CreateNotificationAsync(appointmentToUpdate.Patient.User.Id, patientMessage, NotificationType.RescheduleProposedByDoctor, appointmentToUpdate.Id);
                    
                    // Create confirmation notification for the doctor
                    var doctorConfirmationMessage = $"Su propuesta de reagendamiento ha sido enviada al paciente {appointmentToUpdate.Patient.FullName}. Propuso cambiar la cita del {currentAppointmentFormatted} para el {proposedAppointmentFormatted}. Esperando respuesta del paciente.";
                    await _notificationService.CreateNotificationAsync(user.Id, doctorConfirmationMessage, NotificationType.RescheduleProposalSent, appointmentToUpdate.Id);
                    
                    _logger.LogInformation($"Notification sent to patient {appointmentToUpdate.Patient.User.Email}: Current time Colombia: {currentAppointmentFormatted}, Proposed time Colombia: {proposedAppointmentFormatted}");
                    
                    // Enviar correo electrónico al paciente
                    try
                    {
                        await _appointmentEmailService.SendDoctorRescheduleProposedEmailAsync(appointmentToUpdate, appointmentToUpdate.Patient.User, user);
                        _logger.LogInformation($"Reschedule proposal email sent to patient {appointmentToUpdate.Patient.User.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send reschedule proposal email to patient {appointmentToUpdate.Patient.User.Email}");
                        // No fallar el proceso completo si el correo no se envía
                    }
                }
                // --- Fin notificación al paciente ---

                return RedirectToPage("./Agenda");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                 _logger.LogWarning(ex, "Concurrency error proposing reschedule for Appointment {AppointmentId}", Id);
                 TempData["ErrorMessage"] = "Error de concurrencia. La cita pudo haber sido modificada. Intente de nuevo.";
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing reschedule for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al enviar su propuesta.";
            }
            
            // Repopulate needed view data if save fails
            if (appointmentToUpdate.Patient != null)
            {
                PatientName = appointmentToUpdate.Patient.FullName ?? "Paciente Desconocido";
            }
            var finalAppointmentColombia = ColombiaTimeZoneService.ConvertUtcToColombia(appointmentToUpdate.AppointmentDateTime);
            CurrentDateTime = finalAppointmentColombia.ToString("g");
            return Page();
        }
    }
} 




