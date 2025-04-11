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
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
} 