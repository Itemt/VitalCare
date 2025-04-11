using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public IList<Models.Doctor> Doctors { get; set; } = default!;

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cargando página de gestión de médicos.");
            Doctors = await _context.Doctors
                            .Include(d => d.Specialty)
                            .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
                            .ToListAsync();
        }
    }
} 