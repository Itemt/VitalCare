using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Admin.Medications
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Medication Medication { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication = await _context.Medications.FirstOrDefaultAsync(m => m.Id == id);

            if (medication == null)
            {
                return NotFound();
            }
            else
            {
                Medication = medication;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medication = await _context.Medications.FindAsync(id);
            if (medication != null)
            {
                Medication = medication;
                try
                {
                    _context.Medications.Remove(Medication);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Medicamento ID {Medication.Id} - '{Medication.Name}' eliminado exitosamente.");
                    TempData["SuccessMessage"] = "Medicamento eliminado exitosamente.";
                }
                catch (DbUpdateException ex)
                {
                    // Log the error (uncomment ex variable name and add logging)
                    _logger.LogError(ex, $"Error eliminando medicamento ID {Medication.Id} - '{Medication.Name}'. Puede estar en uso.");
                    // Considerar si hay prescripciones que usan este medicamento
                     ModelState.AddModelError(string.Empty, "Error eliminando el medicamento. Puede que esté asociado a prescripciones existentes.");
                    return Page(); // Devolver la página para mostrar el error
                }
            }

            return RedirectToPage("./Index");
        }
    }
} 