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
             _logger.LogInformation("Cargando página de gestión de citas para admin.");
            // Futuro: Cargar citas desde BD, potencialmente con includes para Paciente, Médico, Médico.Especialidad
            // Appointments = await _context.Appointments
            //      .Include(a => a.Patient)
            //      .Include(a => a.Doctor)
            //          .ThenInclude(d => d.Specialty) // Incluir especialidad del médico
            //      .OrderByDescending(a => a.AppointmentDateTime)
            //      .ToListAsync();
            Appointments = new List<Appointment>(); // Placeholder / Temporal
        }
    }
} 