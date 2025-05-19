using CitasEPS.Models;
using CitasEPS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for IEnumerable
using System.Linq; // Required for Select

namespace CitasEPS.Controllers
{
    [Authorize] // All actions require authentication
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<User> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User); // User is ClaimsPrincipal from ControllerBase
        }

        // GET: api/notifications/unread
        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationViewModel>>> GetUnreadNotifications()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(currentUser.Id, includeRead: false);
            
            var viewModels = notifications.Select(n => {
                string? navigationPath = null;
                if (n.AppointmentId.HasValue)
                {
                    // For now, all roles go to the same details page.
                    navigationPath = $"/Appointments/Details/{n.AppointmentId.Value}";
                }
                return new NotificationViewModel(n, navigationPath);
            }).ToList();
            
            return Ok(viewModels);
        }

        // GET: api/notifications/unreadcount
        [HttpGet("unreadcount")]
        public async Task<ActionResult<int>> GetUnreadNotificationCount()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var count = await _notificationService.GetUnreadNotificationCountAsync(currentUser.Id);
            return Ok(count);
        }

        // GET: api/notifications/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<NotificationViewModel>>> GetAllNotifications()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) 
            {
                return Unauthorized();
            }

            var notificationsFromService = await _notificationService.GetUserNotificationsAsync(currentUser.Id, includeRead: true);

            var notificationViewModels = notificationsFromService.Select(notification => { 
                string? navPath = null;
                if (notification.AppointmentId.HasValue)
                {
                    navPath = $"/Appointments/Details/{notification.AppointmentId.Value}";
                }
                return new NotificationViewModel(notification, navPath);
            }).ToList();
            
            return Ok(notificationViewModels);
        }

        // POST: api/notifications/{id}/markasread
        [HttpPost("{id}/markasread")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            await _notificationService.MarkNotificationAsReadAsync(id);
            return NoContent(); 
        }

        // POST: api/notifications/markallasread
        [HttpPost("markallasread")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            await _notificationService.MarkAllUserNotificationsAsReadAsync(currentUser.Id);
            return NoContent();
        }
    }
} 