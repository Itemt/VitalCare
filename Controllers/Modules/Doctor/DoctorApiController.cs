using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
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
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

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

                // Citas confirmadas (pendientes de completar) - Corregir problema de zona horaria
                var now = DateTime.Now; // Usar hora local en lugar de UTC
                var confirmedAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor.Id && 
                               a.IsConfirmed && 
                               !a.IsCompleted && 
                               !a.IsCancelled);
                               // Remover la condición de fecha futura por ahora para debugging

                // Promedio de satisfacción basado en ratings
                var completedAppointmentsWithRatings = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id &&
                               a.IsCompleted &&
                               a.Rating != null) // Ensure Rating object exists
                    .Select(a => a.Rating) // Select the Rating object
                    .ToListAsync();

                double satisfactionPercentage = 0;
                if (completedAppointmentsWithRatings.Any(r => r.PatientRating > 0))
                {
                    // Filter again for valid ratings before averaging
                    var averageRating = completedAppointmentsWithRatings
                        .Where(r => r.PatientRating > 0)
                        .Average(r => r.PatientRating);
                    satisfactionPercentage = Math.Round((averageRating / 5.0) * 100, 0);
                }
                else
                {
                    // Si no hay ratings aún, calcular basado en citas completadas sin rating
                    var totalCompletedAppointments = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.IsCompleted);
                    
                    if (totalCompletedAppointments > 0)
                    {
                        // Mostrar 0% si hay citas completadas pero sin rating
                        satisfactionPercentage = 0;
                    }
                    // Si no hay citas completadas, mantener 0%
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
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound("Doctor no encontrado");
                }

                // Buscar pacientes que han tenido citas con este doctor
                var patients = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => a.Patient)
                    .Distinct()
                    .Where(p => p != null && (p.FirstName.Contains(term) || 
                               p.LastName.Contains(term) ||
                               (p.DocumentId != null && p.DocumentId.Contains(term))))
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

        // GET: api/doctor/debug-appointments - Debug endpoint
        [HttpGet("debug-appointments")]
        public async Task<ActionResult<object>> GetDebugAppointments()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Usuario no encontrado");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound("Doctor no encontrado");
                }

                var allAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Include(a => a.Patient)
                    .Include(a => a.Rating)
                    .Select(a => new
                    {
                        id = a.Id,
                        patientName = a.Patient != null ? a.Patient.FullName : "N/A",
                        appointmentDateTime = a.AppointmentDateTime,
                        isConfirmed = a.IsConfirmed,
                        isCompleted = a.IsCompleted,
                        isCancelled = a.IsCancelled,
                        hasRating = a.Rating != null,
                        patientRating = a.Rating != null ? a.Rating.PatientRating : 0,
                        notes = a.Notes
                    })
                    .OrderBy(a => a.appointmentDateTime)
                    .ToListAsync();

                var stats = new
                {
                    totalAppointments = allAppointments.Count,
                    confirmedAppointments = allAppointments.Count(a => a.isConfirmed && !a.isCompleted && !a.isCancelled),
                    completedAppointments = allAppointments.Count(a => a.isCompleted),
                    cancelledAppointments = allAppointments.Count(a => a.isCancelled),
                    appointmentsWithRating = allAppointments.Count(a => a.hasRating),
                    doctorId = doctor.Id,
                    doctorName = doctor.FullName,
                    currentDateTime = DateTime.Now,
                    appointments = allAppointments
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message });
            }
        }
    }
} 




