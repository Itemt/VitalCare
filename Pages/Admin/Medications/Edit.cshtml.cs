using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.Admin.Medications
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Medication Medication { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication =  await _context.Medications.FirstOrDefaultAsync(m => m.Id == id);
            if (medication == null)
            {
                return NotFound();
            }
            Medication = medication;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Medication).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medicamento actualizado exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MedicationExists(Medication.Id))
                {
                    return NotFound();
                }
                else
                {
                    // Log the error (uncomment ex variable name and add logging)
                    // ModelState.AddModelError(string.Empty, "Unable to save changes. " +
                    //    "The record was deleted by another user.");
                    throw; // Re-throw for now, consider better concurrency handling
                }
            }

            return RedirectToPage("./Index");
        }

        private bool MedicationExists(int id)
        {
            return _context.Medications.Any(e => e.Id == id);
        }
    }
} 