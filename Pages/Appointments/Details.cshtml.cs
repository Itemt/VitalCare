using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.Appointments
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly CitasEPS.Data.ApplicationDbContext _context;

        public DetailsModel(CitasEPS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the appointment including related Patient and Doctor
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound(); // Appointment with the given ID not found
            }

            Appointment = appointment;
            return Page();
        }
    }
} 