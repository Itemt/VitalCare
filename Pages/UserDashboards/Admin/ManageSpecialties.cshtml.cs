using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Admin
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
            Specialties = await _context.Specialties
                               .OrderBy(s => s.Name)
                               .ToListAsync();
        }
    }
} 




