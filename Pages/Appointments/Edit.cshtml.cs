using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using CitasEPS.Services;

namespace CitasEPS.Pages.Appointments
{
    // [Authorize] // O política específica como "Admin" si solo admins pueden editar todas las citas
    // Por ahora, asumiremos que el paciente puede editar (quizás con limitaciones)
    [Authorize(Roles = "Admin")] // REMOVED "Doctor" from roles, only Admin can edit directly
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly UserManager<User> _userManager; // Inject UserManager for role checks
        private readonly IAppointmentEmailService _appointmentEmailService;
        private readonly INotificationService _notificationService;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger, UserManager<User> userManager, IAppointmentEmailService appointmentEmailService, INotificationService notificationService) // Add UserManager
        {
            _context = context;
            _logger = logger;
            _userManager = userManager; // Store UserManager
            _appointmentEmailService = appointmentEmailService;
            _notificationService = notificationService;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Propiedades para dropdowns
        public SelectList PatientNameSL { get; set; } = default!;
        public SelectList DoctorNameSL { get; set; } = default!;
        public bool IsCurrentUserPatient { get; set; } // New property

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Obtener cita a editar, sin rastreo inicial
            var appointment = await _context.Appointments.AsNoTracking()
                                          .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }
            Appointment = appointment;

            IsCurrentUserPatient = User.IsInRole("Paciente"); // Set the property

            // TODO: Verificar autorización - ¿Este paciente/admin puede editar ESTA cita?
            // Ejemplo: Si es paciente, verificar que Appointment.PatientId coincida con el ID del paciente logueado.

            // Poblar dropdowns solo si es Admin
            if (User.IsInRole("Admin"))
            {
                await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
            }
            else // For Patient/Doctor, store original values to display them as non-editable
            {
                // Optionally, load names for display if not using dropdowns
                ViewData["PatientName"] = await _context.Patients.Where(p => p.Id == Appointment.PatientId).Select(p => p.FullName).FirstOrDefaultAsync();
                ViewData["DoctorName"] = await _context.Doctors.Where(d => d.Id == Appointment.DoctorId).Select(d => d.FullName).FirstOrDefaultAsync();
            }
            return Page();
        }

        // Para proteger de ataques de sobreposteo, habilite las propiedades específicas a las que desea enlazar.
        // Para más detalles, vea https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var originalAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == Appointment.Id);
            if (originalAppointment == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                // For non-admins, ensure PatientId and DoctorId are not changed
                Appointment.PatientId = originalAppointment.PatientId;
                Appointment.DoctorId = originalAppointment.DoctorId;
            }

            // UTC Conversion for AppointmentDateTime (assuming it might be edited)
            // This should be done BEFORE any validation that uses this date.
            var originalBoundDateTime = Appointment.AppointmentDateTime; 
            if (Appointment.AppointmentDateTime.Kind == DateTimeKind.Unspecified)
            {
                Appointment.AppointmentDateTime = DateTime.SpecifyKind(Appointment.AppointmentDateTime, DateTimeKind.Local).ToUniversalTime();
            }
            else if (Appointment.AppointmentDateTime.Kind == DateTimeKind.Local)
            {
                Appointment.AppointmentDateTime = Appointment.AppointmentDateTime.ToUniversalTime();
            }
            // --- End UTC Conversion ---

            // --- START: Working Hours Validation (using original local time for TimeOfDay check) ---
            var localAppTime = originalBoundDateTime.TimeOfDay; 
            var localAppDay = originalBoundDateTime.DayOfWeek;  
            if (localAppDay == DayOfWeek.Saturday || localAppDay == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse de Lunes a Viernes.");
            }
            else if (localAppTime < new TimeSpan(8, 0, 0) || localAppTime >= new TimeSpan(18, 0, 0)) 
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse entre las 8:00 AM y las 6:00 PM (la hora de las 6:00 PM es el cierre, no una hora de inicio disponible).");
            }
            // --- END: Working Hours Validation ---
            
            // --- START: Past Date Validation (comparing against UtcNow as AppointmentDateTime is now UTC) ---
            if (Appointment.AppointmentDateTime < DateTime.UtcNow)
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "No puede seleccionar una fecha u hora en el pasado.");
            }
            // --- END: Past Date Validation ---

            // --- START: Patient Weekly Limit Validation ---
            if (User.IsInRole("Paciente") || User.IsInRole("Admin")) // Or apply to all roles that can change date
            {
                DayOfWeek firstDayOfWeek = DayOfWeek.Monday; 
                DateTime utcStartDate = Appointment.AppointmentDateTime.Date;
                while (utcStartDate.DayOfWeek != firstDayOfWeek) { utcStartDate = utcStartDate.AddDays(-1); }
                DateTime utcEndDate = utcStartDate.AddDays(7);
                var appointmentsInWeek = await _context.Appointments
                    .Where(a => a.PatientId == Appointment.PatientId &&
                                a.Id != Appointment.Id && 
                                !a.IsCancelled && 
                                a.AppointmentDateTime >= utcStartDate && 
                                a.AppointmentDateTime < utcEndDate)
                    .CountAsync();
                if (appointmentsInWeek >= 2)
                {
                    ModelState.AddModelError("Appointment.AppointmentDateTime", "El paciente ya tiene 2 citas agendadas para esta semana. No se pueden agendar más.");
                }
            }
            // --- END: Patient Weekly Limit Validation ---

            // --- START: Doctor Availability Validation ---
            if (ModelState.IsValid) 
            {
                var doctorHasSlot = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == Appointment.DoctorId &&
                                   a.Id != Appointment.Id && 
                                   a.AppointmentDateTime == Appointment.AppointmentDateTime &&
                                   !a.IsCancelled);
                if (doctorHasSlot)
                {
                    ModelState.AddModelError("Appointment.AppointmentDateTime", "El doctor seleccionado ya tiene una cita programada para esta fecha y hora exactas.");
                }
            }
            // --- END: Doctor Availability Validation ---

            if (!ModelState.IsValid)
            {
                IsCurrentUserPatient = User.IsInRole("Paciente"); // Re-set for view if validation fails
                if (User.IsInRole("Admin"))
                {
                    await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                }
                else
                {
                    ViewData["PatientName"] = await _context.Patients.Where(p => p.Id == originalAppointment.PatientId).Select(p => p.FullName).FirstOrDefaultAsync();
                    ViewData["DoctorName"] = await _context.Doctors.Where(d => d.Id == originalAppointment.DoctorId).Select(d => d.FullName).FirstOrDefaultAsync();
                }
                return Page();
            }

            // Preserve sensitive fields if user is a patient
            if (User.IsInRole("Paciente"))
            {
                Appointment.IsConfirmed = originalAppointment.IsConfirmed;
                Appointment.RescheduleRequested = originalAppointment.RescheduleRequested;
                Appointment.ProposedNewDateTime = originalAppointment.ProposedNewDateTime;
                Appointment.WasNoShow = originalAppointment.WasNoShow;
                Appointment.IsCancelled = originalAppointment.IsCancelled;
            }
            // If Admin or Doctor, they can modify IsConfirmed, etc. (ProposedNewDateTime handled by specific flows)
            // For ProposedNewDateTime, it should ideally be nullified if a Doctor/Admin directly sets AppointmentDateTime
            // or IsConfirmed, unless the edit *is* part of confirming a proposal.
            // If an Admin/Doctor confirms the main AppointmentDateTime, clear proposal flags:
            if (!User.IsInRole("Paciente") && Appointment.AppointmentDateTime != originalAppointment.AppointmentDateTime)
            {
                Appointment.RescheduleRequested = false;
                Appointment.ProposedNewDateTime = null;
            }

            _context.Attach(Appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Enviar notificación y correo si la fecha/hora cambió
                if (originalAppointment.AppointmentDateTime != Appointment.AppointmentDateTime)
                {
                    try
                    {
                        // Obtener información completa de la cita para envío de correos
                        var updatedAppointment = await _context.Appointments
                            .Include(a => a.Patient)
                            .ThenInclude(p => p.User)
                            .Include(a => a.Doctor)
                            .ThenInclude(d => d.User)
                            .FirstOrDefaultAsync(a => a.Id == Appointment.Id);

                        if (updatedAppointment?.Patient?.User != null && updatedAppointment?.Doctor?.User != null)
                        {
                            // Enviar notificación al paciente
                            await _notificationService.CreateNotificationAsync(
                                updatedAppointment.Patient.User.Id,
                                $"Su cita ha sido modificada. Nueva fecha y hora: {updatedAppointment.AppointmentDateTime:dd/MM/yyyy 'a las' HH:mm}",
                                NotificationType.AppointmentModified,
                                updatedAppointment.Id
                            );

                            // Enviar correo al paciente
                            _logger.LogInformation($"Enviando correo de modificación al paciente {updatedAppointment.Patient.User.Email}");
                            await _appointmentEmailService.SendAppointmentModifiedEmailAsync(
                                updatedAppointment, 
                                updatedAppointment.Patient.User, 
                                updatedAppointment.Doctor.User,
                                "Cambio de fecha y hora"
                            );
                            _logger.LogInformation($"Correo de modificación enviado exitosamente al paciente {updatedAppointment.Patient.User.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error al enviar notificación o correo de modificación para la cita {AppointmentId}.", Appointment.Id);
                    }
                }

                TempData["SuccessMessage"] = "Cita actualizada exitosamente.";
                 _logger.LogInformation("Cita con ID: {AppointmentId} actualizada.", Appointment.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Error de concurrencia actualizando cita ID: {AppointmentId}", Appointment.Id);
                if (!AppointmentExists(Appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "La cita fue modificada por otro usuario. Por favor, recargue la página e intente de nuevo.");
                    IsCurrentUserPatient = User.IsInRole("Paciente");
                    if (User.IsInRole("Admin")) await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                    else {
                         ViewData["PatientName"] = await _context.Patients.Where(p => p.Id == originalAppointment.PatientId).Select(p => p.FullName).FirstOrDefaultAsync();
                         ViewData["DoctorName"] = await _context.Doctors.Where(d => d.Id == originalAppointment.DoctorId).Select(d => d.FullName).FirstOrDefaultAsync();
                    }
                    return Page();
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error inesperado actualizando cita ID: {AppointmentId}", Appointment.Id);
                 ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al guardar los cambios.");
                 IsCurrentUserPatient = User.IsInRole("Paciente");
                 if (User.IsInRole("Admin")) await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                 else {
                      ViewData["PatientName"] = await _context.Patients.Where(p => p.Id == originalAppointment.PatientId).Select(p => p.FullName).FirstOrDefaultAsync();
                      ViewData["DoctorName"] = await _context.Doctors.Where(d => d.Id == originalAppointment.DoctorId).Select(d => d.FullName).FirstOrDefaultAsync();
                 } 
                 return Page();
            }

            return RedirectToPage("./Index");
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // Método auxiliar para cargar datos para dropdowns, seleccionando valores actuales
        private async Task PopulateDropdownsAsync(object? selectedPatient = null, object? selectedDoctor = null)
        {
             var patients = await _context.Patients
                                        .OrderBy(p => p.LastName)
                                        .ThenBy(p => p.FirstName)
                                        .ToListAsync();
            PatientNameSL = new SelectList(patients, nameof(CitasEPS.Models.Patient.Id), nameof(CitasEPS.Models.Patient.FullName), selectedPatient);

            var doctors = await _context.Doctors
                                      .Include(d => d.Specialty) // Include specialty for better display name if needed
                                      .OrderBy(d => d.LastName)
                                      .ThenBy(d => d.FirstName)
                                      .ToListAsync();
            // Consider creating a FullNameWithSpecialty property on Doctor model for cleaner display
            DoctorNameSL = new SelectList(doctors, nameof(Models.Doctor.Id), nameof(Models.Doctor.FullName), selectedDoctor);
        }
    }
} 