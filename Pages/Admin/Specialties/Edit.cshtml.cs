using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Specialties;

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
    public Specialty? Specialty { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Cargando datos de Especialidad ID {SpecialtyId} para editar.", id);
        Specialty = await _context.Specialties.FindAsync(id);

        if (Specialty == null)
        {
            _logger.LogWarning("Especialidad ID {SpecialtyId} no encontrada para editar (GET).", id);
             TempData["WarningMessage"] = $"La especialidad con ID {id} no fue encontrada.";
            return RedirectToPage("../ManageSpecialties");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Specialty == null || !ModelState.IsValid)
        {
             _logger.LogWarning("Edición de especialidad fallida debido a errores de modelo o propiedad nula.");
            // If Specialty is null, we cannot proceed, return to list
             if (Specialty == null) return RedirectToPage("../ManageSpecialties");
            return Page(); // Return page with validation errors
        }

        // Normalize name for comparison
        var normalizedNewName = Specialty.Name.Trim();
         if (string.IsNullOrEmpty(normalizedNewName))
        {
             ModelState.AddModelError("Specialty.Name", "El nombre de la especialidad no puede estar vacío.");
             return Page();
        }

        // Check if another specialty already exists with the new name (case-insensitive)
        bool nameExists = await _context.Specialties
            .AnyAsync(s => s.Id != Specialty.Id && s.Name.ToUpper() == normalizedNewName.ToUpper());

        if (nameExists)
        {
            _logger.LogWarning("Intento de renombrar especialidad ID {SpecialtyId} a un nombre duplicado: {SpecialtyName}", Specialty.Id, normalizedNewName);
            ModelState.AddModelError("Specialty.Name", $"Ya existe otra especialidad con el nombre '{normalizedNewName}'.");
            return Page();
        }

        // Assign normalized name before attempting to save
        Specialty.Name = normalizedNewName;

        _context.Attach(Specialty).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Especialidad ID {SpecialtyId} actualizada exitosamente a '{SpecialtyName}'.", Specialty.Id, Specialty.Name);
            TempData["SuccessMessage"] = $"Especialidad '{Specialty.Name}' actualizada exitosamente.";
            return RedirectToPage("../ManageSpecialties");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Error de concurrencia al actualizar Especialidad ID {SpecialtyId}.", Specialty.Id);
            if (!await SpecialtyExistsAsync(Specialty.Id))
            {
                 TempData["ErrorMessage"] = "La especialidad que intentaba editar fue eliminada por otro usuario.";
                 return RedirectToPage("../ManageSpecialties");
            }
            else
            {
                 ModelState.AddModelError(string.Empty, "El registro fue modificado por otro usuario después de que usted lo cargó. " +
                                                    "Sus cambios no se guardaron. Intente de nuevo.");
                // Optionally reload data from DB
                // var currentDbValues = await _context.Specialties.FindAsync(Specialty.Id);
                // if (currentDbValues != null) { Specialty = currentDbValues; }
                return Page();
            }
        }
         catch (DbUpdateException ex)
        {
             _logger.LogError(ex, "Error al guardar la especialidad actualizada ID {SpecialtyId}.", Specialty.Id);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los cambios. Por favor, inténtelo de nuevo.");
            return Page();
        }
    }

    private Task<bool> SpecialtyExistsAsync(int id)
    {
        return _context.Specialties.AnyAsync(e => e.Id == id);
    }
}
