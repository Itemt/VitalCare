using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;

namespace CitasEPS.Pages.Admin
{
    public class DiagnosticDataModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiagnosticDataModel> _logger;

        public DiagnosticDataModel(ApplicationDbContext context, ILogger<DiagnosticDataModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Specialty> Specialties { get; set; } = new();
        public List<Doctor> Doctors { get; set; } = new();

        public async Task OnGetAsync()
        {
            Specialties = await _context.Specialties
                .OrderBy(s => s.Name)
                .ToListAsync();

            Doctors = await _context.Doctors
                .Include(d => d.Specialty)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostMarkAllDoctorsAvailableAsync()
        {
            try
            {
                var doctors = await _context.Doctors.ToListAsync();
                foreach (var doctor in doctors)
                {
                    doctor.IsAvailable = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Marcados {doctors.Count} doctores como disponibles");
                
                TempData["SuccessMessage"] = $"Se marcaron {doctors.Count} doctores como disponibles.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar doctores como disponibles");
                TempData["ErrorMessage"] = "Error al actualizar los doctores.";
            }

            return RedirectToPage();
        }
    }
} 