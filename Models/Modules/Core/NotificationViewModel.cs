using System;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core; // Assuming Notification and NotificationType are here

namespace CitasEPS.Models.Modules.Core
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? AppointmentId { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public NotificationType NotificationType { get; set; }
        public string? NavigationPath { get; set; }

        // Constructor to map from Notification entity
        public NotificationViewModel(Notification notification, string? navigationPath = null)
        {
            Id = notification.Id;
            UserId = notification.UserId;
            AppointmentId = notification.AppointmentId;
            Message = notification.Message;
            IsRead = notification.IsRead;
            CreatedAt = notification.CreatedAt;
            NotificationType = notification.NotificationType;
            NavigationPath = navigationPath;
        }
        // Parameterless constructor for model binding if needed
        public NotificationViewModel() { }
    }
} 




