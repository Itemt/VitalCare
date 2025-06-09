using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Services.Modules.Common;

namespace CitasEPS.Services.Modules.Common
{
    public interface IAppointmentEmailService
    {
        Task SendAppointmentCreatedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentConfirmedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentModifiedEmailAsync(Appointment appointment, User patient, User doctor, string modificationType);
        Task SendAppointmentCancelledEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendAppointmentReminderEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendRescheduleRequestedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendRescheduleApprovedEmailAsync(Appointment appointment, User patient, User doctor);
        Task SendRescheduleRejectedEmailAsync(Appointment appointment, User patient, User doctor, DateTime originalDateTime);
    }
} 




