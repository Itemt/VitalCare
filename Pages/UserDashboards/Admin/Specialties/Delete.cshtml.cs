using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.UserDashboards.Admin.Specialties;

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
    public Specialty? Specialty { get; set; }
    public bool HasAssociatedDoctors { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Cargando confirmación de eliminación para Especialidad ID {SpecialtyId}.", id);
        Specialty = await _context.Specialties.FindAsync(id);

        if (Specialty == null)
        {
            _logger.LogWarning("Intento de eliminación para Especialidad ID {SpecialtyId} no encontrada (GET).", id);
            TempData["WarningMessage"] = $"La especialidad con ID {id} no fue encontrada.";
            return RedirectToPage("../ManageSpecialties");
        }

        // Check for associated doctors
        HasAssociatedDoctors = await _context.Doctors.AnyAsync(d => d.SpecialtyId == id);
        if (HasAssociatedDoctors)
        {
             _logger.LogWarning("Intento de eliminación de Especialidad ID {SpecialtyId} ('{SpecialtyName}') fallido: hay médicos asociados.", id, Specialty.Name);
             // Message is handled in the View based on HasAssociatedDoctors flag
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var specialtyToDelete = await _context.Specialties.FindAsync(id);

        if (specialtyToDelete == null)
        {
             _logger.LogWarning("Intento de eliminación para Especialidad ID {SpecialtyId} no encontrada (POST).", id);
            TempData["WarningMessage"] = $"La especialidad con ID {id} ya no existe o fue eliminada.";
            return RedirectToPage("../ManageSpecialties");
        }

        // Re-check for associated doctors before deleting
        HasAssociatedDoctors = await _context.Doctors.AnyAsync(d => d.SpecialtyId == id);
        if (HasAssociatedDoctors)
        {
             _logger.LogWarning("Confirmación de eliminación de Especialidad ID {SpecialtyId} fallida: médicos asociados encontrados.", id);
             TempData["ErrorMessage"] = "No se puede eliminar esta especialidad porque tiene médicos asociados.";
            // Need to re-populate Specialty for the view if returning Page() showing the error
            Specialty = specialtyToDelete;
             return Page(); // Return to the delete confirmation page showing the error
        }

        try
        {
            _context.Specialties.Remove(specialtyToDelete);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Especialidad ID {SpecialtyId} ('{SpecialtyName}') eliminada exitosamente.", id, specialtyToDelete.Name);
            TempData["SuccessMessage"] = $"Especialidad '{specialtyToDelete.Name}' eliminada exitosamente.";
            return RedirectToPage("../ManageSpecialties");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al eliminar la especialidad ID {SpecialtyId}.", id);
            TempData["ErrorMessage"] = "Ocurrió un error al eliminar la especialidad. Por favor, inténtelo de nuevo.";
            // Re-populate Specialty for the view
            Specialty = specialtyToDelete;
            return Page(); // Return to confirmation page on error
        }
    }
}




