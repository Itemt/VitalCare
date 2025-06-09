using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class EditPrescriptionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditPrescriptionModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Prescription Prescription { get; set; } = default!;
        public SelectList? Medications { get; set; }
        public Appointment? RelatedAppointment { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound("ID de prescripción no proporcionado.");
            }

            Prescription = await _context.Prescriptions
                                    .Include(p => p.Appointment) // Necesitamos la cita para verificar el Doctor
                                    .FirstOrDefaultAsync(m => m.Id == id);

            if (Prescription == null)
            {
                return NotFound("Prescripción no encontrada.");
            }

            // Verificar que el Doctor logueado es el Doctor de la cita asociada a la prescripción
            var user = await _userManager.GetUserAsync(User);
            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (user == null || currentDoctor == null || Prescription.DoctorId != currentDoctor.Id)
            {
                 // También se puede verificar comparando Prescription.Appointment.DoctorId == currentDoctor.Id si se confía en los datos de la cita
                return Forbid("No tiene permiso para editar esta prescripción.");
            }

            RelatedAppointment = Prescription.Appointment; // Guardar para la vista

            // Validar que la cita no esté completada
            if (RelatedAppointment != null && RelatedAppointment.IsCompleted)
            {
                TempData["ErrorMessage"] = "No se pueden editar prescripciones de citas completadas.";
                return RedirectToPage("/Appointments/Details", new { id = RelatedAppointment.Id });
            }

            // Validar que la cita no esté expirada
            if (RelatedAppointment != null && RelatedAppointment.AppointmentDateTime < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "No se pueden editar prescripciones de citas expiradas.";
                return RedirectToPage("/Appointments/Details", new { id = RelatedAppointment.Id });
            }

            // Cargar lista de medicamentos
            var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
            Medications = new SelectList(medicationsList, "Id", "Name", Prescription.MedicationId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
             // Cargar la prescripción original desde la BD para evitar overposting de IDs clave
            var prescriptionToUpdate = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescriptionToUpdate == null)
            {
                return NotFound();
            }

             // Verificar permiso de nuevo
            var user = await _userManager.GetUserAsync(User);
            var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
             if (user == null || currentDoctor == null || prescriptionToUpdate.DoctorId != currentDoctor.Id)
            {
                return Forbid("No tiene permiso para guardar cambios en esta prescripción.");
            }

            // Validar que la cita no esté completada o expirada
            if (prescriptionToUpdate.Appointment != null)
            {
                if (prescriptionToUpdate.Appointment.IsCompleted)
                {
                    TempData["ErrorMessage"] = "No se pueden editar prescripciones de citas completadas.";
                    return RedirectToPage("/Appointments/Details", new { id = prescriptionToUpdate.AppointmentId });
                }

                if (prescriptionToUpdate.Appointment.AppointmentDateTime < DateTime.UtcNow)
                {
                    TempData["ErrorMessage"] = "No se pueden editar prescripciones de citas expiradas.";
                    return RedirectToPage("/Appointments/Details", new { id = prescriptionToUpdate.AppointmentId });
                }
            }

            // Intentar actualizar solo las propiedades permitidas (MedicationId, Dosage, Instructions)
            if (await TryUpdateModelAsync<Prescription>(
                prescriptionToUpdate,
                "Prescription", // Prefijo usado en el BindProperty
                p => p.MedicationId, p => p.Dosage, p => p.Instructions))
            {
                // No se modifica PrescriptionDate, DoctorId, PatientId, AppointmentId
                 try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Prescripción actualizada exitosamente.";
                    // Redirigir a los detalles de la cita original
                    return RedirectToPage("/Appointments/Details", new { id = prescriptionToUpdate.AppointmentId }); 
                }
                 catch (DbUpdateConcurrencyException)
                {
                    if (!PrescriptionExists(prescriptionToUpdate.Id))
                    { return NotFound(); }
                    else { throw; }
                }
                catch (Exception ex)
                {
                    // Log error with details
                    Console.Error.WriteLine($"Error al guardar prescripción: {ex.Message}");
                    TempData["ErrorMessage"] = "Ha ocurrido un error al guardar los cambios en la prescripción.";
                    ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                }
            }

            // Si falla el TryUpdateModelAsync o hay otro error, recargar datos y mostrar página
            RelatedAppointment = prescriptionToUpdate.Appointment; // Recargar para la vista
            var medicationsList = await _context.Medications.OrderBy(m => m.Name).ToListAsync();
            Medications = new SelectList(medicationsList, "Id", "Name", Prescription.MedicationId);
            return Page();
        }

        private bool PrescriptionExists(int id)
        {
          return _context.Prescriptions.Any(e => e.Id == id);
        }
    }
} 




