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

namespace CitasEPS.Pages.Doctor
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

        public IList<Models.Patient> DoctorPatients { get; set; } = new List<Models.Patient>();
        public Models.Doctor CurrentDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (CurrentDoctor == null) return Forbid("Registro de Doctor no encontrado.");

            // Encontrar todos los pacientes únicos que han tenido citas con este doctor
            DoctorPatients = await _context.Appointments
                .Where(a => a.DoctorId == CurrentDoctor.Id)
                .Select(a => a.Patient) // Seleccionar solo el paciente de cada cita
                .Distinct() // Obtener solo pacientes únicos
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName) // Ordenar por apellido, luego nombre
                .ToListAsync();

            return Page();
        }
    }
} 