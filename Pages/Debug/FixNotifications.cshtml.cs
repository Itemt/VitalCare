using CitasEPS.Data;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Services;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CitasEPS.Pages.Debug
{
    [Authorize(Roles = "Admin")]
    public class FixNotificationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAppointmentEmailService _appointmentEmailService;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<FixNotificationsModel> _logger;

        public FixNotificationsModel(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAppointmentEmailService appointmentEmailService,
            IEmailSender emailSender,
            UserManager<User> userManager,
            ILogger<FixNotificationsModel> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _appointmentEmailService = appointmentEmailService;
            _emailSender = emailSender;
            _userManager = userManager;
            _logger = logger;
        }

        public string? Results { get; set; }
        public SystemStatusInfo? SystemStatus { get; set; }

        public async Task OnGetAsync()
        {
            await LoadSystemStatus();
        }

        public async Task<IActionResult> OnPostTestBasicAsync()
        {
            var log = new StringBuilder();
            log.AppendLine("🧪 INICIANDO TEST BÁSICO DE NOTIFICACIONES");
            log.AppendLine("===============================================");

            try
            {
                // Test 1: Buscar un doctor con usuario
                var doctorWithUser = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId != null);

                if (doctorWithUser?.User == null)
                {
                    log.AppendLine("❌ ERROR CRÍTICO: No hay doctores con usuarios vinculados");
                    Results = log.ToString();
                    await LoadSystemStatus();
                    return Page();
                }

                log.AppendLine($"✅ Doctor encontrado: {doctorWithUser.FullName}");
                log.AppendLine($"   User ID: {doctorWithUser.User.Id}");
                log.AppendLine($"   Email: {doctorWithUser.User.Email}");

                // Test 2: Crear notificación directa
                log.AppendLine("\n📢 Creando notificación de prueba...");
                var testMessage = $"🧪 TEST DIRECTO: Notificación de prueba creada el {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                await _notificationService.CreateNotificationAsync(
                    doctorWithUser.User.Id,
                    testMessage,
                    NotificationType.RescheduleRequestedByPatient,
                    null
                );

                log.AppendLine("✅ Notificación creada exitosamente");

                // Test 3: Verificar en base de datos
                var savedNotification = await _context.Notifications
                    .Where(n => n.UserId == doctorWithUser.User.Id && n.Message.Contains("TEST DIRECTO"))
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (savedNotification != null)
                {
                    log.AppendLine($"✅ Notificación verificada en BD: ID {savedNotification.Id}");
                    log.AppendLine($"   Tipo: {savedNotification.NotificationType}");
                    log.AppendLine($"   Leída: {savedNotification.IsRead}");
                }
                else
                {
                    log.AppendLine("❌ ERROR: Notificación no encontrada en BD");
                }

                log.AppendLine("\n🎯 TEST BÁSICO COMPLETADO");
            }
            catch (Exception ex)
            {
                log.AppendLine($"💥 ERROR: {ex.Message}");
                log.AppendLine($"Stack: {ex.StackTrace}");
                _logger.LogError(ex, "Error en test básico de notificaciones");
            }

            Results = log.ToString();
            await LoadSystemStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostTestRescheduleAsync(int appointmentId)
        {
            var log = new StringBuilder();
            log.AppendLine($"🩺 SIMULANDO REAGENDAMIENTO - Cita ID: {appointmentId}");
            log.AppendLine("======================================================");

            try
            {
                // Cargar cita completa
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor.User)
                    .Include(a => a.Patient.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    log.AppendLine("❌ ERROR: Cita no encontrada");
                    Results = log.ToString();
                    await LoadSystemStatus();
                    return Page();
                }

                log.AppendLine($"✅ Cita encontrada: {appointment.Id}");
                log.AppendLine($"   Paciente: {appointment.Patient?.FullName ?? "N/A"}");
                log.AppendLine($"   Doctor: {appointment.Doctor?.FullName ?? "N/A"}");

                // Verificar doctor user
                if (appointment.Doctor?.User == null)
                {
                    log.AppendLine("⚠️ Doctor.User es null, cargando manualmente...");
                    if (appointment.Doctor?.UserId != null)
                    {
                        appointment.Doctor.User = await _context.Users.FindAsync(appointment.Doctor.UserId.Value);
                        log.AppendLine($"   User cargado: {appointment.Doctor.User != null}");
                    }
                }

                if (appointment.Doctor?.User != null)
                {
                    // Simular exactamente lo que hace el código real
                    var patientName = appointment.Patient?.FullName ?? "Paciente Test";
                    var appointmentFormatted = ColombiaTimeZoneService.FormatInColombia(appointment.AppointmentDateTime);
                    var doctorMessage = $"El paciente {patientName} ha solicitado reagendar la cita del {appointmentFormatted}.";

                    log.AppendLine($"\n📢 Creando notificación de reagendamiento...");
                    log.AppendLine($"   Para Doctor User ID: {appointment.Doctor.User.Id}");
                    log.AppendLine($"   Mensaje: {doctorMessage}");

                    await _notificationService.CreateNotificationAsync(
                        appointment.Doctor.User.Id,
                        doctorMessage,
                        NotificationType.RescheduleRequestedByPatient,
                        appointment.Id
                    );

                    log.AppendLine("✅ Notificación de reagendamiento creada exitosamente");

                    // Test de email también
                    try
                    {
                        log.AppendLine("\n📧 Enviando email de prueba...");
                        await _appointmentEmailService.SendRescheduleRequestedEmailAsync(
                            appointment,
                            appointment.Patient?.User ?? new User { Email = "test@test.com" },
                            appointment.Doctor.User
                        );
                        log.AppendLine("✅ Email enviado exitosamente");
                    }
                    catch (Exception emailEx)
                    {
                        log.AppendLine($"❌ Error enviando email: {emailEx.Message}");
                    }
                }
                else
                {
                    log.AppendLine("❌ ERROR: No se pudo cargar Doctor.User");
                }

                log.AppendLine("\n🎯 SIMULACIÓN DE REAGENDAMIENTO COMPLETADA");
            }
            catch (Exception ex)
            {
                log.AppendLine($"💥 ERROR: {ex.Message}");
                _logger.LogError(ex, "Error simulando reagendamiento para cita {AppointmentId}", appointmentId);
            }

            Results = log.ToString();
            await LoadSystemStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostTestEmailAsync(string email)
        {
            var log = new StringBuilder();
            log.AppendLine($"📧 PROBANDO EMAIL A: {email}");
            log.AppendLine("================================");

            try
            {
                var subject = "🧪 Test Email - VitalCare";
                var message = $"Este es un email de prueba enviado desde VitalCare el {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                await _emailSender.SendEmailAsync(email, subject, message);
                log.AppendLine("✅ Email enviado exitosamente");
            }
            catch (Exception ex)
            {
                log.AppendLine($"❌ ERROR enviando email: {ex.Message}");
                _logger.LogError(ex, "Error enviando email de prueba a {Email}", email);
            }

            Results = log.ToString();
            await LoadSystemStatus();
            return Page();
        }

        private async Task LoadSystemStatus()
        {
            try
            {
                var totalNotifications = await _context.Notifications.CountAsync();
                var unreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);
                var activeAppointments = await _context.Appointments.CountAsync(a => !a.IsCancelled && !a.IsCompleted);
                var doctorsWithUser = await _context.Doctors.CountAsync(d => d.UserId != null);
                var doctorsWithoutUser = await _context.Doctors
                    .Where(d => d.UserId == null)
                    .ToListAsync();

                SystemStatus = new SystemStatusInfo
                {
                    TotalNotifications = totalNotifications,
                    UnreadNotifications = unreadNotifications,
                    ActiveAppointments = activeAppointments,
                    DoctorsWithUser = doctorsWithUser,
                    DoctorsWithoutUser = doctorsWithoutUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando estado del sistema");
                SystemStatus = null;
            }
        }

        public class SystemStatusInfo
        {
            public int TotalNotifications { get; set; }
            public int UnreadNotifications { get; set; }
            public int ActiveAppointments { get; set; }
            public int DoctorsWithUser { get; set; }
            public List<Doctor> DoctorsWithoutUser { get; set; } = new();
        }
    }
} 