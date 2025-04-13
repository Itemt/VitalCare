using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")] // Ensure only Admins can access
    public class ManagePatientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManagePatientsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Patient> Patients { get;set; } = new List<Patient>();

        public async Task OnGetAsync()
        {
            Patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }
    }
} 