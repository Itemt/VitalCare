using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CitasEPS.Services
{
    public class AppointmentPolicyService : IAppointmentPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private const int MaxWeeklyAppointments = 2;
        private const int MaxWeeklyCancellations = 3;

        public AppointmentPolicyService(ApplicationDbContext context, IDateTimeService dateTimeService)
        { 
            _context = context;
            _dateTimeService = dateTimeService;
        }

        public bool CanPatientCreateAppointment(int patientId, DateTime newAppointmentDate, out string reason)
        {
            reason = string.Empty;
            var (startOfWeek, endOfWeek) = _dateTimeService.GetWeekRange(newAppointmentDate);

            var scheduledCount = _context.Appointments
                .Count(a => a.PatientId == patientId && 
                             !a.IsCancelled && 
                             a.AppointmentDateTime >= startOfWeek && 
                             a.AppointmentDateTime <= endOfWeek);

            if (scheduledCount >= MaxWeeklyAppointments)
            {
                reason = $"Ya tiene {MaxWeeklyAppointments} citas programadas (o que ya ocurrieron sin ser canceladas) para la semana del {startOfWeek:dd/MM/yyyy} al {endOfWeek:dd/MM/yyyy}.";
                return false;
            }
            return true;
        }

        public bool CanPatientCancelAppointment(int patientId, out string reason)
        {
            reason = string.Empty;
            var now = _dateTimeService.GetNow(); // Fecha actual, probablemente local
            var (localStartOfWeek, localEndOfWeek) = _dateTimeService.GetWeekRange(now);

            // Convertir los límites de la semana a UTC para la consulta a la BD
            var utcStartOfWeek = localStartOfWeek.ToUniversalTime();
            var utcEndOfWeek = localEndOfWeek.ToUniversalTime();

            var cancelledCount = _context.Appointments
                .Count(a => a.PatientId == patientId && 
                             a.IsCancelled && 
                             a.AppointmentDateTime >= utcStartOfWeek && // Usar versiones UTC
                             a.AppointmentDateTime <= utcEndOfWeek);   // Usar versiones UTC
            
            if (cancelledCount >= MaxWeeklyCancellations)
            {
                // Para el mensaje al usuario, es mejor mostrar las fechas locales
                reason = $"Ya ha cancelado {MaxWeeklyCancellations} citas programadas para la semana actual ({localStartOfWeek:dd/MM/yyyy} - {localEndOfWeek:dd/MM/yyyy}). No puede cancelar más citas esta semana.";
                return false;
            }
            return true;
        }

        public AppointmentStats GetPatientWeeklyAppointmentStats(int patientId, DateTime referenceDate)
        {
            var (localStartOfWeek, localEndOfWeek) = _dateTimeService.GetWeekRange(referenceDate);

            // Convert local week boundaries to UTC for DB query
            var utcStartOfWeek = localStartOfWeek.ToUniversalTime();
            var utcEndOfWeek = localEndOfWeek.ToUniversalTime();

            var appointmentsInWeek = _context.Appointments
                .Where(a => a.PatientId == patientId && 
                             a.AppointmentDateTime >= utcStartOfWeek && 
                             a.AppointmentDateTime <= utcEndOfWeek)
                .ToList(); // Materialize once for multiple counts

            var stats = new AppointmentStats
            {
                ScheduledForLimitInWeek = appointmentsInWeek.Count(a => !a.IsCancelled),
                PendingOrConfirmedForDisplayInWeek = appointmentsInWeek.Count(a => !a.IsCancelled && !a.IsCompleted && !a.WasNoShow),
                CancelledInWeek = appointmentsInWeek.Count(a => a.IsCancelled),
                WeekStartDate = localStartOfWeek, // For display, use the local start/end date of week
                WeekEndDate = localEndOfWeek
            };
            return stats;
        }
    }
} 