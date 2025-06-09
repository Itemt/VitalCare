using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PatientModel = CitasEPS.Models.Modules.Users.Patient;

namespace CitasEPS.Pages.UserDashboards.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManagePatientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagePatientsModel> _logger;

        public ManagePatientsModel(ApplicationDbContext context, ILogger<ManagePatientsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<PatientModel> Patients { get; set; } = default!;

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cargando lista de pacientes para administración.");
            
            Patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
                
            _logger.LogInformation("Se cargaron {Count} pacientes.", Patients.Count);
        }
    }
} 




