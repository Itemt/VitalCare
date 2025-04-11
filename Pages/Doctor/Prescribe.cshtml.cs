using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PrescribeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PrescribeModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Prescription? Prescription { get; set; }

        public Appointment? Appointment { get; set; }
        public SelectList? Medications { get; set; }

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            if (appointmentId == null)
            {
                return NotFound("ID de cita no proporcionado.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Not logged in

            // Obtener el Doctor asociado al usuario
            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (currentDoctor == null) return Forbid("El usuario actual no es un doctor registrado.");

            // Cargar la cita, asegurándose que pertenece al doctor actual
            Appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor) // Aunque ya tenemos currentDoctor, es bueno tenerlo en el objeto Appointment
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (Appointment == null)
            {
                return NotFound("Cita no encontrada.");
            }

            // Verificar si la cita pertenece al doctor actual
            if (Appointment.DoctorId != currentDoctor.Id)
            {
                return Forbid("No tiene permiso para prescribir en esta cita.");
            }

            // Preparar el modelo de Prescripción para la vista
            Prescription = new Prescription
            {
                AppointmentId = Appointment.Id,
                DoctorId = currentDoctor.Id,
                PatientId = Appointment.PatientId,
                PrescriptionDate = DateTime.UtcNow // Fecha actual por defecto
            };

            // Cargar lista de medicamentos para el dropdown
            var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
            Medications = new SelectList(medicationsList, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == user.Email);
            if (currentDoctor == null) return Forbid();

            // Volver a cargar la cita original para validación y contexto
            Appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == Prescription.AppointmentId);

            if (Appointment == null)
            {
                return NotFound("Cita asociada no encontrada.");
            }

            // Validar que el doctor que postea es el doctor de la cita
            if (Appointment.DoctorId != currentDoctor.Id)
            {
                return Forbid("Intento de prescripción no autorizado.");
            }

            // Asignar IDs que no vienen del form directamente (seguridad)
            Prescription.DoctorId = currentDoctor.Id;
            Prescription.PatientId = Appointment.PatientId;
            Prescription.PrescriptionDate = DateTime.UtcNow; // Usar fecha/hora del servidor

            // Quitar validación del modelo para propiedades de navegación antes de ModelState.IsValid
            ModelState.Remove("Prescription.Appointment");
            ModelState.Remove("Prescription.Medication");
            ModelState.Remove("Prescription.Doctor");
            ModelState.Remove("Prescription.Patient");

            if (!ModelState.IsValid)
            {
                // Si el modelo no es válido, recargar la lista de medicamentos
                var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
                Medications = new SelectList(medicationsList, "Id", "Name", Prescription.MedicationId);
                // Appointment ya fue cargado
                return Page();
            }

            _context.Prescriptions.Add(Prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescripción creada exitosamente."; // Mensaje de éxito

            // Redirigir de vuelta a la agenda del doctor
            return RedirectToPage("./Agenda");
        }
    }
} 