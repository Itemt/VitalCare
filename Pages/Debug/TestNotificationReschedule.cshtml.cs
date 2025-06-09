using CitasEPS.Data;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CitasEPS.Pages.Debug
{
    [Authorize(Roles = "Admin")]
    public class TestNotificationRescheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TestNotificationRescheduleModel> _logger;

        public TestNotificationRescheduleModel(ApplicationDbContext context, INotificationService notificationService, ILogger<TestNotificationRescheduleModel> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public IList<Appointment> AvailableAppointments { get; set; } = new List<Appointment>();
        public string? TestResults { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAppointments();
        }

        public async Task<IActionResult> OnPostAsync(int appointmentId)
        {
            var results = new StringBuilder();
            results.AppendLine($"🔍 DIAGNÓSTICO DE REAGENDAMIENTO - Cita ID: {appointmentId}");
            results.AppendLine($"Fecha/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            results.AppendLine("═══════════════════════════════════════════");

            try
            {
                // Paso 1: Cargar la cita
                results.AppendLine("📋 PASO 1: Cargando cita...");
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor.User)
                    .Include(a => a.Doctor.Specialty)
                    .Include(a => a.Patient.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    results.AppendLine("❌ ERROR: Cita no encontrada");
                    TestResults = results.ToString();
                    await LoadAppointments();
                    return Page();
                }

                results.AppendLine($"✅ Cita encontrada: ID {appointment.Id}");
                results.AppendLine($"   • Paciente: {appointment.Patient?.FullName ?? "N/A"}");
                results.AppendLine($"   • Doctor: {appointment.Doctor?.FullName ?? "N/A"}");
                results.AppendLine($"   • Fecha: {appointment.AppointmentDateTime:dd/MM/yyyy HH:mm}");

                // Paso 2: Verificar Doctor
                results.AppendLine();
                results.AppendLine("👨‍⚕️ PASO 2: Verificando información del doctor...");
                if (appointment.Doctor == null)
                {
                    results.AppendLine("❌ ERROR: Doctor es null");
                    TestResults = results.ToString();
                    await LoadAppointments();
                    return Page();
                }

                results.AppendLine($"✅ Doctor cargado: {appointment.Doctor.FullName}");
                results.AppendLine($"   • Doctor ID: {appointment.Doctor.Id}");
                results.AppendLine($"   • Doctor UserId: {appointment.Doctor.UserId}");

                // Paso 3: Verificar User del Doctor
                results.AppendLine();
                results.AppendLine("👤 PASO 3: Verificando User del doctor...");
                if (appointment.Doctor.User == null)
                {
                    results.AppendLine("⚠️ Doctor.User es null, intentando cargar manualmente...");
                    
                    if (appointment.Doctor.UserId.HasValue)
                    {
                        appointment.Doctor.User = await _context.Users.FindAsync(appointment.Doctor.UserId.Value);
                        if (appointment.Doctor.User != null)
                        {
                            results.AppendLine($"✅ User cargado manualmente: ID {appointment.Doctor.User.Id}");
                            results.AppendLine($"   • Email: {appointment.Doctor.User.Email}");
                        }
                        else
                        {
                            results.AppendLine($"❌ ERROR: No se pudo cargar User con ID {appointment.Doctor.UserId.Value}");
                            
                            // Verificar si existe en la BD
                            var userExists = await _context.Users.AnyAsync(u => u.Id == appointment.Doctor.UserId.Value);
                            results.AppendLine($"   • ¿Usuario existe en BD? {userExists}");
                        }
                    }
                    else
                    {
                        results.AppendLine("❌ ERROR: Doctor.UserId es null");
                    }
                }
                else
                {
                    results.AppendLine($"✅ Doctor.User ya cargado: ID {appointment.Doctor.User.Id}");
                    results.AppendLine($"   • Email: {appointment.Doctor.User.Email}");
                }

                // Paso 4: Intentar crear notificación
                results.AppendLine();
                results.AppendLine("📢 PASO 4: Creando notificación de prueba...");
                
                if (appointment.Doctor.User != null)
                {
                    var testMessage = $"🧪 PRUEBA: Solicitud de reagendamiento para cita del {ColombiaTimeZoneService.FormatInColombia(appointment.AppointmentDateTime)} - Test realizado a las {DateTime.Now:HH:mm:ss}";
                    
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            appointment.Doctor.User.Id, 
                            testMessage, 
                            NotificationType.RescheduleRequestedByPatient, 
                            appointment.Id
                        );
                        
                        results.AppendLine("✅ Notificación creada exitosamente!");
                        results.AppendLine($"   • Destinatario: User ID {appointment.Doctor.User.Id}");
                        results.AppendLine($"   • Mensaje: {testMessage}");

                        // Verificar que se guardó en la BD
                        var savedNotification = await _context.Notifications
                            .Where(n => n.UserId == appointment.Doctor.User.Id && n.Message.Contains("PRUEBA"))
                            .OrderByDescending(n => n.CreatedAt)
                            .FirstOrDefaultAsync();

                        if (savedNotification != null)
                        {
                            results.AppendLine($"✅ Notificación verificada en BD: ID {savedNotification.Id}");
                            results.AppendLine($"   • Creada en: {savedNotification.CreatedAt:dd/MM/yyyy HH:mm:ss}");
                        }
                        else
                        {
                            results.AppendLine("⚠️ No se encontró la notificación en la BD");
                        }
                    }
                    catch (Exception ex)
                    {
                        results.AppendLine($"❌ ERROR al crear notificación: {ex.Message}");
                        results.AppendLine($"   • Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    results.AppendLine("❌ No se puede crear notificación: Doctor.User es null");
                }

                // Paso 5: Mostrar notificaciones recientes del doctor
                results.AppendLine();
                results.AppendLine("📋 PASO 5: Notificaciones recientes del doctor...");
                
                if (appointment.Doctor.User != null)
                {
                    var recentNotifications = await _context.Notifications
                        .Where(n => n.UserId == appointment.Doctor.User.Id)
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(5)
                        .ToListAsync();

                    if (recentNotifications.Any())
                    {
                        results.AppendLine($"✅ Encontradas {recentNotifications.Count} notificaciones recientes:");
                        foreach (var notif in recentNotifications)
                        {
                            results.AppendLine($"   • ID {notif.Id}: {notif.Message.Substring(0, Math.Min(50, notif.Message.Length))}...");
                            results.AppendLine($"     Creada: {notif.CreatedAt:dd/MM/yyyy HH:mm:ss}, Leída: {notif.IsRead}");
                        }
                    }
                    else
                    {
                        results.AppendLine("⚠️ No se encontraron notificaciones para este doctor");
                    }
                }

                results.AppendLine();
                results.AppendLine("🎯 DIAGNÓSTICO COMPLETADO");

            }
            catch (Exception ex)
            {
                results.AppendLine($"💥 ERROR CRÍTICO: {ex.Message}");
                results.AppendLine($"Stack Trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error en diagnóstico de reagendamiento para cita {AppointmentId}", appointmentId);
            }

            TestResults = results.ToString();
            await LoadAppointments();
            return Page();
        }

        private async Task LoadAppointments()
        {
            AvailableAppointments = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Patient)
                .Where(a => !a.IsCancelled && !a.IsCompleted)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Take(10)
                .ToListAsync();
        }
    }
} 