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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Medication Medication { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Solicitud de edición de medicamento con ID nulo.");
                TempData["WarningMessage"] = "No se especificó un ID de medicamento válido.";
                return RedirectToPage("./Index");
            }

            _logger.LogInformation("Cargando medicamento ID {MedicationId} para editar.", id.Value);
            var medication = await _context.Medications.FindAsync(id.Value);

            if (medication == null)
            {
                _logger.LogWarning("Intento de editar medicamento ID {MedicationId} no encontrado (GET).", id.Value);
                TempData["WarningMessage"] = $"El medicamento con ID {id.Value} no fue encontrado.";
                return RedirectToPage("./Index");
            }
            Medication = medication;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Medication.Name = Medication.Name?.Trim() ?? string.Empty;
            Medication.Description = Medication.Description?.Trim();

            ModelState.Clear();
            if (!TryValidateModel(Medication, nameof(Medication)))
            {
                _logger.LogWarning("Edición de medicamento ID {MedicationId} fallida debido a errores de modelo después de normalizar.", id);
                return Page();
            }

            if (string.IsNullOrEmpty(Medication.Name))
            {
                ModelState.AddModelError("Medication.Name", "El nombre del medicamento no puede estar vacío.");
                return Page();
            }

            bool duplicateExists = await _context.Medications
                .AnyAsync(m => m.Id != id && m.Name.ToUpper() == Medication.Name.ToUpper());

            if (duplicateExists)
            {
                _logger.LogWarning("Intento de actualizar medicamento ID {CurrentId} a un nombre duplicado: {DuplicateName}", id, Medication.Name);
                ModelState.AddModelError("Medication.Name", $"Ya existe otro medicamento con el nombre '{Medication.Name}'.");
                return Page();
            }

            var medicationToUpdate = await _context.Medications.FindAsync(id);

            if (medicationToUpdate == null)
            {
                _logger.LogWarning("Intento de actualizar medicamento ID {MedicationId} no encontrado (POST).", id);
                TempData["WarningMessage"] = $"El medicamento que intentaba editar ya no existe.";
                return RedirectToPage("./Index");
            }

            medicationToUpdate.Name = Medication.Name;
            medicationToUpdate.Description = Medication.Description;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Medicamento ID {MedicationId} actualizado exitosamente a '{NewName}'.", id, medicationToUpdate.Name);
                TempData["SuccessMessage"] = $"Medicamento '{medicationToUpdate.Name}' actualizado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al actualizar medicamento ID {MedicationId}.", id);
                ModelState.AddModelError(string.Empty, "El registro fue modificado por otro usuario después de que usted lo cargó. " +
                                                "La operación de guardado fue cancelada. Si aún desea editar este registro, " +
                                                "vuelva a cargarlo.");
                Medication = medicationToUpdate;
                ModelState.Remove("Medication");
                return Page();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar cambios para medicamento ID {MedicationId}.", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los cambios. Por favor, inténtelo de nuevo.");
                return Page();
            }
        }
    }
}