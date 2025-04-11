using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Appointments
{
    // [Authorize] // O política específica como "Admin" si solo admins pueden editar todas las citas
    // Por ahora, asumiremos que el paciente puede editar (quizás con limitaciones)
    [Authorize(Roles = "Patient,Admin")] // Permitir a ambos editar por ahora
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Appointment Appointment { get; set; } = default!;

        // Propiedades para dropdowns
        public SelectList PatientNameSL { get; set; } = default!;
        public SelectList DoctorNameSL { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Obtener cita a editar, sin rastreo inicial
            var appointment = await _context.Appointments.AsNoTracking()
                                          .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }
            Appointment = appointment;

            // TODO: Verificar autorización - ¿Este paciente/admin puede editar ESTA cita?
            // Ejemplo: Si es paciente, verificar que Appointment.PatientId coincida con el ID del paciente logueado.

            // Poblar dropdowns
            await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
            return Page();
        }

        // Para proteger de ataques de sobreposteo, habilite las propiedades específicas a las que desea enlazar.
        // Para más detalles, vea https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
             // TODO: Verificar autorización antes de guardar - ¿Puede el usuario actual modificar esta cita?

            if (!ModelState.IsValid)
            {
                // Repoblar dropdowns si la validación falla
                await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                return Page();
            }

            _context.Attach(Appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita actualizada exitosamente.";
                 _logger.LogInformation("Cita con ID: {AppointmentId} actualizada.", Appointment.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Error de concurrencia actualizando cita ID: {AppointmentId}", Appointment.Id);
                if (!AppointmentExists(Appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    // Añadir manejo de error de concurrencia si es necesario
                    ModelState.AddModelError(string.Empty, "La cita fue modificada por otro usuario. Por favor, recargue la página e intente de nuevo.");
                    await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error inesperado actualizando cita ID: {AppointmentId}", Appointment.Id);
                 ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al guardar los cambios.");
                 await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
                 return Page();
            }

            return RedirectToPage("./Index");
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // Método auxiliar para cargar datos para dropdowns, seleccionando valores actuales
        private async Task PopulateDropdownsAsync(object? selectedPatient = null, object? selectedDoctor = null)
        {
             var patients = await _context.Patients
                                        .OrderBy(p => p.LastName)
                                        .ThenBy(p => p.FirstName)
                                        .ToListAsync();
            PatientNameSL = new SelectList(patients, nameof(Patient.Id), nameof(Patient.FullName), selectedPatient);

            var doctors = await _context.Doctors
                                      .OrderBy(d => d.LastName)
                                      .ThenBy(d => d.FirstName)
                                      .ToListAsync();
            DoctorNameSL = new SelectList(doctors, nameof(Doctor.Id), nameof(Doctor.FullName), selectedDoctor);
        }
    }
} 