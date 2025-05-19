using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Appointments;

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
    public Appointment? Appointment { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Cargando confirmación de eliminación para Cita ID {AppointmentId}.", id);

        Appointment = await _context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Doctor)
                                    .ThenInclude(d => d.Specialty)
                                .FirstOrDefaultAsync(m => m.Id == id);

        if (Appointment == null)
        {
            _logger.LogWarning("Intento de eliminación para Cita ID {AppointmentId} no encontrada (GET).", id);
             TempData["WarningMessage"] = $"La cita con ID {id} no fue encontrada.";
            return RedirectToPage("../ManageAppointments");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var appointmentToDelete = await _context.Appointments.FindAsync(id);

        if (appointmentToDelete == null)
        {
            _logger.LogWarning("Intento de eliminación para Cita ID {AppointmentId} no encontrada (POST).", id);
            TempData["WarningMessage"] = $"La cita con ID {id} ya no existe o fue eliminada por otro usuario.";
            return RedirectToPage("../ManageAppointments");
        }

        try
        {
            _context.Appointments.Remove(appointmentToDelete);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cita ID {AppointmentId} eliminada exitosamente.", id);
            TempData["SuccessMessage"] = $"Cita con ID {id} eliminada exitosamente.";
            return RedirectToPage("../ManageAppointments");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al eliminar la cita ID {AppointmentId}.", id);
            TempData["ErrorMessage"] = "Ocurrió un error al eliminar la cita. Por favor, inténtelo de nuevo.";
            return RedirectToPage("./Details", new { id = id }); 
        }
    }
} 