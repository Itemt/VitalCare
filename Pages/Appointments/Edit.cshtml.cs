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

namespace CitasEPS.Pages.Appointments
{
    [Authorize] // Or specific policy like "Admin" if only admins can edit all appointments
    public class EditModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;

        public EditModel(CitasEPS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Properties for dropdowns
        public SelectList PatientNameSL { get; set; } = default!;
        public SelectList DoctorNameSL { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the appointment to edit, without tracking initially
            var appointment = await _context.Appointments.AsNoTracking()
                                          .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }
            Appointment = appointment;

            // Populate dropdowns
            await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns if validation fails
                await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                return Page();
            }

            _context.Attach(Appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita actualizada exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(Appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    // Add concurrency error handling if needed
                    ModelState.AddModelError(string.Empty, "La cita fue modificada por otro usuario. Por favor, recargue la página e intente de nuevo.");
                    await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // Helper method to load data for dropdowns, selecting current values
        private async Task PopulateDropdownsAsync(object? selectedPatient = null, object? selectedDoctor = null)
        {
             var patients = await _context.Patients
                                        .OrderBy(p => p.LastName)
                                        .ThenBy(p => p.FirstName)
                                        .ToListAsync();
            PatientNameSL = new SelectList(patients, nameof(Patient.Id), nameof(Patient.FullName), selectedPatient);

            var doctors = await _context.Doctors
                                      .OrderBy(d => d.LastName)
                                      .ThenBy(d => d.FirstName)
                                      .ToListAsync();
            DoctorNameSL = new SelectList(doctors, nameof(Doctor.Id), nameof(Doctor.FullName), selectedDoctor);
        }
    }
} 