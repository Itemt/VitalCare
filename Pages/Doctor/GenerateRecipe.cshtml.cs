using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class GenerateRecipeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public GenerateRecipeModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Appointment> CompletedAppointments { get; set; } = default!;
        public Models.Doctor? CurrentDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Obtener el usuario actual
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Obtener el doctor actual
            CurrentDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (CurrentDoctor == null)
            {
                return NotFound("Doctor no encontrado");
            }

            // Obtener las citas completadas del doctor con sus prescripciones
            CompletedAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Prescriptions)
                    .ThenInclude(p => p.Medication)
                .Where(a => a.DoctorId == CurrentDoctor.Id && 
                           a.IsConfirmed && 
                           !a.IsCancelled &&
                           a.AppointmentDateTime < DateTime.Now) // Citas pasadas (completadas)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return Page();
        }
    }
} 