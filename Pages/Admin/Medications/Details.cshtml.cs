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
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Medication? Medication { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Solicitud de detalles de medicamento con ID nulo.");
                return Page();
            }

            _logger.LogInformation("Cargando detalles para Medicamento ID {MedicationId}.", id.Value);
            Medication = await _context.Medications
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (Medication == null)
            {
                _logger.LogWarning("Detalles de Medicamento ID {MedicationId} no encontrado.", id.Value);
            }
            
            return Page();
        }
    }
}