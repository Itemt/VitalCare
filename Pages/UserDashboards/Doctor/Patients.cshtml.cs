using CitasEPS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using PatientModel = CitasEPS.Models.Modules.Users.Patient;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PatientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<PatientModel> DoctorPatients { get; set; } = new List<PatientModel>();
        public DoctorModel? CurrentDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (CurrentDoctor == null) return Forbid("Registro de Doctor no encontrado.");

            // Encontrar todos los pacientes únicos que han tenido citas con este Doctor
            DoctorPatients = await _context.Appointments
                .Where(a => a.DoctorId == CurrentDoctor.Id)
                .Select(a => a.Patient)
                .Where(p => p != null)
                .Distinct()
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new PatientModel
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    DocumentId = p.DocumentId,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    UserId = p.UserId,
                    User = p.User,
                    Appointments = p.Appointments
                }).ToListAsync();

            return Page();
        }
    }
} 





