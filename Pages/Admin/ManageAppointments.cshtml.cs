using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageAppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageAppointmentsModel> _logger;

        public ManageAppointmentsModel(ApplicationDbContext context, ILogger<ManageAppointmentsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Appointment> Appointments { get; set; } = default!;

        public async Task OnGetAsync()
        {
             _logger.LogInformation("Loading appointment management page for admin.");
            // Future: Load appointments from database, potentially with includes for Patient, Doctor, Doctor.Specialty
            // Appointments = await _context.Appointments
            //      .Include(a => a.Patient)
            //      .Include(a => a.Doctor)
            //          .ThenInclude(d => d.Specialty) // Include doctor's specialty
            //      .OrderByDescending(a => a.AppointmentDateTime)
            //      .ToListAsync();
            Appointments = new List<Appointment>(); // Placeholder
        }
    }
} 