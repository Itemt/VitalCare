using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageSpecialtiesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageSpecialtiesModel> _logger;

        public ManageSpecialtiesModel(ApplicationDbContext context, ILogger<ManageSpecialtiesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Specialty> Specialties { get; set; } = default!;

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cargando página de gestión de especialidades.");
            // Futuro: Cargar especialidades desde la base de datos
            // Specialties = await _context.Specialties.ToListAsync();
            Specialties = new List<Specialty>(); // Placeholder / Temporal
        }
    }
} 