using CitasEPS.Models;
using CitasEPS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(currentUser.Id, includeRead: false);
            return Ok(notifications);
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
        public async Task<ActionResult<IEnumerable<Notification>>> GetAllNotifications()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            // Consider adding pagination here for performance if notification lists can get very long
            var notifications = await _notificationService.GetUserNotificationsAsync(currentUser.Id, includeRead: true);
            return Ok(notifications);
        }

        // POST: api/notifications/{id}/markasread
        [HttpPost("{id}/markasread")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            // Optional: Verify the notification belongs to the current user before marking as read
            // This would require modifying GetNotificationById or adding a method to INotificationService
            // For now, we assume the ID is valid and service handles logic.

            await _notificationService.MarkNotificationAsReadAsync(id);
            return NoContent(); // Successfully processed, no content to return
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