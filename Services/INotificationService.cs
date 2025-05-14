using CitasEPS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitasEPS.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string message, NotificationType notificationType, int? appointmentId = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool includeRead = false);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task MarkAllUserNotificationsAsReadAsync(int userId);
        Task<int> GetUnreadNotificationCountAsync(int userId);
    }
} 