using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization; // Add for authorization

namespace CitasEPS.Pages.Appointments
{
    [Authorize] // Require login to view appointments
    public class IndexModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;

        public IndexModel(CitasEPS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Appointment> Appointment { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // Include related Patient and Doctor data when fetching appointments
            Appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .OrderBy(a => a.AppointmentDateTime) // Order by date
                .ToListAsync();

            // Future enhancement: Filter appointments based on logged-in user (if not admin)
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Requires System.Security.Claims
            // if (!User.HasClaim("IsAdmin", "true")) {
            //    // Assuming Patient has a UserId link or similar logic
            //    Appointment = Appointment.Where(a => a.Patient.UserId == int.Parse(userId)).ToList();
            // }
        }
    }
} 