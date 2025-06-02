using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Patients;

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
    public CitasEPS.Models.Patient? Patient { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Patient = await _context.Patients
                              .AsNoTracking() // Read-only for confirmation
                              .FirstOrDefaultAsync(m => m.Id == id);

        if (Patient == null)
        {
            _logger.LogWarning("Intento de eliminación para Paciente ID {PatientId} no encontrado (GET).", id);
            // Setting Patient to null will trigger the warning in the view
            return Page(); // Return Page so the view can show the 'not found' message
            // Alternatively: return NotFound(...);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Re-fetch the entity to ensure it exists before attempting deletion
        var patientToDelete = await _context.Patients.FindAsync(id);

        if (patientToDelete == null)
        {
            _logger.LogWarning("Intento de eliminación para Paciente ID {PatientId} no encontrado (POST).", id);
            TempData["WarningMessage"] = $"El paciente con ID {id} ya no existe o fue eliminado por otro usuario.";
            return RedirectToPage("../ManagePatients");
        }

        // Optional: Check for dependencies (e.g., appointments)
        bool hasAppointments = await _context.Appointments.AnyAsync(a => a.PatientId == id);
        if (hasAppointments) 
        {
             _logger.LogWarning("Intento de eliminación fallido para Paciente ID {PatientId} debido a citas asociadas.", id);
            TempData["ErrorMessage"] = "Este paciente no puede ser eliminado porque tiene citas médicas registradas.";
             // Re-populate Patient property for the view display if returning Page()
            Patient = patientToDelete; // Assign the fetched entity back to the property
            return Page();
        }

        try
        {
            _context.Patients.Remove(patientToDelete);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Paciente ID {PatientId} ({FullName}) eliminado exitosamente.", patientToDelete.Id, patientToDelete.FullName);
            TempData["SuccessMessage"] = $"Paciente '{patientToDelete.FullName}' eliminado exitosamente.";
            return RedirectToPage("../ManagePatients");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al eliminar el paciente ID {PatientId}.", id);
            TempData["ErrorMessage"] = "Ocurrió un error al eliminar el paciente. Por favor, inténtelo de nuevo o contacte al administrador.";
            // Re-populate Patient property for the view display
             Patient = patientToDelete;
            return Page(); 
        }
    }
}
