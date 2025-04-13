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
    [Authorize(Roles = "Doctor")] // Solo los doctores pueden acceder
    public class AgendaModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AgendaModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Models.Appointment> Appointments { get; set; } = new List<Models.Appointment>();
        public Models.Doctor? CurrentDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // Encontrar el registro Doctor asociado con el User logueado (por email)
            CurrentDoctor = await _context.Doctors
                                        .FirstOrDefaultAsync(d => d.Email == user.Email);

            if (CurrentDoctor == null)
            {
                // Considerar qué hacer si un User con rol Doctor no tiene un registro Doctor asociado
                 return NotFound("Registro de Doctor no encontrado para este usuario.");
                 // O quizás redirigir a una página de error o perfil incompleto
            }

            // Obtener las citas asignadas a este doctor, incluyendo datos del paciente
            Appointments = await _context.Appointments
                .Where(a => a.DoctorId == CurrentDoctor.Id)
                .Include(a => a.Patient) // Incluir datos del paciente
                .OrderBy(a => a.AppointmentDateTime) // Ordenar por fecha/hora
                .ToListAsync();

            return Page();
        }
    }
} 