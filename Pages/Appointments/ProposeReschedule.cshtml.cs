using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient")]
    public class ProposeRescheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProposeRescheduleModel> _logger;

        public ProposeRescheduleModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<ProposeRescheduleModel> logger)
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
        public string DoctorName { get; set; } = default!;
        public string CurrentDateTime { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "La nueva fecha y hora propuesta es requerida.")]
            [Display(Name = "Nueva Fecha y Hora Propuesta")]
            public DateTime ProposedNewDateTime { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentAppointment = await _context.Appointments
                                        .Include(a => a.Doctor)
                                        .Include(a => a.Patient) // Need patient to verify ownership
                                        .FirstOrDefaultAsync(a => a.Id == Id);

            if (CurrentAppointment == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                return RedirectToPage("./Index");
            }

            // Verify ownership
            if (CurrentAppointment.Patient?.Email != user.Email)
            {
                 TempData["ErrorMessage"] = "No tiene permiso para modificar esta cita.";
                 return RedirectToPage("./Index");
            }

            // Check if eligible for reschedule proposal (Original date must be in the future)
            if (CurrentAppointment.IsCompleted || CurrentAppointment.AppointmentDateTime < DateTime.Now || CurrentAppointment.RescheduleRequested)
            {
                TempData["ErrorMessage"] = "Esta cita no es elegible para proponer reagendamiento.";
                return RedirectToPage("./Index");
            }

            DoctorName = CurrentAppointment.Doctor?.FullName ?? "Doctor Desconocido";
            CurrentDateTime = CurrentAppointment.AppointmentDateTime.ToString("g");

            Input = new InputModel
            {
                // Pre-fill suggestion
                ProposedNewDateTime = CurrentAppointment.AppointmentDateTime.AddDays(7) 
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appointmentToUpdate = await _context.Appointments
                                           .Include(a => a.Patient)
                                           .Include(a => a.Doctor) // Needed for validation checks
                                           .FirstOrDefaultAsync(a => a.Id == Id);

            if (appointmentToUpdate == null)
            {
                 TempData["ErrorMessage"] = "Cita no encontrada al intentar guardar.";
                 return RedirectToPage("./Index");
            }
            
            // Repopulate required view data in case of validation error
            DoctorName = appointmentToUpdate.Doctor?.FullName ?? "Doctor Desconocido";
            CurrentDateTime = appointmentToUpdate.AppointmentDateTime.ToString("g");

            // Verify ownership again
            if (appointmentToUpdate.Patient?.Email != user.Email)
            {
                 TempData["ErrorMessage"] = "No tiene permiso para modificar esta cita.";
                 return RedirectToPage("./Index");
            }

            // Check eligibility again before applying changes (Original date must be in the future)
            if (appointmentToUpdate.IsCompleted || appointmentToUpdate.AppointmentDateTime < DateTime.Now || appointmentToUpdate.RescheduleRequested)
            {
                TempData["ErrorMessage"] = "Esta cita ya no es elegible para proponer reagendamiento.";
                return RedirectToPage("./Index");
            }

            // --- START: Convert Input.ProposedNewDateTime to UTC EARLY ---
            var originalProposedDateTime = Input.ProposedNewDateTime;
            if (originalProposedDateTime.Kind == DateTimeKind.Unspecified)
            {
                _logger.LogInformation($"Original Input.ProposedNewDateTime '{originalProposedDateTime}' had Kind Unspecified. Assuming Local, then converting to UTC.");
                Input.ProposedNewDateTime = DateTime.SpecifyKind(originalProposedDateTime, DateTimeKind.Local).ToUniversalTime();
            }
            else if (originalProposedDateTime.Kind == DateTimeKind.Local)
            {
                _logger.LogInformation($"Original Input.ProposedNewDateTime '{originalProposedDateTime}' had Kind Local. Converting to UTC.");
                Input.ProposedNewDateTime = originalProposedDateTime.ToUniversalTime();
            }
            else // Already UTC
            {
                 _logger.LogInformation($"Original Input.ProposedNewDateTime '{originalProposedDateTime}' was already Kind Utc. No conversion needed.");
                Input.ProposedNewDateTime = originalProposedDateTime; // Ensure assignment
            }
            _logger.LogInformation($"Early converted Input.ProposedNewDateTime: '{Input.ProposedNewDateTime}', Kind: '{Input.ProposedNewDateTime.Kind}'. This will be used for validations.");
            // --- END: Convert Input.ProposedNewDateTime to UTC EARLY ---
            
            // --- START VALIDATIONS --- 
            if (Input.ProposedNewDateTime < DateTime.UtcNow) // Compare against UtcNow as Input.ProposedNewDateTime is now UTC
            {
                 ModelState.AddModelError("Input.ProposedNewDateTime", "No puede proponer una fecha u hora en el pasado.");
            }

            // Working Hours Validation (using the original local time for TimeOfDay check)
            var localAppTime = originalProposedDateTime.TimeOfDay; // Use the time from the original, local DateTime
            var localAppDay = originalProposedDateTime.DayOfWeek;  // Use the day from the original, local DateTime

            if (localAppDay == DayOfWeek.Saturday || localAppDay == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "Las citas solo pueden agendarse de Lunes a Viernes.");
            }
            // Valid times are >= 8 AM AND < 6 PM. Invalid times are < 8 AM OR >= 6 PM.
            else if (localAppTime < new TimeSpan(8, 0, 0) || localAppTime >= new TimeSpan(18, 0, 0)) 
            {
                // Updated message for clarity on the 6 PM boundary
                ModelState.AddModelError("Input.ProposedNewDateTime", "Las citas solo pueden agendarse entre las 8:00 AM y las 6:00 PM (la hora de las 6:00 PM es el cierre, no una hora de inicio disponible).");
            }

            // Patient Weekly Limit Validation
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
            DateTime startDate = Input.ProposedNewDateTime.Date; // This is UTC date
            while (startDate.DayOfWeek != firstDayOfWeek) { startDate = startDate.AddDays(-1); }
            DateTime endDate = startDate.AddDays(7);
            var appointmentsInWeek = await _context.Appointments
                .Where(a => a.PatientId == appointmentToUpdate.PatientId && 
                            a.Id != Id && // Exclude current appointment being proposed for
                            !a.IsCancelled && // Exclude cancelled appointments
                            // No need to filter by other status, count all non-cancelled
                            a.AppointmentDateTime >= startDate && 
                            a.AppointmentDateTime < endDate)
                .CountAsync();
            if (appointmentsInWeek >= 2)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "Ya tiene 2 citas agendadas para la semana seleccionada (excluyendo citas canceladas).");
            }

            // Doctor Availability Validation
            var doctorHasSlot = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointmentToUpdate.DoctorId && 
                               a.Id != Id && 
                               !a.IsCancelled && // Doctor's slot is free if an appointment in it was cancelled
                               a.AppointmentDateTime == Input.ProposedNewDateTime);
            if (doctorHasSlot)
            {
                ModelState.AddModelError("Input.ProposedNewDateTime", "El doctor ya tiene una cita programada para la fecha y hora exactas propuestas.");
            }
            // --- END VALIDATIONS --- 

            if (!ModelState.IsValid)
            {
                CurrentAppointment = appointmentToUpdate; // Needed to repopulate view correctly
                return Page();
            }

            // Update Appointment
            appointmentToUpdate.ProposedNewDateTime = Input.ProposedNewDateTime;
            appointmentToUpdate.RescheduleRequested = true;
            appointmentToUpdate.IsConfirmed = false; // Requires doctor confirmation now

            _context.Attach(appointmentToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient {user.Email} proposed reschedule for Appointment {Id} to {Input.ProposedNewDateTime}");
                TempData["SuccessMessage"] = "Propuesta de reagendamiento enviada exitosamente. El consultorio revisará su solicitud.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                 _logger.LogWarning(ex, "Concurrency error proposing reschedule for Appointment {AppointmentId}", Id);
                 TempData["ErrorMessage"] = "Error: La cita fue modificada por otro usuario. Intente de nuevo.";
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing reschedule for Appointment {AppointmentId}", Id);
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al enviar su propuesta.";
            }

            return RedirectToPage("./Index");
        }
    }
} 