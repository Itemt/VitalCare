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

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
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
             var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
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

            // --- START: Add Past Date Validation ---
            if (Appointment.AppointmentDateTime < DateTime.Now)
            {
                 ModelState.AddModelError("Appointment.AppointmentDateTime", "No puede seleccionar una fecha u hora en el pasado.");
            }
            // --- END: Add Past Date Validation ---

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

            // Ensure the DateTime is in UTC before saving
            Appointment.AppointmentDateTime = Appointment.AppointmentDateTime.ToUniversalTime();

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