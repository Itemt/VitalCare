using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PrescriptionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PrescriptionsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Prescription> Prescriptions { get; set; } = new List<Prescription>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (currentDoctor == null) return Forbid("El usuario actual no es un Doctor registrado.");

            // Obtener todas las prescripciones del doctor que tienen cita asociada
            Prescriptions = await _context.Prescriptions
                .Where(p => p.DoctorId == currentDoctor.Id && p.AppointmentId.HasValue)
                .Include(p => p.Patient)
                .Include(p => p.Medication)
                .Include(p => p.Appointment)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return Page();
        }
    }
} 