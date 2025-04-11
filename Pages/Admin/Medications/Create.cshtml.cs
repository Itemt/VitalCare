using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.Admin.Medications
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Medication Medication { get; set; } = default!;
        

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
          if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Medications.Add(Medication);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Medicamento creado exitosamente.";

            return RedirectToPage("./Index");
        }
    }
} 