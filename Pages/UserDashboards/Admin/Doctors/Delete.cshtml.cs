using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Admin.Doctors;

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
    public DoctorModel Doctor { get; set; } = default!;
    public bool HasAssociatedAppointments { get; set; } = false;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var doctor = await _context.Doctors
                                 .Include(d => d.Specialty) // Include Specialty for display
                                 .AsNoTracking() // No need to track for deletion confirmation view
                                 .FirstOrDefaultAsync(m => m.Id == id);

        if (doctor == null)
        {
             _logger.LogWarning("Intento de eliminación para Médico ID {DoctorId} no encontrado (GET).", id);
            return NotFound($"Médico con ID {id} no encontrado.");
        }

        Doctor = doctor;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // It's safer to re-fetch the entity before deleting it
        var doctorToDelete = await _context.Doctors.FindAsync(id);

        if (doctorToDelete == null)
        {
            _logger.LogWarning("Intento de eliminación para Médico ID {DoctorId} no encontrado (POST).", id);
            // Optionally add a TempData message indicating it was already deleted or not found
            TempData["WarningMessage"] = $"El médico con ID {id} ya no existe.";
            return RedirectToPage("../ManageDoctors"); 
        }

        // Optional: Check for related data (e.g., appointments) before deleting
        // bool hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id);
        // if (hasAppointments) 
        // {
        //     ModelState.AddModelError(string.Empty, "Este médico no puede ser eliminado porque tiene citas asociadas.");
        //     // Re-populate needed data for the view if returning Page()
        //     Doctor = await _context.Doctors.Include(d => d.Specialty).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ?? doctorToDelete;
        //     return Page();
        // }

        try
        {
            _context.Doctors.Remove(doctorToDelete);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Médico ID {DoctorId} ({FullName}) eliminado exitosamente.", doctorToDelete.Id, doctorToDelete.FullName);
            TempData["SuccessMessage"] = $"Médico '{doctorToDelete.FullName}' eliminado exitosamente.";
            return RedirectToPage("../ManageDoctors");
        }
        catch (DbUpdateException ex)
        {
            // Log the error
            _logger.LogError(ex, "Error al eliminar el médico ID {DoctorId}.", id);
            // Add a model error or TempData to inform the user
            TempData["ErrorMessage"] = "Ocurrió un error al eliminar el médico. Puede que tenga datos relacionados (citas) que impiden su eliminación. Por favor, inténtelo de nuevo o contacte al administrador.";
            // Re-populate needed data before returning the page
            Doctor = await _context.Doctors.Include(d => d.Specialty).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ?? doctorToDelete;
            return Page(); // Or RedirectToPage to avoid issues with displaying the same page after error
        }
    }
}





