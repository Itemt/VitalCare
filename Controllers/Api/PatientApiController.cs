using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Controllers.Api
{
    [ApiController]
    [Route("api/patient")]
    [Authorize(Roles = "Paciente")]
    public class PatientApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<PatientApiController> _logger;

        public PatientApiController(ApplicationDbContext context, UserManager<User> userManager, ILogger<PatientApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "API funcionando", timestamp = DateTime.Now });
        }

        [HttpGet("next-appointment")]
        public async Task<IActionResult> GetNextAppointment()
        {
            try
            {
                // Obtener el usuario actual
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Ok(new { success = true, appointment = (object)null, message = "Usuario no encontrado" });
                }

                // Buscar el paciente
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return Ok(new { success = true, appointment = (object)null, message = "Paciente no encontrado" });
                }

                // Obtener la fecha actual en UTC para comparación
                var currentDateTime = DateTime.UtcNow;

                // Buscar citas futuras - usar ToListAsync y filtrar en memoria para evitar problemas de PostgreSQL
                var allAppointments = await _context.Appointments
                    .Where(a => a.PatientId == patient.Id)
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();

                // Filtrar en memoria las citas futuras
                var futureAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime > currentDateTime)
                    .ToList();

                if (!futureAppointments.Any())
                {
                    return Ok(new { success = true, appointment = (object)null, message = "No hay citas futuras" });
                }

                var appointment = futureAppointments.First();

                // Respuesta simple
                return Ok(new { 
                    success = true, 
                    appointment = new {
                        id = appointment.Id,
                        dateTime = appointment.AppointmentDateTime,
                        doctorName = "Dr. Por Asignar",
                        appointmentType = "Consulta General",
                        location = "Consultorio",
                        status = "Programada"
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message, stack = ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace.Length)) });
            }
        }

        [HttpGet("prescriptions-count")]
        public async Task<IActionResult> GetPrescriptionsCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Primero obtener el Patient asociado al User
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return Ok(new { success = true, count = 0 });
                }

                var prescriptionsCount = await _context.Prescriptions
                    .Include(p => p.Appointment)
                    .Where(p => p.Appointment.PatientId == patient.Id) // <-- CORREGIDO: Usar patient.Id
                    .CountAsync();

                return Ok(new { success = true, count = prescriptionsCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Error interno del servidor" });
            }
        }

        [HttpGet("health-status")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Primero obtener el Patient asociado al User
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return Ok(new { success = true, healthData = (object)null });
                }

                // Get the latest health data for this patient
                var latestHealthData = await _context.HealthData
                    .Where(h => h.PatientId == patient.Id) // <-- CORREGIDO: Usar patient.Id
                    .OrderByDescending(h => h.RecordedDate)
                    .FirstOrDefaultAsync();

                if (latestHealthData == null)
                {
                    return Ok(new { success = true, healthData = (object)null });
                }

                var healthData = new
                {
                    weight = latestHealthData.Weight.ToString(),
                    height = latestHealthData.Height.ToString(),
                    systolic = latestHealthData.SystolicPressure.ToString(),
                    diastolic = latestHealthData.DiastolicPressure.ToString(),
                    heartRate = latestHealthData.HeartRate?.ToString(),
                    activityLevel = latestHealthData.ActivityLevel,
                    bmi = latestHealthData.BMI.ToString(),
                    bloodPressure = latestHealthData.BloodPressure,
                    status = latestHealthData.HealthStatus,
                    lastUpdate = latestHealthData.RecordedDate
                };

                return Ok(new { success = true, healthData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Error interno del servidor" });
            }
        }

        [HttpPost("save-health-data")]
        public async Task<IActionResult> SaveHealthData([FromBody] HealthDataRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Primero obtener el Patient asociado al User
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return BadRequest(new { success = false, error = "No se encontró el registro de paciente" });
                }

                // Validate required fields - SIMPLIFICADO: Solo peso y altura son obligatorios
                if (string.IsNullOrEmpty(request.Weight) || string.IsNullOrEmpty(request.Height))
                {
                    return BadRequest(new { success = false, error = "Peso y altura son obligatorios" });
                }

                // Parse and validate values
                if (!decimal.TryParse(request.Weight, out decimal weight) || weight <= 0)
                {
                    return BadRequest(new { success = false, error = "Peso inválido" });
                }

                if (!decimal.TryParse(request.Height, out decimal height) || height <= 0)
                {
                    return BadRequest(new { success = false, error = "Altura inválida" });
                }

                int? heartRate = null;
                if (!string.IsNullOrEmpty(request.HeartRate))
                {
                    if (int.TryParse(request.HeartRate, out int hr))
                    {
                        heartRate = hr;
                    }
                }

                // Calculate BMI and determine health status (sin presión arterial)
                decimal bmi = weight / (height * height);
                string healthStatus = DetermineHealthStatusSimple(bmi);

                // Create new health data record
                var healthData = new HealthData
                {
                    PatientId = patient.Id, // <-- CORREGIDO: Usar patient.Id
                    Weight = weight,
                    Height = height,
                    SystolicPressure = 0, // Valor por defecto
                    DiastolicPressure = 0, // Valor por defecto
                    HeartRate = heartRate,
                    ActivityLevel = request.ActivityLevel,
                    RecordedDate = DateTime.Now,
                    HealthStatus = healthStatus
                };

                _context.HealthData.Add(healthData);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Datos de salud guardados exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Error interno del servidor" });
            }
        }

        [HttpGet("health-history")]
        public async Task<IActionResult> GetHealthHistory()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Primero obtener el Patient asociado al User
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return Ok(new { success = true, history = new object[0] });
                }

                // Get the last 10 health records for this patient
                var healthHistory = await _context.HealthData
                    .Where(h => h.PatientId == patient.Id) // <-- CORREGIDO: Usar patient.Id
                    .OrderByDescending(h => h.RecordedDate)
                    .Take(10)
                    .Select(h => new
                    {
                        weight = h.Weight,
                        height = h.Height,
                        heartRate = h.HeartRate,
                        activityLevel = h.ActivityLevel,
                        timestamp = h.RecordedDate,
                        bmi = h.BMI,
                        healthStatus = h.HealthStatus
                    })
                    .ToListAsync();

                return Ok(new { success = true, history = healthHistory });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Error interno del servidor" });
            }
        }

        [HttpGet("simple-appointment")]
        public async Task<IActionResult> GetSimpleAppointment()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Ok(new { success = false, message = "No user" });
                }

                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null)
                {
                    return Ok(new { success = false, message = "No patient found" });
                }

                // Contar todas las citas
                var appointmentCount = await _context.Appointments.CountAsync(a => a.PatientId == patient.Id);
                
                // Obtener la primera cita sin filtros de fecha
                var firstAppointment = await _context.Appointments
                    .Where(a => a.PatientId == patient.Id)
                    .OrderBy(a => a.AppointmentDateTime)
                    .FirstOrDefaultAsync();

                return Ok(new { 
                    success = true, 
                    patientId = patient.Id,
                    totalAppointments = appointmentCount,
                    firstAppointment = firstAppointment != null ? new {
                        id = firstAppointment.Id,
                        dateTime = firstAppointment.AppointmentDateTime.ToString("yyyy-MM-dd HH:mm"),
                        confirmed = firstAppointment.IsConfirmed,
                        cancelled = firstAppointment.IsCancelled
                    } : null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
        }

        private string GetStatusText(Appointment appointment)
        {
            if (appointment.IsCancelled)
                return "Cancelada";
            if (appointment.IsCompleted)
                return "Completada";
            if (appointment.IsConfirmed)
                return "Confirmada";
            if (appointment.WasNoShow)
                return "No se presentó";
            
            return "Programada";
        }

        private string DetermineHealthStatus(decimal bmi, int systolic, int diastolic)
        {
            // Determine health status based on BMI and blood pressure
            bool normalBMI = bmi >= 18.5m && bmi < 25m;
            bool normalBP = systolic < 120 && diastolic < 80;

            if (normalBMI && normalBP)
                return "Excelente";
            
            if (normalBMI && (systolic < 130 || diastolic < 80))
                return "Bueno";
            
            if ((bmi >= 25m && bmi < 30m) || (systolic >= 130 && systolic < 140) || (diastolic >= 80 && diastolic < 90))
                return "Aceptable";
            
            if (bmi >= 30m || systolic >= 140 || diastolic >= 90)
                return "Requiere Atención";
            
            if (bmi < 18.5m)
                return "Bajo Peso";
            
            return "Sin Evaluar";
        }

        private string DetermineHealthStatusSimple(decimal bmi)
        {
            // Determine health status based on BMI
            if (bmi < 18.5m)
                return "Bajo Peso";
            if (bmi >= 18.5m && bmi < 25m)
                return "Normal";
            if (bmi >= 25m && bmi < 30m)
                return "Sobrepeso";
            if (bmi >= 30m)
                return "Obesidad";
            
            return "Sin Evaluar";
        }
    }

    public class HealthDataRequest
    {
        public string? Weight { get; set; }
        public string? Height { get; set; }
        public string? HeartRate { get; set; }
        public string? ActivityLevel { get; set; }
    }
} 