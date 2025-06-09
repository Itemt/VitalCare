using System;
using CitasEPS.Models.Modules.Appointments;

namespace CitasEPS.Services
{
    public class AppointmentStats
    {
        public int ScheduledForLimitInWeek { get; set; }
        public int PendingOrConfirmedForDisplayInWeek { get; set; }
        public int CancelledInWeek { get; set; }
        public int RescheduledInWeek { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int MaxWeeklyAppointments { get; } = 2;
        public int MaxWeeklyCancellations { get; } = 3;
        public int MaxWeeklyReschedules { get; } = 2;
    }

    public interface IAppointmentPolicyService
    {
        bool CanPatientCreateAppointment(int patientId, DateTime newAppointmentDate, out string reason);
        bool CanPatientCancelAppointment(int patientId, out string reason);
        bool CanPatientRequestReschedule(int patientId, out string reason);
        bool CanRescheduleAppointment(int appointmentId, out string reason);
        bool CanRescheduleAppointment(Appointment appointment, bool isDoctorInitiated = false);
        AppointmentStats GetPatientWeeklyAppointmentStats(int patientId, DateTime referenceDate);
    }
} 



