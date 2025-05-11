using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PatientHistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientHistoryModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Models.Patient? TargetPatient { get; set; }
        public Models.Doctor? CurrentDoctor { get; set; }
        public IList<Models.Appointment> AppointmentHistory { get; set; } = new List<Models.Appointment>();
        public IList<Models.Prescription> PatientPrescriptions { get; set; } = new List<Models.Prescription>();

        public async Task<IActionResult> OnGetAsync(int? patientId)
        {
            if (patientId == null)
            {
                return NotFound("ID de paciente no proporcionado.");
            }

            // Obtener doctor actual
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            CurrentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (CurrentDoctor == null) return Forbid("Registro de Doctor no encontrado.");

            // Obtener paciente
            TargetPatient = await _context.Patients.FindAsync(patientId);
            if (TargetPatient == null)
            {
                return NotFound("Paciente no encontrado.");
            }

            // Cargar historial de citas ENTRE este doctor y este paciente
            // Incluir Prescripciones y Medicamentos asociados
            AppointmentHistory = await _context.Appointments
                .Where(a => a.DoctorId == CurrentDoctor.Id && a.PatientId == patientId)
                .Include(a => a.Prescriptions ?? new List<Prescription>())
                    .ThenInclude(p => p.Medication)
                .OrderByDescending(a => a.AppointmentDateTime) // Mostrar más recientes primero
                .ToListAsync();

            // Load all prescriptions for the target patient separately
            if (TargetPatient != null) 
            {
                PatientPrescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == TargetPatient.Id)
                    .Include(p => p.Medication) 
                    .Include(p => p.Doctor)   
                    .OrderByDescending(p => p.PrescriptionDate)
                    .ToListAsync();
            }

            // Opcional: Verificar si el doctor realmente ha tenido citas con este paciente
            // Podríamos hacerlo comprobando si AppointmentHistory tiene elementos o consultando si existe al menos una cita
            bool hasHistory = await _context.Appointments.AnyAsync(a => a.DoctorId == CurrentDoctor.Id && a.PatientId == patientId);
            if (!hasHistory)
            {
                // Aunque el paciente exista, si este doctor no lo ha atendido, no debería ver su historial "vacío"
                 // Podríamos mostrar un mensaje o redirigir
                 // return Forbid("No tiene historial de citas con este paciente."); 
                 // O simplemente mostrar la página con 0 citas (como está ahora)
            }

            return Page();
        }
    }
} 