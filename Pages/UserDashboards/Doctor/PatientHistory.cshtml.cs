using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PatientModel = CitasEPS.Models.Modules.Users.Patient;
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Doctor
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

        public PatientModel? TargetPatient { get; set; }
        public DoctorModel? CurrentDoctor { get; set; }
        public IList<Appointment> AppointmentHistory { get; set; } = new List<Appointment>();
        public IList<Prescription> PatientPrescriptions { get; set; } = new List<Prescription>();

        public async Task<IActionResult> OnGetAsync(int? patientId)
        {
            if (patientId == null)
            {
                return NotFound("ID de paciente no proporcionado.");
            }

            // Obtener Doctor actual
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            CurrentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (CurrentDoctor == null) return Forbid("Registro de Doctor no encontrado.");

            // Obtener paciente
            TargetPatient = await _context.Patients.FindAsync(patientId);
            if (TargetPatient == null)
            {
                return NotFound("Paciente no encontrado.");
            }

            // Cargar historial de citas ENTRE este Doctor y este paciente
            // Incluir Prescripciones y Medicamentos asociados
            AppointmentHistory = await _context.Appointments
                .Where(a => a.DoctorId == CurrentDoctor.Id && a.PatientId == patientId)
                .Include(a => a.Prescriptions)
                    .ThenInclude(p => p.Medication)
                .OrderByDescending(a => a.AppointmentDateTime) // Mostrar más recientes primero
                .ToListAsync();

            // Load all prescriptions for the target Patient separately
            if (TargetPatient != null) 
            {
                PatientPrescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == TargetPatient.Id)
                    .Include(p => p.Medication) 
                    .Include(p => p.Doctor)   
                    .OrderByDescending(p => p.PrescriptionDate)
                    .ToListAsync();
            }

            // Opcional: Verificar si el Doctor realmente ha tenido citas con este paciente
            // Podríamos hacerlo comprobando si AppointmentHistory tiene elementos o consultando si existe al menos una cita
            bool hasHistory = await _context.Appointments.AnyAsync(a => a.DoctorId == CurrentDoctor.Id && a.PatientId == patientId);
            if (!hasHistory)
            {
                // Aunque el paciente exista, si este Doctor no lo ha atendido, no debería ver su historial "vacío"
                 // Podríamos mostrar un mensaje o redirigir
                 // return Forbid("No tiene historial de citas con este paciente."); 
                 // O simplemente mostrar la página con 0 citas (como está ahora)
            }

            return Page();
        }
    }
} 





