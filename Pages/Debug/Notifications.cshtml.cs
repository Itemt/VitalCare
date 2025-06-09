using CitasEPS.Data;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Models.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Debug
{
    [Authorize(Roles = "Admin")] // Solo admins pueden ver esto
    public class NotificationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NotificationsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Notification> Notifications { get; set; } = new List<Notification>();
        public IList<Doctor> Doctors { get; set; } = new List<Doctor>();

        public async Task OnGetAsync()
        {
            // Obtener las últimas 20 notificaciones
            Notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Obtener todos los doctores con sus usuarios
            Doctors = await _context.Doctors
                .Include(d => d.User)
                .ToListAsync();
        }
    }
} 