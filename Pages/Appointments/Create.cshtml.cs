using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Models.Modules.Common;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CitasEPS.Services;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Paciente")] // Restrict to Patients only
    public class CreateModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; // Inject UserManager
        private readonly ILogger<CreateModel> _logger;
        private readonly IAppointmentPolicyService _appointmentPolicyService; // <<< Inject service
        private readonly INotificationService _notificationService; // <<< Inject NotificationService
        private readonly IAppointmentEmailService _appointmentEmailService; // <<< Inject AppointmentEmailService

        public CreateModel(CitasEPS.Data.ApplicationDbContext context, UserManager<User> userManager, ILogger<CreateModel> logger, IAppointmentPolicyService appointmentPolicyService, INotificationService notificationService, IAppointmentEmailService appointmentEmailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _appointmentPolicyService = appointmentPolicyService; // <<< Assign service
            _notificationService = notificationService; // <<< Assign NotificationService
            _appointmentEmailService = appointmentEmailService; // <<< Assign AppointmentEmailService
        }

        // Store the logged-in patient's ID and name
        public int LoggedInPatientId { get; set; }
        public string LoggedInPatientName { get; set; } = "";

        // This method runs when the page is requested via GET
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Should not happen if Authorize is working, but good practice
                return Challenge(); // Or redirect to login
            }

            // Check if email is confirmed before allowing access to appointment creation
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"User {user.Email} attempted to access appointment creation without a confirmed email.");
                TempData["ErrorMessage"] = "Debe confirmar su dirección de correo electrónico antes de poder agendar citas.";
                return RedirectToPage("/Index"); 
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null)
            {
                // Log error and display message to user
                _logger.LogError($"Could not find Patient record for logged-in user {user.Email}.");

                // Attempt to create missing Patient record automatically
                try
                {
                    _logger.LogInformation($"Attempting to create missing Patient record for user {user.Email} (ID: {user.Id})");
                    
                    patient = new Patient
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName ?? "Sin nombre",
                        LastName = user.LastName ?? "Sin apellido", 
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        DocumentId = user.DocumentId,
                        Gender = user.Gender ?? Gender.Otro
                    };
                    
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Successfully created Patient record for user {user.Email} (ID: {user.Id})");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to auto-create Patient record for user {user.Email} (ID: {user.Id})");
                TempData["ErrorMessage"] = "No se pudo encontrar su registro de paciente asociado. Por favor, contacte a soporte.";
                return RedirectToPage("/Index"); // Redirect to a safe page
                }
            }

            LoggedInPatientId = patient.Id;
            LoggedInPatientName = patient.FullName;
            ViewData["PatientName"] = LoggedInPatientName; // Pass name to view

            // Pre-populate dropdown lists for Specialties (Doctors loaded via AJAX)
            await PopulateDropdownsAsync();
            return Page();
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Add a property to bind the selected specialty ID from the dropdown
        [BindProperty(SupportsGet = true)]
        public int? SelectedSpecialtyId { get; set; }

        // Properties to hold the SelectList data for the dropdowns
        // PatientNameSL removed
        public SelectList SpecialtySL { get; set; } = default!;
        // DoctorNameSL will be populated dynamically


        // This method runs when the form is submitted via POST
        public async Task<IActionResult> OnPostAsync()
        {
             var user = await _userManager.GetUserAsync(User);
             if (user == null) return Challenge();

            // Check if email is confirmed before processing the appointment POST
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"User {user.Email} attempted to POST to appointment creation without a confirmed email.");
                TempData["ErrorMessage"] = "Debe confirmar su dirección de correo electrónico antes de poder agendar citas.";
                // Repopulate dropdowns before redirecting or returning the page
                // Consider if SelectedSpecialtyId is available and valid here or if it needs to be handled more robustly
                await PopulateDropdownsAsync(SelectedSpecialtyId); 
                return RedirectToPage("/Index"); // Redirecting is simpler than trying to re-render the page with an error before patient is loaded
            }

             var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
             if (patient == null)
             { // Should have been caught in OnGet, but double check
                _logger.LogError($"POST Error: Could not find Patient record for logged-in user {user.Email}.");

                // Attempt to create missing Patient record automatically
                try
                {
                    _logger.LogInformation($"Attempting to create missing Patient record for user {user.Email} (ID: {user.Id}) during POST");
                    
                    patient = new Patient
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName ?? "Sin nombre",
                        LastName = user.LastName ?? "Sin apellido", 
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        DocumentId = user.DocumentId,
                        Gender = user.Gender ?? Gender.Otro
                    };
                    
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Successfully created Patient record for user {user.Email} (ID: {user.Id}) during POST");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to auto-create Patient record for user {user.Email} (ID: {user.Id}) during POST");
                TempData["ErrorMessage"] = "Error al procesar la solicitud. No se encontró su registro de paciente.";
                return RedirectToPage("/Index");
                }
             }

             LoggedInPatientId = patient.Id;
             LoggedInPatientName = patient.FullName;
             ViewData["PatientName"] = LoggedInPatientName; // Pass name back to view if validation fails

            // Assign patient ID before validation
            Appointment.PatientId = LoggedInPatientId;

            // --- START: Store original local DateTime for specific validations ---
            var localAppointmentDateTime = Appointment.AppointmentDateTime; // Store the original, assumed local, DateTime
            if (localAppointmentDateTime.Kind == DateTimeKind.Unspecified)
            {
                // If unspecified, explicitly treat it as Local for the purpose of local time validations.
                // The database conversion to UTC will handle its own logic later.
                localAppointmentDateTime = DateTime.SpecifyKind(localAppointmentDateTime, DateTimeKind.Local);
                _logger.LogInformation($"Original BOUND AppointmentDateTime '{Appointment.AppointmentDateTime}' was Unspecified. For local validation, treating as Local: '{localAppointmentDateTime}'.");
            }
            else
            {
                 _logger.LogInformation($"Original BOUND AppointmentDateTime for local validation: '{localAppointmentDateTime}', Kind: '{localAppointmentDateTime.Kind}'.");
            }
            // --- END: Store original local DateTime for specific validations ---

            // --- START: Convert AppointmentDateTime to UTC EARLY (for database and UTC-dependent logic) ---
            var originalBoundDateTimeForUtcConversion = Appointment.AppointmentDateTime;
            if (originalBoundDateTimeForUtcConversion.Kind == DateTimeKind.Unspecified)
            {
                _logger.LogInformation($"Original BOUND AppointmentDateTime for UTC conversion '{originalBoundDateTimeForUtcConversion}' had Kind Unspecified. Assuming Local, then converting to UTC.");
                Appointment.AppointmentDateTime = DateTime.SpecifyKind(originalBoundDateTimeForUtcConversion, DateTimeKind.Local).ToUniversalTime();
            }
            else if (originalBoundDateTimeForUtcConversion.Kind == DateTimeKind.Local)
            {
                _logger.LogInformation($"Original BOUND AppointmentDateTime for UTC conversion '{originalBoundDateTimeForUtcConversion}' had Kind Local. Converting to UTC.");
                Appointment.AppointmentDateTime = originalBoundDateTimeForUtcConversion.ToUniversalTime();
            }
            else // Already UTC
            {
                 _logger.LogInformation($"Original BOUND AppointmentDateTime for UTC conversion '{originalBoundDateTimeForUtcConversion}' was already Kind Utc. No conversion needed.");
                Appointment.AppointmentDateTime = originalBoundDateTimeForUtcConversion;
            }
            _logger.LogInformation($"Early converted AppointmentDateTime (for DB): '{Appointment.AppointmentDateTime}', Kind: '{Appointment.AppointmentDateTime.Kind}'.");
            // --- END: Convert AppointmentDateTime to UTC EARLY ---

            // --- START: Add Past Date Validation (SIMPLIFIED) ---
            // Usar hora de Colombia directamente (UTC-5)
            var utcNow = DateTime.UtcNow;
            var colombiaCurrentTime = utcNow.AddHours(-5); // UTC-5 para Colombia
            
            // La fecha viene del formulario como local time, asumir que es hora de Colombia
            var requestedDateTime = localAppointmentDateTime;
            
            _logger.LogInformation($"[DEBUG] UTC Actual: {utcNow:dd/MM/yyyy HH:mm:ss}");
            _logger.LogInformation($"[DEBUG] Colombia Actual: {colombiaCurrentTime:dd/MM/yyyy HH:mm:ss}");
            _logger.LogInformation($"[DEBUG] Fecha Solicitada: {requestedDateTime:dd/MM/yyyy HH:mm:ss}");
            _logger.LogInformation($"[DEBUG] Fecha Kind: {requestedDateTime.Kind}");
            
            // Verificar si la fecha solicitada es futura (comparar con hora de Colombia)
            if (requestedDateTime <= colombiaCurrentTime)
            {
                var timezoneDiff = colombiaCurrentTime - requestedDateTime;
                _logger.LogWarning($"[ERROR] Fecha inválida. Diferencia: {timezoneDiff.TotalMinutes} minutos en el pasado");
                
                ModelState.AddModelError("Appointment.AppointmentDateTime", 
                    $"No puede seleccionar una fecha en el pasado. " +
                    $"Fecha actual (Colombia): {colombiaCurrentTime:dd/MM/yyyy HH:mm}. " +
                    $"Fecha solicitada: {requestedDateTime:dd/MM/yyyy HH:mm}");
            }
            else
            {
                // Verificar margen mínimo de 15 minutos
                var timeDifference = requestedDateTime - colombiaCurrentTime;
                if (timeDifference.TotalMinutes < 15)
                {
                    _logger.LogWarning($"[ERROR] Fecha muy cercana. Diferencia: {timeDifference.TotalMinutes} minutos");
                    ModelState.AddModelError("Appointment.AppointmentDateTime", 
                        $"Debe seleccionar una fecha con al menos 15 minutos de anticipación. " +
                        $"Hora actual (Colombia): {colombiaCurrentTime:HH:mm}");
                }
                else
                {
                    _logger.LogInformation($"[SUCCESS] Fecha válida. Diferencia: {timeDifference.TotalMinutes} minutos en el futuro");
                }
            }
            // --- END: Add Past Date Validation ---

            // --- START: Working Hours Validation (Mon-Fri, 8 AM - 6 PM) - USING COLOMBIA TIME ---
            var colombiaAppTime = requestedDateTime.TimeOfDay;
            var colombiaAppDay = requestedDateTime.DayOfWeek;

            if (colombiaAppDay == DayOfWeek.Saturday || colombiaAppDay == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse de Lunes a Viernes (horario de Colombia).");
            }
            else if (colombiaAppTime < new TimeSpan(8, 0, 0) || colombiaAppTime >= new TimeSpan(18, 0, 0)) // 8 AM to 5:59 PM
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse entre las 8:00 AM y las 5:59 PM (horario de Colombia).");
            }
            // --- END: Working Hours Validation ---

            // --- START: Patient Weekly Limit Validation (Max 2 per week) ---
            // The existing custom logic here will be replaced by the service call
            if (ModelState.IsValid) // Only proceed if previous validations passed
            {
                if (!_appointmentPolicyService.CanPatientCreateAppointment(patient.Id, Appointment.AppointmentDateTime, out string reason))
                {
                    ModelState.AddModelError("Appointment.AppointmentDateTime", reason);
                }
            }
            // --- END: Patient Weekly Limit Validation ---

            // --- START: Doctor Availability Validation (No Double Booking) ---
            if (ModelState.IsValid) // Only proceed if previous validations passed
            {
                var doctorHasSlot = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == Appointment.DoctorId &&
                                   a.AppointmentDateTime == Appointment.AppointmentDateTime);

                if (doctorHasSlot) // If true, means there IS an existing appointment
                {
                    ModelState.AddModelError("Appointment.AppointmentDateTime", "El doctor seleccionado ya tiene una cita programada para esta fecha y hora exactas.");
                }
            }
            // --- END: Doctor Availability Validation ---

            // Remove the invalid Appointment.SpecialtyId references
            // Validate DoctorId specifically
            if (!ModelState.IsValid || Appointment.DoctorId == 0)
            {
                 if (Appointment.DoctorId == 0) {
                     ModelState.AddModelError("Appointment.DoctorId", "Debe seleccionar un doctor.");
                 }

                _logger.LogInformation($"Validación falló. Repoblando dropdowns. SelectedSpecialtyId: {SelectedSpecialtyId}, Appointment.DoctorId: {Appointment.DoctorId}");

                // Repopulate using the SelectedSpecialtyId property
                await PopulateDropdownsAsync(SelectedSpecialtyId);
                
                // IMPORTANT: Mantener la selección del doctor si existe una especialidad válida
                if (SelectedSpecialtyId.HasValue && SelectedSpecialtyId > 0)
                {
                    var doctors = await _context.Doctors
                                              .Where(d => d.SpecialtyId == SelectedSpecialtyId.Value)
                                              .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
                                              .Select(d => new { 
                                                  d.Id, 
                                                  FullName = d.FirstName + " " + d.LastName 
                                              })
                                              .ToListAsync();
                    
                    _logger.LogInformation($"Encontrados {doctors.Count} doctores para especialidad {SelectedSpecialtyId}. Doctor seleccionado: {Appointment.DoctorId}");
                    
                    // Use ViewData to pass the list back if validation fails, manteniendo la selección del doctor
                    ViewData["DoctorNameSL"] = new SelectList(doctors, "Id", "FullName", Appointment.DoctorId);
                    
                    // NUEVO: Agregar información adicional para el JavaScript del frontend
                    ViewData["PreselectedDoctorId"] = Appointment.DoctorId;
                    ViewData["PreselectedSpecialtyId"] = SelectedSpecialtyId;
                }
                else
                {
                    _logger.LogWarning("No hay especialidad seleccionada válida para repoblar doctores");
                }
                
                return Page();
            }

            // At this point, Appointment.AppointmentDateTime is already UTC.
            // The block that was here previously to convert it has been moved up.

            // Ensure ProposedNewDateTime is also UTC if it has a value
            if (Appointment.ProposedNewDateTime.HasValue)
            {
                var originalProposedDateTime = Appointment.ProposedNewDateTime.Value;
                _logger.LogInformation($"Original ProposedNewDateTime '{originalProposedDateTime}', Kind: '{originalProposedDateTime.Kind}'.");
                if (originalProposedDateTime.Kind == DateTimeKind.Unspecified)
                {
                    _logger.LogInformation($"ProposedNewDateTime had Kind Unspecified. Assuming Local, then converting to UTC.");
                    Appointment.ProposedNewDateTime = DateTime.SpecifyKind(originalProposedDateTime, DateTimeKind.Local).ToUniversalTime();
                }
                else if (originalProposedDateTime.Kind == DateTimeKind.Local)
                {
                    _logger.LogInformation($"ProposedNewDateTime had Kind Local. Converting to UTC.");
                    Appointment.ProposedNewDateTime = originalProposedDateTime.ToUniversalTime();
                }
                // If already UTC, no change needed unless it was a different instance.
                // Ensure it is assigned to handle that potential.
                else
                {
                     Appointment.ProposedNewDateTime = originalProposedDateTime; // Already UTC
                }
                _logger.LogInformation($"Final ProposedNewDateTime to be saved: '{Appointment.ProposedNewDateTime.Value}', Kind: '{Appointment.ProposedNewDateTime.Value.Kind}'.");
            }
            else
            {
                _logger.LogInformation("ProposedNewDateTime is null. No conversion needed.");
            }

            _context.Appointments.Add(Appointment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cita ID {AppointmentId} guardada en la BD.", Appointment.Id);

            try
            {
                // Notificación para el Paciente
                var patientUser = await _userManager.FindByIdAsync(patient.UserId.ToString());
                if (patientUser != null)
                {
                    _logger.LogInformation($"Intentando notificación para Paciente User ID: {patientUser.Id} para cita ID: {Appointment.Id}");
                    
                    // Convertir fecha UTC a hora local de Colombia (UTC-5) para mostrar al paciente
                    var colombiaTimeZone = TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Colombia Standard Time", "Colombia Standard Time");
                    var appointmentColombia = TimeZoneInfo.ConvertTimeFromUtc(Appointment.AppointmentDateTime, colombiaTimeZone);
                    
                    await _notificationService.CreateNotificationAsync(
                        patientUser.Id,
                        $"Su cita para el {appointmentColombia:dd/MM/yyyy 'a las' HH:mm} ha sido agendada y está pendiente de confirmación por el consultorio.",
                        NotificationType.NewAppointment,
                        Appointment.Id
                    );
                    _logger.LogInformation($"Notificación creada para Paciente User ID: {patientUser.Id} por nueva cita ID: {Appointment.Id}");
                }
                else
                {
                    _logger.LogWarning($"No se pudo encontrar el User para el Paciente ID: {patient.Id} (UserId: {patient.UserId}) para enviar notificación.");
                }

                // Obtener el doctor con su información de User para la notificación
                var doctorForNotification = await _context.Doctors
                                                    .Include(d => d.User) 
                                                    .FirstOrDefaultAsync(d => d.Id == Appointment.DoctorId);

                // Notificación para el Doctor
                if (doctorForNotification?.User != null)
                {
                    _logger.LogInformation($"Intentando notificación para Doctor User ID: {doctorForNotification.User.Id} para cita ID: {Appointment.Id}");
                    
                    // Convertir fecha UTC a hora local de Colombia (UTC-5) para mostrar al doctor
                    var colombiaTimeZone = TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Colombia Standard Time", "Colombia Standard Time");
                    var appointmentColombia = TimeZoneInfo.ConvertTimeFromUtc(Appointment.AppointmentDateTime, colombiaTimeZone);
                    
                    await _notificationService.CreateNotificationAsync(
                        doctorForNotification.User.Id,
                        $"Tiene una nueva cita agendada con {LoggedInPatientName} para el {appointmentColombia:dd/MM/yyyy 'a las' HH:mm}.",
                        NotificationType.NewAppointment,
                        Appointment.Id
                    );
                    _logger.LogInformation($"Notificación creada para Doctor User ID: {doctorForNotification.User.Id} por nueva cita ID: {Appointment.Id}");
                }
                else
                {
                    _logger.LogWarning($"No se pudo notificar al Doctor ID: {Appointment.DoctorId}. 'doctorForNotification.User' es null. Doctor cargado: {doctorForNotification != null}, Doctor.UserId FK: {doctorForNotification?.UserId}");
                    if (doctorForNotification != null && doctorForNotification.UserId.HasValue && doctorForNotification.User == null)
                    {
                        _logger.LogWarning($"Doctor ID {doctorForNotification.Id} tiene UserId {doctorForNotification.UserId} pero doctor.User no fue cargado. REVISAR .Include(d => d.User).");
                    }
                }

                // Envío de correo electrónico al paciente
                if (patientUser != null && doctorForNotification?.User != null)
                {
                    _logger.LogInformation($"Enviando correo de cita agendada al paciente {patientUser.Email}");
                    await _appointmentEmailService.SendAppointmentCreatedEmailAsync(Appointment, patientUser, doctorForNotification.User);
                    _logger.LogInformation($"Correo de cita agendada enviado exitosamente al paciente {patientUser.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificaciones o enviar correos para la cita {AppointmentId}.", Appointment.Id);
                // Optionally, inform the user that notifications might have failed, though the main operation (appointment creation) succeeded.
            }

            TempData["SuccessMessage"] = $"Cita para {LoggedInPatientName} agendada exitosamente.";

            return RedirectToPage("./Index");
        }

        // Handler to get doctors based on specialtyId - called via AJAX
        public async Task<JsonResult> OnGetDoctorsBySpecialtyAsync(int specialtyId)
        {
            try
            {
                _logger.LogInformation($"[AJAX] Buscando doctores para especialidad ID: {specialtyId}");
                
                // SIMPLIFICADO: Buscar todos los doctores de la especialidad especificando solo campos que existen
                var doctors = await _context.Doctors
                                          .Where(d => d.SpecialtyId == specialtyId)
                                          .OrderBy(d => d.LastName)
                                          .ThenBy(d => d.FirstName)
                                          .Select(d => new { 
                                              d.Id, 
                                              FullName = d.FirstName + " " + d.LastName 
                                          })
                                          .ToListAsync();
                
                _logger.LogInformation($"[AJAX SUCCESS] Encontrados {doctors.Count} doctores para especialidad {specialtyId}");
                
                return new JsonResult(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AJAX ERROR] Error al buscar doctores para especialidad {specialtyId}: {ex.Message}");
                return new JsonResult(new { error = "Error al cargar médicos", message = ex.Message });
            }
        }

        // Helper method to load data for static dropdowns
        private async Task PopulateDropdownsAsync(int? selectedSpecialtyId = null)
        {
            // Populate Specialties, using the passed selectedSpecialtyId
            var specialties = await _context.Specialties
                                        .OrderBy(s => s.Name)
                                        .ToListAsync();
            SpecialtySL = new SelectList(specialties, nameof(Specialty.Id), nameof(Specialty.Name), selectedSpecialtyId);

            // Doctor dropdown is populated dynamically via AJAX
            // Patient dropdown removed
        }
    }
} 



