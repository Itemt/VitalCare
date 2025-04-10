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

namespace CitasEPS.Pages.Appointments
{
    [Authorize] // Require login to create appointments
    public class CreateModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;

        public CreateModel(CitasEPS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // This method runs when the page is requested via GET
        public async Task<IActionResult> OnGetAsync()
        {
            // Pre-populate dropdown lists for Patients and Doctors
            await PopulateDropdownsAsync();
            return Page();
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Add a property to bind the selected specialty ID from the dropdown
        [BindProperty(SupportsGet = true)]
        public int? SelectedSpecialtyId { get; set; }

        // Properties to hold the SelectList data for the dropdowns
        public SelectList PatientNameSL { get; set; } = default!;
        public SelectList SpecialtySL { get; set; } = default!;
        // DoctorNameSL will be populated dynamically


        // This method runs when the form is submitted via POST
        public async Task<IActionResult> OnPostAsync()
        {
            // Remove the invalid Appointment.SpecialtyId references
            if (!ModelState.IsValid || Appointment.DoctorId == 0 || Appointment.PatientId == 0)
            {
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

            _context.Appointments.Add(Appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cita agendada exitosamente.";

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
            // Populate Patients
            var patients = await _context.Patients
                                        .OrderBy(p => p.LastName)
                                        .ThenBy(p => p.FirstName)
                                        .ToListAsync();
            PatientNameSL = new SelectList(patients, nameof(Patient.Id), nameof(Patient.FullName));

            // Populate Specialties, using the passed selectedSpecialtyId
            var specialties = await _context.Specialties
                                        .OrderBy(s => s.Name)
                                        .ToListAsync();
            SpecialtySL = new SelectList(specialties, nameof(Specialty.Id), nameof(Specialty.Name), selectedSpecialtyId);

            // Doctor dropdown is populated dynamically via AJAX, no need to load here initially
            // unless repopulating after POST error with a specialty already selected (handled in OnPostAsync)

            // Future Enhancement: Pre-select patient based on logged-in user
        }
    }
} 