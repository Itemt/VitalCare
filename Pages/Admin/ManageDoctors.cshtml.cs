using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageDoctorsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageDoctorsModel> _logger;

        public ManageDoctorsModel(ApplicationDbContext context, ILogger<ManageDoctorsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Doctor> Doctors { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Futuro: Cargar médicos desde la base de datos
             _logger.LogInformation("Cargando página de gestión de médicos.");
            // Doctors = await _context.Doctors.Include(d => d.Specialty).ToListAsync();
            Doctors = new List<Doctor>(); // Placeholder / Temporal
        }
    }
} 