using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Controllers
{
    [Authorize(Roles = "Doctor")]
    [Route("api/doctor")]
    [ApiController]
    public class DoctorApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DoctorApiController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/doctor/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Usuario no encontrado");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Email == user.Email);

                if (doctor == null)
                {
                    return NotFound("Doctor no encontrado");
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Citas para hoy
                var todayAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor.Id && 
                               a.AppointmentDateTime >= today && 
                               a.AppointmentDateTime < tomorrow &&
                               !a.IsCancelled);

                // Citas confirmadas (pendientes de completar)
                var confirmedAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor.Id && 
                               a.IsConfirmed && 
                               !a.IsCompleted && 
                               !a.IsCancelled &&
                               a.AppointmentDateTime >= DateTime.UtcNow);

                // Promedio de satisfacción basado en ratings
                var completedAppointmentsWithRatings = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id && 
                               a.IsCompleted && 
                               a.Rating != null && 
                               a.Rating.PatientRating > 0)
                    .Include(a => a.Rating)
                    .ToListAsync();

                double satisfactionPercentage = 0;
                if (completedAppointmentsWithRatings.Any())
                {
                    var averageRating = completedAppointmentsWithRatings
                        .Average(a => a.Rating!.PatientRating);
                    satisfactionPercentage = Math.Round((averageRating / 5.0) * 100, 0);
                }

                return Ok(new
                {
                    todayAppointments,
                    confirmedAppointments,
                    satisfactionPercentage = (int)satisfactionPercentage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET: api/doctor/search-patients
        [HttpGet("search-patients")]
        public async Task<ActionResult<object>> SearchPatients([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return Ok(new List<object>());
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Usuario no encontrado");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Email == user.Email);

                if (doctor == null)
                {
                    return NotFound("Doctor no encontrado");
                }

                // Buscar pacientes que han tenido citas con este doctor
                var patients = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => a.Patient)
                    .Distinct()
                    .Where(p => p.FirstName.Contains(term) || 
                               p.LastName.Contains(term) ||
                               p.DocumentId.Contains(term))
                    .Select(p => new
                    {
                        id = p.Id,
                        fullName = p.FullName,
                        documentId = p.DocumentId,
                        phoneNumber = p.PhoneNumber
                    })
                    .Take(10)
                    .ToListAsync();

                return Ok(patients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message });
            }
        }
    }
} 