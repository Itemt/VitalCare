using CitasEPS.Models;

namespace CitasEPS.Services
{
    public interface IAppointmentEmailService
    {
        Task SendAppointmentCreatedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentConfirmedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentModifiedEmailAsync(Appointment appointment, User patient, User doctor, string modificationType);
        Task SendAppointmentCancelledEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentReminderEmailAsync(Appointment appointment, User patient, User doctor);
    }
} 