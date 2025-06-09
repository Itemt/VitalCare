using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(int userId, string message, NotificationType notificationType, int? appointmentId = null)
        {
            if (string.IsNullOrEmpty(message)) // UserId cannot be null or empty as it's an int
            {
                // Consider logging this or throwing a specific exception
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                NotificationType = notificationType,
                AppointmentId = appointmentId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            // Log successful notification creation
            Console.WriteLine($"[NotificationService] Notification created successfully - UserId: {userId}, Type: {notificationType}, Message: {message}");
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool includeRead = false)
        {
            var query = _context.Notifications
                                .Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query.OrderByDescending(n => n.CreatedAt)
                              .ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllUserNotificationsAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications
                                            .Where(n => n.UserId == userId && !n.IsRead)
                                            .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _context.Notifications
                                 .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
} 




