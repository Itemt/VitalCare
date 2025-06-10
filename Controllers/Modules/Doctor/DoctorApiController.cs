using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
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
                    return Unauthorized(new { error = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound(new { error = "Doctor no encontrado" });
                }

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Estadísticas básicas
                int todayAppointments = 0;
                int confirmedAppointments = 0;
                int completedAppointments = 0;
                int satisfactionPercentage = 0;
                int completedPercentage = 0;

                try
                {
                    // Citas para hoy
                    todayAppointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && 
                                   a.AppointmentDateTime >= today && 
                                   a.AppointmentDateTime < tomorrow &&
                                   a.IsCancelled == false)
                        .CountAsync();

                    // Citas confirmadas (pendientes de completar)
                    confirmedAppointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && 
                                   a.IsConfirmed == true && 
                                   a.IsCompleted == false && 
                                   a.IsCancelled == false)
                        .CountAsync();

                    // Citas completadas
                    completedAppointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.IsCompleted == true)
                        .CountAsync();

                    // Calcular satisfacción usando un promedio simple si hay ratings
                    var ratingsQuery = _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.IsCompleted == true)
                        .Include(a => a.Rating)
                        .Where(a => a.Rating != null && a.Rating.PatientRating > 0);

                    if (await ratingsQuery.AnyAsync())
                    {
                        var avgRating = await ratingsQuery.AverageAsync(a => a.Rating.PatientRating);
                        satisfactionPercentage = (int)Math.Round((avgRating / 5.0) * 100, 0);
                    }
                    else
                    {
                        // Si no hay ratings, calcular basado en citas completadas vs canceladas
                        var totalNonCancelledAppointments = await _context.Appointments
                            .Where(a => a.DoctorId == doctor.Id && a.IsCancelled == false)
                            .CountAsync();

                        if (totalNonCancelledAppointments > 0)
                        {
                            satisfactionPercentage = (int)Math.Round((completedAppointments / (double)totalNonCancelledAppointments) * 100, 0);
                        }
                    }

                    // Porcentaje de citas completadas
                    var totalAppointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.IsCancelled == false)
                        .CountAsync();

                    if (totalAppointments > 0)
                    {
                        completedPercentage = (int)Math.Round((completedAppointments / (double)totalAppointments) * 100, 0);
                    }
                }
                catch (Exception queryEx)
                {
                    // Log the specific query error but continue with default values
                    Console.WriteLine($"Query error in dashboard stats: {queryEx.Message}");
                }

                return Ok(new
                {
                    todayAppointments,
                    confirmedAppointments,
                    completedAppointments,
                    satisfactionPercentage,
                    completedPercentage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard stats error: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/doctor/search-patients-by-cedula
        [HttpGet("search-patients-by-cedula")]
        public async Task<ActionResult<object>> SearchPatientsByCedula([FromQuery] string cedula)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cedula))
                {
                    return Ok(new List<object>());
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound(new { error = "Doctor no encontrado" });
                }

                try
                {
                    // Buscar pacientes por cédula
                    var patients = await _context.Patients
                        .Where(p => p.DocumentId != null && p.DocumentId.Contains(cedula))
                        .Select(p => new
                        {
                            id = p.Id,
                            fullName = p.FullName ?? "Sin nombre",
                            documentId = p.DocumentId ?? "Sin documento",
                            phoneNumber = p.PhoneNumber ?? "Sin teléfono"
                        })
                        .Take(10)
                        .ToListAsync();

                    return Ok(patients);
                }
                catch (Exception queryEx)
                {
                    Console.WriteLine($"Query error in search patients: {queryEx.Message}");
                    return Ok(new List<object>());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search patients error: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET: api/doctor/next-appointment
        [HttpGet("next-appointment")]
        public async Task<ActionResult<object>> GetNextAppointment()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound(new { error = "Doctor no encontrado" });
                }

                var now = DateTime.Now;
                
                try
                {
                    var nextAppointment = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && 
                                   a.AppointmentDateTime > now &&
                                   a.IsCancelled == false)
                        .Include(a => a.Patient)
                        .OrderBy(a => a.AppointmentDateTime)
                        .FirstOrDefaultAsync();

                    if (nextAppointment != null)
                    {
                        var statusClass = "bg-primary";
                        var statusText = "Programada";

                        if (nextAppointment.IsCompleted)
                        {
                            statusClass = "bg-success";
                            statusText = "Completada";
                        }
                        else if (nextAppointment.IsConfirmed)
                        {
                            statusClass = "bg-info";
                            statusText = "Confirmada";
                        }

                        return Ok(new
                        {
                            appointment = new
                            {
                                id = nextAppointment.Id,
                                date = nextAppointment.AppointmentDateTime.ToString("dd/MM/yyyy"),
                                time = nextAppointment.AppointmentDateTime.ToString("HH:mm"),
                                patientName = nextAppointment.Patient?.FullName ?? "Sin nombre",
                                patientDocument = nextAppointment.Patient?.DocumentId ?? "Sin documento",
                                status = statusText,
                                statusClass = statusClass,
                                notes = nextAppointment.Notes ?? ""
                            }
                        });
                    }

                    return Ok(new { appointment = (object)null, message = "No hay próximas citas programadas" });
                }
                catch (Exception queryEx)
                {
                    Console.WriteLine($"Query error in next appointment: {queryEx.Message}");
                    return Ok(new { appointment = (object)null, message = "Error al cargar próxima cita" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Next appointment error: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/doctor/upcoming-appointments
        [HttpGet("upcoming-appointments")]
        public async Task<ActionResult<object>> GetUpcomingAppointments()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return NotFound(new { error = "Doctor no encontrado" });
                }

                var now = DateTime.Now;
                
                try
                {
                    var appointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && 
                                   a.AppointmentDateTime > now &&
                                   a.IsCancelled == false)
                        .Include(a => a.Patient)
                        .OrderBy(a => a.AppointmentDateTime)
                        .Take(20)
                        .ToListAsync();

                    var appointmentsList = appointments.Select(a =>
                    {
                        var statusClass = "bg-primary";
                        var statusText = "Programada";

                        if (a.IsCompleted)
                        {
                            statusClass = "bg-success";
                            statusText = "Completada";
                        }
                        else if (a.IsConfirmed)
                        {
                            statusClass = "bg-info";
                            statusText = "Confirmada";
                        }

                        return new
                        {
                            id = a.Id,
                            date = a.AppointmentDateTime.ToString("dd/MM/yyyy"),
                            time = a.AppointmentDateTime.ToString("HH:mm"),
                            patientName = a.Patient?.FullName ?? "Sin nombre",
                            patientDocument = a.Patient?.DocumentId ?? "Sin documento",
                            status = statusText,
                            statusClass = statusClass,
                            notes = a.Notes ?? ""
                        };
                    }).ToList();

                    return Ok(new { appointments = appointmentsList });
                }
                catch (Exception queryEx)
                {
                    Console.WriteLine($"Query error in upcoming appointments: {queryEx.Message}");
                    return Ok(new { appointments = new List<object>() });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upcoming appointments error: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message, stackTrace = ex.StackTrace });
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

        // GET: api/doctor/test-auth
        [HttpGet("test-auth")]
        public async Task<ActionResult<object>> TestAuth()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Ok(new { authenticated = false, message = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                return Ok(new 
                { 
                    authenticated = true,
                    userId = user.Id,
                    userName = user.UserName,
                    doctorFound = doctor != null,
                    doctorId = doctor?.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error en test de autenticación", details = ex.Message });
            }
        }

        // GET: api/doctor/verify-user-doctor
        [HttpGet("verify-user-doctor")]
        public async Task<ActionResult<object>> VerifyUserDoctor()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Ok(new { error = "Usuario no encontrado" });
                }

                // Check user details
                var userRoles = await _userManager.GetRolesAsync(user);
                
                // Check all doctors in the database
                var allDoctors = await _context.Doctors.ToListAsync();
                
                // Try to find doctor with different approaches
                var doctorByUserId = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                return Ok(new
                {
                    currentUser = new
                    {
                        id = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        roles = userRoles
                    },
                    allDoctorsInDb = allDoctors.Select(d => new
                    {
                        id = d.Id,
                        fullName = d.FullName,
                        userId = d.UserId,
                        licenseNumber = d.LicenseNumber
                    }).ToList(),
                    doctorLookupResults = new
                    {
                        byUserId = doctorByUserId != null ? new
                        {
                            id = doctorByUserId.Id,
                            fullName = doctorByUserId.FullName,
                            userId = doctorByUserId.UserId
                        } : null,

                    },
                    userIdType = user.Id.GetType().Name,
                    issue = doctorByUserId == null ? "Doctor record not found for this user" : "Doctor found successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error en verificación", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/doctor/debug-data
        [HttpGet("debug-data")]
        public async Task<ActionResult<object>> DebugData()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Ok(new { error = "Usuario no encontrado" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    return Ok(new { error = "Doctor no encontrado", userId = user.Id });
                }

                var now = DateTime.Now;

                // Obtener TODAS las citas del doctor (consulta simple primero)
                var allAppointmentsSimple = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => new
                    {
                        id = a.Id,
                        appointmentDateTime = a.AppointmentDateTime,
                        isConfirmed = a.IsConfirmed,
                        isCompleted = a.IsCompleted,
                        isCancelled = a.IsCancelled,
                        patientId = a.PatientId,
                        notes = a.Notes ?? "Sin notas",
                        isFutureAppointment = a.AppointmentDateTime > now,
                        daysDifference = (a.AppointmentDateTime - now).TotalDays
                    })
                    .OrderBy(a => a.appointmentDateTime)
                    .ToListAsync();

                // Ahora obtener los nombres de pacientes por separado
                var appointmentsWithPatients = new List<object>();
                foreach (var apt in allAppointmentsSimple)
                {
                    var patient = await _context.Patients.FindAsync(apt.patientId);
                    appointmentsWithPatients.Add(new
                    {
                        id = apt.id,
                        appointmentDateTime = apt.appointmentDateTime,
                        isConfirmed = apt.isConfirmed,
                        isCompleted = apt.isCompleted,
                        isCancelled = apt.isCancelled,
                        patientName = patient?.FullName ?? "Sin paciente",
                        patientId = apt.patientId,
                        notes = apt.notes,
                        isFutureAppointment = apt.isFutureAppointment,
                        daysDifference = apt.daysDifference
                    });
                }

                // Filtrar citas futuras no canceladas
                var futureAppointments = appointmentsWithPatients
                    .Where(a => (bool)a.GetType().GetProperty("isFutureAppointment")?.GetValue(a)! && 
                               !(bool)a.GetType().GetProperty("isCancelled")?.GetValue(a)!)
                    .ToList();

                // Filtrar citas confirmadas
                var confirmedAppointments = appointmentsWithPatients
                    .Where(a => (bool)a.GetType().GetProperty("isConfirmed")?.GetValue(a)! && 
                               !(bool)a.GetType().GetProperty("isCompleted")?.GetValue(a)! && 
                               !(bool)a.GetType().GetProperty("isCancelled")?.GetValue(a)!)
                    .ToList();

                return Ok(new
                {
                    doctorId = doctor.Id,
                    doctorName = doctor.FullName,
                    currentTime = now,
                    totalAppointments = appointmentsWithPatients.Count,
                    futureAppointmentsCount = futureAppointments.Count,
                    confirmedAppointmentsCount = confirmedAppointments.Count,
                    allAppointments = appointmentsWithPatients,
                    futureAppointments = futureAppointments,
                    confirmedAppointments = confirmedAppointments,
                    nextAppointment = futureAppointments.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error en debug", details = ex.Message, stackTrace = ex.StackTrace });
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




