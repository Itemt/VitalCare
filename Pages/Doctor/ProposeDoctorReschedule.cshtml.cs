using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class ProposeDoctorRescheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProposeDoctorRescheduleModel> _logger;

        public ProposeDoctorRescheduleModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ProposeDoctorRescheduleModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Appointment ID

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public Appointment CurrentAppointment { get; set; } = default!;
        public string PatientName { get; set; } = default!;
        public string CurrentDateTime { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "La nueva fecha y hora propuesta es requerida.")]
            [Display(Name = "Nueva Fecha y Hora Propuesta")]
            public DateTime ProposedNewDateTime { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // TODO: Load Appointment, verify doctor ownership, check eligibility (future, not completed, not cancelled, no pending patient proposal)
            // TODO: Populate view data (PatientName, CurrentDateTime)
            // TODO: Set initial Input.ProposedNewDateTime suggestion

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
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


            PatientName = CurrentAppointment.Patient?.FullName ?? "Paciente Desconocido";
            CurrentDateTime = CurrentAppointment.AppointmentDateTime.ToLocalTime().ToString("g"); // Display local time
            Input = new InputModel { ProposedNewDateTime = CurrentAppointment.AppointmentDateTime.AddDays(1) }; // Suggest next day

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // TODO: Load Appointment, verify doctor ownership, check eligibility
            // TODO: Validate Input.ProposedNewDateTime (past, working hours, patient availability, etc.) - use UTC
            // TODO: Set Appointment.ProposedNewDateTime, DoctorProposedReschedule=true, IsConfirmed=false
            // TODO: Save changes

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
             if (doctor == null) return NotFound("Doctor profile not found.");

            var appointmentToUpdate = await _context.Appointments
                                            .Include(a => a.Patient) // Needed for validation checks potentially
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

            // --- START: Convert Input.ProposedNewDateTime to UTC EARLY ---
            var originalProposedDateTime = Input.ProposedNewDateTime; // Keep local for validation
            var utcProposedDateTime = Input.ProposedNewDateTime;
             if (utcProposedDateTime.Kind == DateTimeKind.Unspecified)
            {
                 utcProposedDateTime = DateTime.SpecifyKind(utcProposedDateTime, DateTimeKind.Local).ToUniversalTime();
            }
            else if (utcProposedDateTime.Kind == DateTimeKind.Local)
            {
                utcProposedDateTime = utcProposedDateTime.ToUniversalTime();
            }
            _logger.LogInformation($"Proposed Time UTC: {utcProposedDateTime}");
             // --- END: Convert Input.ProposedNewDateTime to UTC EARLY ---

            // --- START VALIDATIONS --- 
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
            // --- END VALIDATIONS --- 

            if (!ModelState.IsValid)
            {
                // Repopulate needed view data
                PatientName = appointmentToUpdate.Patient?.FullName ?? "Paciente Desconocido";
                CurrentDateTime = appointmentToUpdate.AppointmentDateTime.ToLocalTime().ToString("g");
                return Page();
            }

            // Update Appointment
            appointmentToUpdate.ProposedNewDateTime = utcProposedDateTime; // Save UTC version
            appointmentToUpdate.DoctorProposedReschedule = true;
            appointmentToUpdate.RescheduleRequested = false; // Clear any previous patient request
            appointmentToUpdate.IsConfirmed = false; // Requires patient confirmation now

            _context.Attach(appointmentToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Doctor {user.Email} proposed reschedule for Appointment {Id} to {utcProposedDateTime}");
                TempData["SuccessMessage"] = "Propuesta de reagendamiento enviada al paciente exitosamente.";
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
            PatientName = appointmentToUpdate.Patient?.FullName ?? "Paciente Desconocido";
            CurrentDateTime = appointmentToUpdate.AppointmentDateTime.ToLocalTime().ToString("g");
            return Page();
        }
    }
} 