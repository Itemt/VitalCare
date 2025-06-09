using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; 
using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging; 

namespace CitasEPS.Pages.UserDashboards.Admin.Medications
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger; 

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger) 
        {
            _context = context;
            _logger = logger; 
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Medication Medication { get; set; } = new(); 


        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            // Trim input
            Medication.Name = Medication.Name?.Trim() ?? string.Empty;
            Medication.Description = Medication.Description?.Trim(); 

            // Re-validate after trimming
            ModelState.Clear(); 
            if (!TryValidateModel(Medication, nameof(Medication))) 
            {
                 _logger.LogWarning("Creación de medicamento fallida debido a errores de modelo después de normalizar.");
                return Page();
            }
            
            // Basic check for empty name after trimming
            if (string.IsNullOrEmpty(Medication.Name))
            {
                ModelState.AddModelError("Medication.Name", "El nombre del medicamento no puede estar vacío.");
                return Page();
            }

            // Check if medication already exists (case-insensitive)
            bool medicationExists = await _context.Medications
                .AnyAsync(m => m.Name.ToUpper() == Medication.Name.ToUpper());

            if (medicationExists)
            {
                _logger.LogWarning("Intento de crear medicamento duplicado: {MedicationName}", Medication.Name);
                ModelState.AddModelError("Medication.Name", $"El medicamento '{Medication.Name}' ya existe.");
                return Page();
            }

            _context.Medications.Add(Medication);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Nuevo medicamento '{MedicationName}' (ID: {MedicationId}) creado exitosamente.", Medication.Name, Medication.Id);
                TempData["SuccessMessage"] = $"Medicamento '{Medication.Name}' creado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar el nuevo medicamento '{MedicationName}'.", Medication.Name);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el medicamento. Por favor, inténtelo de nuevo.");
                return Page();
            }
        }
    }
}




