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
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient,Admin,Doctor")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Appointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Intento de ver detalles de cita sin ID.");
                return NotFound();
            }

            // Obtener la cita incluyendo Paciente, Médico, Especialidad y Prescripciones
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialty)
                .Include(a => a.Patient)
                .Include(a => a.Prescriptions)
                    .ThenInclude(p => p.Medication)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                 _logger.LogWarning("No se encontró cita con ID: {AppointmentId} para ver detalles.", id);
                return NotFound();
            }

            Appointment = appointment;
            _logger.LogInformation("Mostrando detalles para cita ID: {AppointmentId}", id);
            return Page();
        }
    }
} 