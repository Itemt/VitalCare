using System.Linq; 
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
        public Medication Medication { get; set; } = new(); 
        public bool HasAssociatedPrescriptions { get; set; } 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Solicitud de eliminación de medicamento con ID nulo.");
                TempData["WarningMessage"] = "No se especificó un ID de medicamento válido para eliminar.";
                return RedirectToPage("./Index");
            }

            _logger.LogInformation("Cargando confirmación de eliminación para Medicamento ID {MedicationId}.", id.Value);
            Medication = await _context.Medications.FindAsync(id.Value);

            if (Medication == null)
            {
                _logger.LogWarning("Intento de eliminar medicamento ID {MedicationId} no encontrado (GET).", id.Value);
                TempData["WarningMessage"] = $"El medicamento con ID {id.Value} no fue encontrado.";
                return RedirectToPage("./Index");
            }

            try 
            {
                HasAssociatedPrescriptions = await _context.Prescriptions.AnyAsync(p => p.MedicationId == id.Value);
                if (HasAssociatedPrescriptions)
                {
                    _logger.LogWarning("Intento de eliminación de Medicamento ID {MedicationId} ('{MedicationName}') fallido: hay prescripciones asociadas.", id.Value, Medication.Name);
                     // Message is handled in the View based on HasAssociatedPrescriptions flag
                }
            }
            catch (Exception ex) 
            {
                 _logger.LogError(ex, "Error al verificar prescripciones asociadas para Medicamento ID {MedicationId}.", id.Value);
                 // Decide how to handle this - maybe allow deletion but log error, or prevent deletion.
                 // For safety, let's prevent deletion if we can't check.
                 ModelState.AddModelError(string.Empty, "No se pudo verificar si hay prescripciones asociadas. No se puede eliminar por seguridad.");
                 // We'll treat this like having associations for the UI
                 HasAssociatedPrescriptions = true; 
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id) 
        {
            var medicationToDelete = await _context.Medications.FindAsync(id);

            if (medicationToDelete == null)
            {
                _logger.LogWarning("Intento de confirmar eliminación de medicamento ID {MedicationId} no encontrado (POST).", id);
                TempData["WarningMessage"] = $"El medicamento que intentaba eliminar ya no existe.";
                return RedirectToPage("./Index");
            }

            try 
            {
                HasAssociatedPrescriptions = await _context.Prescriptions.AnyAsync(p => p.MedicationId == id);
                if (HasAssociatedPrescriptions)
                {
                    _logger.LogWarning("Confirmación de eliminación de Medicamento ID {MedicationId} fallida: prescripciones asociadas encontradas.", id);
                    ModelState.AddModelError(string.Empty, "No se puede eliminar este medicamento porque está asociado a prescripciones existentes.");
                    Medication = medicationToDelete;
                    return Page(); 
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error al re-verificar prescripciones asociadas para Medicamento ID {MedicationId} durante POST.", id);
                 ModelState.AddModelError(string.Empty, "No se pudo verificar si hay prescripciones asociadas. No se puede eliminar por seguridad.");
                 Medication = medicationToDelete; 
                 HasAssociatedPrescriptions = true; 
                 return Page();
            }

            try
            {
                _context.Medications.Remove(medicationToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Medicamento ID {MedicationId} ('{MedicationName}') eliminado exitosamente.", id, medicationToDelete.Name);
                TempData["SuccessMessage"] = $"Medicamento '{medicationToDelete.Name}' eliminado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al eliminar el medicamento ID {MedicationId}.", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al eliminar el medicamento. Por favor, inténtelo de nuevo.");
                Medication = medicationToDelete;
                return Page(); 
            }
        }
    }
}