using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.UserDashboards.Admin.Appointments;

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

    public Appointment? Appointment { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Cargando detalles para Cita ID {AppointmentId}.", id);

        Appointment = await _context.Appointments
                                                .Include(a => a.Patient) // Include Patient details
                .Include(a => a.Doctor) // Include Doctor details
                .ThenInclude(d => d.Specialty) // Then include Specialty details via Doctor
                                .AsNoTracking() // Read-only view
                                .FirstOrDefaultAsync(m => m.Id == id);

        if (Appointment == null)
        {
            _logger.LogWarning("Detalles de Cita ID {AppointmentId} no encontrado.", id);
            // Let the view handle the null Appointment property
            return Page(); // Return Page so the view can display the 'not found' message
            // Alternatively: return NotFound($"Cita con ID {id} no encontrada.");
        }

        return Page();
    }
}




