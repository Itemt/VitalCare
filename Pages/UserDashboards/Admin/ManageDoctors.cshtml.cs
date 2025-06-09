using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Admin
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

        public IList<DoctorModel> Doctors { get; set; } = default!;

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cargando lista de doctores para administración.");
            
            Doctors = await _context.Doctors
                .Include(d => d.Specialty)
                .Include(d => d.User)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
                
            _logger.LogInformation("Se cargaron {Count} doctores.", Doctors.Count);
        }
    }
} 





