using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Admin")] // Or specific policy like "Admin"
    public class DeleteModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;

        public DeleteModel(CitasEPS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the appointment including related data to display confirmation details
            Appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsNoTracking() // No need to track for deletion confirmation
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Appointment == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ErrorMessage = String.Format("Error al eliminar la cita {0}. Intente de nuevo.", id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointmentToDelete = await _context.Appointments.FindAsync(id);

            if (appointmentToDelete == null)
            {
                // Appointment already deleted or never existed
                 TempData["SuccessMessage"] = "La cita ya no existe o fue eliminada.";
                 return RedirectToPage("./Index");
            }

            try
            {
                _context.Appointments.Remove(appointmentToDelete);
                await _context.SaveChangesAsync();
                 TempData["SuccessMessage"] = "Cita eliminada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                 return RedirectToAction("./Delete", new { id = id, saveChangesError = true });
            }
        }
    }
} 