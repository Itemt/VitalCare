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

        // Properties to hold the SelectList data for the dropdowns
        public SelectList PatientNameSL { get; set; } = default!;
        public SelectList DoctorNameSL { get; set; } = default!;


        // This method runs when the form is submitted via POST
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // If model state is invalid, repopulate dropdowns and return the page to show errors
                await PopulateDropdownsAsync();
                return Page();
            }

            _context.Appointments.Add(Appointment);
            await _context.SaveChangesAsync();

            // Optionally: Add a success message using TempData
            TempData["SuccessMessage"] = "Cita agendada exitosamente.";

            return RedirectToPage("./Index"); // Redirect to the list page after successful creation
        }

        // Helper method to load data for dropdowns
        private async Task PopulateDropdownsAsync()
        {
             var patients = await _context.Patients
                                        .OrderBy(p => p.LastName)
                                        .ThenBy(p => p.FirstName)
                                        .ToListAsync();
            PatientNameSL = new SelectList(patients, nameof(Patient.Id), nameof(Patient.FullName));

            var doctors = await _context.Doctors
                                      .OrderBy(d => d.LastName)
                                      .ThenBy(d => d.FirstName)
                                      .ToListAsync();
            DoctorNameSL = new SelectList(doctors, nameof(Doctor.Id), nameof(Doctor.FullName));

            // Future Enhancement: If not admin, maybe pre-select the logged-in user as the patient?
            // This would require linking User accounts to Patient records.
        }
    }
} 