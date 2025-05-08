using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization; // Add for authorization
using Microsoft.EntityFrameworkCore; // Required for ToListAsync
using Microsoft.AspNetCore.Identity; // Required for UserManager
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient")] // Restrict to Patients only
    public class CreateModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; // Inject UserManager
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(CitasEPS.Data.ApplicationDbContext context, UserManager<User> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null)
            {
                // Log error and display message to user
                _logger.LogError($"Could not find Patient record for logged-in user {user.Email}.");
                TempData["ErrorMessage"] = "No se pudo encontrar su registro de paciente asociado. Por favor, contacte a soporte.";
                return RedirectToPage("/Index"); // Redirect to a safe page
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
             var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
             if (patient == null)
             { // Should have been caught in OnGet, but double check
                _logger.LogError($"POST Error: Could not find Patient record for logged-in user {user.Email}.");
                TempData["ErrorMessage"] = "Error al procesar la solicitud. No se encontró su registro de paciente.";
                return RedirectToPage("/Index");
             }

             LoggedInPatientId = patient.Id;
             LoggedInPatientName = patient.FullName;
             ViewData["PatientName"] = LoggedInPatientName; // Pass name back to view if validation fails

            // Assign patient ID before validation
            Appointment.PatientId = LoggedInPatientId;

            // --- START: Convert AppointmentDateTime to UTC EARLY ---
            var originalBoundDateTime = Appointment.AppointmentDateTime;
            if (originalBoundDateTime.Kind == DateTimeKind.Unspecified)
            {
                _logger.LogInformation($"Original BOUND AppointmentDateTime '{originalBoundDateTime}' had Kind Unspecified. Assuming Local, then converting to UTC.");
                Appointment.AppointmentDateTime = DateTime.SpecifyKind(originalBoundDateTime, DateTimeKind.Local).ToUniversalTime();
            }
            else if (originalBoundDateTime.Kind == DateTimeKind.Local)
            {
                _logger.LogInformation($"Original BOUND AppointmentDateTime '{originalBoundDateTime}' had Kind Local. Converting to UTC.");
                Appointment.AppointmentDateTime = originalBoundDateTime.ToUniversalTime();
            }
            else // Already UTC
            {
                 _logger.LogInformation($"Original BOUND AppointmentDateTime '{originalBoundDateTime}' was already Kind Utc. No conversion needed.");
                // Ensure it's assigned if it was a different instance, though unlikely here.
                Appointment.AppointmentDateTime = originalBoundDateTime;
            }
            _logger.LogInformation($"Early converted AppointmentDateTime: '{Appointment.AppointmentDateTime}', Kind: '{Appointment.AppointmentDateTime.Kind}'. This will be used for validations.");
            // --- END: Convert AppointmentDateTime to UTC EARLY ---

            // --- START: Add Past Date Validation ---
            if (Appointment.AppointmentDateTime < DateTime.Now)
            {
                 ModelState.AddModelError("Appointment.AppointmentDateTime", "No puede seleccionar una fecha u hora en el pasado.");
            }
            // --- END: Add Past Date Validation ---

            // --- START: Working Hours Validation (Mon-Fri, 8 AM - 6 PM) ---
            var appTime = Appointment.AppointmentDateTime.TimeOfDay;
            var appDay = Appointment.AppointmentDateTime.DayOfWeek;

            if (appDay == DayOfWeek.Saturday || appDay == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse de Lunes a Viernes.");
            }
            else if (appTime < new TimeSpan(8, 0, 0) || appTime >= new TimeSpan(18, 0, 0)) // 8 AM to 5:59 PM
            {
                ModelState.AddModelError("Appointment.AppointmentDateTime", "Las citas solo pueden agendarse entre las 8:00 AM y las 5:59 PM.");
            }
            // --- END: Working Hours Validation ---

            // --- START: Patient Weekly Limit Validation (Max 2 per week) ---
            if (ModelState.IsValid) // Only proceed if previous validations passed
            {
                DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // Assuming week starts on Monday
                DateTime startDate = Appointment.AppointmentDateTime.Date;
                while (startDate.DayOfWeek != firstDayOfWeek)
                {
                    startDate = startDate.AddDays(-1);
                }
                DateTime endDate = startDate.AddDays(7);

                var appointmentsInWeek = await _context.Appointments
                    .Where(a => a.PatientId == Appointment.PatientId &&
                                // No need to filter by status, count all
                                a.AppointmentDateTime >= startDate &&
                                a.AppointmentDateTime < endDate)
                    .CountAsync();

                if (appointmentsInWeek >= 2)
                {
                    ModelState.AddModelError("Appointment.AppointmentDateTime", "El paciente ya tiene 2 citas agendadas para esta semana. No se pueden agendar más.");
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

                // Repopulate using the SelectedSpecialtyId property
                await PopulateDropdownsAsync(SelectedSpecialtyId);
                if (SelectedSpecialtyId.HasValue && SelectedSpecialtyId > 0)
                {
                    var doctors = await _context.Doctors
                                              .Where(d => d.SpecialtyId == SelectedSpecialtyId.Value)
                                              .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
                                              .Select(d => new { d.Id, d.FullName })
                                              .ToListAsync();
                     // Use ViewData to pass the list back if validation fails
                     ViewData["DoctorNameSL"] = new SelectList(doctors, "Id", "FullName", Appointment.DoctorId);
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

            TempData["SuccessMessage"] = $"Cita para {LoggedInPatientName} agendada exitosamente.";

            return RedirectToPage("./Index");
        }

        // Handler to get doctors based on specialtyId - called via AJAX
        public async Task<JsonResult> OnGetDoctorsBySpecialtyAsync(int specialtyId)
        {
            var doctors = await _context.Doctors
                                      .Where(d => d.SpecialtyId == specialtyId)
                                      .OrderBy(d => d.LastName)
                                      .ThenBy(d => d.FirstName)
                                      .Select(d => new { d.Id, d.FullName }) // Select only needed data
                                      .ToListAsync();
            return new JsonResult(doctors);
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