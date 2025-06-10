using CitasEPS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitasEPS.Models; 
using CitasEPS.Models.Modules.Users; 
using CitasEPS.Models.Modules.Medical; 
using CitasEPS.Models.Modules.Appointments; 
using CitasEPS.Models.Modules.Core;
using PatientModel = CitasEPS.Models.Modules.Users.Patient;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PatientDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientDetailsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public PatientModel? Patient { get; set; }
        public IList<Appointment> PatientAppointments { get; set; } = new List<Appointment>();
        public DoctorModel? CurrentDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync(int patientId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (CurrentDoctor == null) return Forbid("Registro de Doctor no encontrado.");

            // Get patient details
            Patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (Patient == null)
            {
                return NotFound("Paciente no encontrado.");
            }

            // Get patient appointments with this doctor
            PatientAppointments = await _context.Appointments
                .Where(a => a.PatientId == patientId && a.DoctorId == CurrentDoctor.Id)
                .Include(a => a.Rating)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return Page();
        }
    }
} 