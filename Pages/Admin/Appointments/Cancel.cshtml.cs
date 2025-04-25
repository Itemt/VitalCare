using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Appointments;

[Authorize(Roles = "Admin")]
public class CancelModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CancelModel> _logger;

    public CancelModel(ApplicationDbContext context, ILogger<CancelModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public Appointment? Appointment { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Cargando confirmación de cancelación para Cita ID {AppointmentId}.", id);

        Appointment = await _context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Doctor)
                                    .ThenInclude(d => d.Specialty)
                                // Don't use AsNoTracking here, as we might want to delete this object later
                                .FirstOrDefaultAsync(m => m.Id == id);

        if (Appointment == null)
        {
            _logger.LogWarning("Intento de cancelación para Cita ID {AppointmentId} no encontrada (GET).", id);
             TempData["WarningMessage"] = $"La cita con ID {id} no fue encontrada.";
            return RedirectToPage("../ManageAppointments");
        }
        
        // Prevent cancelling past appointments from the confirmation page directly
        if (Appointment.AppointmentDateTime < DateTime.Now)
        {
             _logger.LogWarning("Intento de cancelar una cita pasada (ID: {AppointmentId}) desde la página de confirmación.", id);
            TempData["ErrorMessage"] = "No se pueden cancelar citas que ya han ocurrido.";
            return RedirectToPage("../ManageAppointments");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Re-fetch the appointment to ensure it exists and is still in the future
        var appointmentToCancel = await _context.Appointments.FindAsync(id);

        if (appointmentToCancel == null)
        {
            _logger.LogWarning("Intento de cancelación para Cita ID {AppointmentId} no encontrada (POST).", id);
            TempData["WarningMessage"] = $"La cita con ID {id} ya no existe o fue cancelada por otro usuario.";
            return RedirectToPage("../ManageAppointments");
        }
        
        if (appointmentToCancel.AppointmentDateTime < DateTime.Now)
        {
            _logger.LogWarning("Intento de confirmar cancelación de una cita pasada (ID: {AppointmentId}).", id);
            TempData["ErrorMessage"] = "Esta cita ya ha ocurrido y no puede ser cancelada.";
            return RedirectToPage("../ManageAppointments");
        }

        try
        {
            _context.Appointments.Remove(appointmentToCancel);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cita ID {AppointmentId} cancelada exitosamente.", id);
            TempData["SuccessMessage"] = $"Cita con ID {id} cancelada exitosamente.";
            return RedirectToPage("../ManageAppointments");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al cancelar la cita ID {AppointmentId}.", id);
            TempData["ErrorMessage"] = "Ocurrió un error al cancelar la cita. Por favor, inténtelo de nuevo.";
            // Optionally, return to the confirmation page if needed, reloading the data
            // Appointment = await _context.Appointments... // Re-fetch details if returning Page()
            // return Page();
            return RedirectToPage("./Details", new { id = id }); // Redirect back to details on error might be better UX
        }
    }
}
