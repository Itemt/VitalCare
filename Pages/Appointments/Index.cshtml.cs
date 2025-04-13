using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Identity; 
using System.Security.Claims; 
using Microsoft.Extensions.Logging; 

namespace CitasEPS.Pages.Appointments
{
    [Authorize] 
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<Appointment> Appointment { get; set; } = default!;
        public string? CurrentUserName { get; set; } 
        public string? UserRole { get; set; } 
        public bool IsDoctor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found or not logged in.");
                return Challenge(); 
            }

            CurrentUserName = user.UserName; 
            Appointment = new List<Appointment>(); 

            if (await _userManager.IsInRoleAsync(user, "Patient"))
            {
                UserRole = "Patient";
                _logger.LogInformation($"Loading appointments for Patient: {user.UserName} (ID: {user.Id})");
                Appointment = await _context.Appointments
                    .Where(a => a.PatientId == user.Id) 
                    .Include(a => a.Doctor) 
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();
                 _logger.LogInformation($"Found {Appointment.Count} appointments for Patient {user.UserName}.");
            }
            else if (await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                UserRole = "Doctor";
                _logger.LogInformation($"Loading appointments for Doctor: {user.UserName} (ID: {user.Id})");
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id); 
                if (doctor != null)
                {
                    Appointment = await _context.Appointments
                                        .Where(a => a.DoctorId == doctor.Id)
                                        .Include(a => a.Patient)
                                        .Include(a => a.Doctor) 
                                        .OrderBy(a => a.AppointmentDateTime) 
                                        .ToListAsync();
                    IsDoctor = true;
                }
                else
                {
                    // Manejar el caso donde el usuario Doctor no tiene un perfil de Doctor asociado
                    _logger.LogWarning($"Usuario con rol Doctor (ID: {user.Id}) no tiene un registro Doctor asociado.");
                    Appointment = new List<Appointment>(); // Lista vacía
                }
                _logger.LogInformation($"Found {Appointment.Count} appointments for Doctor {user.UserName}.");
            }
            else
            {
                 _logger.LogWarning($"User {user.UserName} (ID: {user.Id}) has role {User.FindFirstValue(ClaimTypes.Role)} but is not handled in Appointment Index.");
                 TempData["ErrorMessage"] = "No tiene permiso para ver esta sección o su rol no está configurado.";
            }

            return Page();
        }
    }
}