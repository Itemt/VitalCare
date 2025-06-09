using System;

namespace CitasEPS.Services
{
    public class AppointmentStats
    {
        public int ScheduledForLimitInWeek { get; set; }
        public int PendingOrConfirmedForDisplayInWeek { get; set; }
        public int CancelledInWeek { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int MaxWeeklyAppointments { get; } = 2;
        public int MaxWeeklyCancellations { get; } = 3;
    }

    public interface IAppointmentPolicyService
    {
        bool CanPatientCreateAppointment(int patientId, DateTime newAppointmentDate, out string reason);
        bool CanPatientCancelAppointment(int patientId, out string reason);
        AppointmentStats GetPatientWeeklyAppointmentStats(int patientId, DateTime referenceDate);
    }
} 



