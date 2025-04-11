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
using Microsoft.AspNetCore.Identity; // Required for UserManager
using System.Security.Claims; // Required for ClaimsPrincipal extension methods if needed

namespace CitasEPS.Pages.Appointments
{
    [Authorize(Roles = "Patient")] // Requerir rol Paciente
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<Appointment> Appointment { get;set; } = default!;
        public string PatientName { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // No ha iniciado sesión
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
            if (patient == null)
            {
                 _logger.LogWarning($"No se encontró registro de Paciente para el usuario {user.Email}."); // Changed Log
                 // Opcionalmente establecer un mensaje de error
                 TempData["ErrorMessage"] = "No se encontró su registro de paciente.";
                 Appointment = new List<Appointment>(); // Asignar lista vacía
                 return Page(); // Mostrar la página pero sin datos
            }

            PatientName = patient.FullName; // Almacenar nombre del paciente si se necesita en la vista

            // Filtrar citas para el paciente conectado
            Appointment = await _context.Appointments
                .Where(a => a.PatientId == patient.Id)
                .Include(a => a.Doctor) // Incluir detalles del Médico
                    .ThenInclude(d => d.Specialty) // Incluir Especialidad del Médico
                // .Include(a => a.Patient) // Ya no es necesario porque filtramos por patient ID
                .OrderBy(a => a.AppointmentDateTime) // Ordenar por fecha
                .ToListAsync();

             _logger.LogInformation($"Se cargaron {Appointment.Count} citas para el paciente {patient.FullName} (ID: {patient.Id})."); // Changed Log

            return Page();
        }
    }
} 