using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Pages.Patient
{
    [Authorize(Roles = "Paciente")]
    public class RateAppointmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public RateAppointmentModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public Appointment? Appointment { get; set; }
        public Models.Patient? CurrentPatient { get; set; }

        public class InputModel
        {
            public int AppointmentId { get; set; }

            [Required(ErrorMessage = "Por favor seleccione una calificación.")]
            [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas.")]
            [Display(Name = "Calificación")]
            public int PatientRating { get; set; }

            [StringLength(500, ErrorMessage = "El comentario no puede exceder los 500 caracteres.")]
            [Display(Name = "Comentario")]
            public string? PatientComment { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int appointmentId)
        {
            // Obtener el usuario actual
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Obtener el paciente actual
            CurrentPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (CurrentPatient == null)
            {
                return NotFound("Paciente no encontrado");
            }

            // Obtener la cita con todas las relaciones necesarias
            Appointment = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialty)
                .Include(a => a.Patient)
                .Include(a => a.Rating)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (Appointment == null)
            {
                return NotFound("Cita no encontrada");
            }

            // Verificar que la cita pertenece al paciente actual
            if (Appointment.PatientId != CurrentPatient.Id)
            {
                return Forbid("No tiene permiso para calificar esta cita");
            }

            // Verificar que la cita está completada y no cancelada
            if (!Appointment.IsCompleted || Appointment.IsCancelled)
            {
                TempData["ErrorMessage"] = "Solo se pueden calificar citas completadas.";
                return RedirectToPage("/Appointments/Index");
            }

            // Verificar que no haya sido calificada por el paciente ya
            if (Appointment.Rating != null && Appointment.Rating.PatientRating > 0)
            {
                TempData["InfoMessage"] = "Esta cita ya ha sido calificada.";
                return RedirectToPage("/Appointments/Index");
            }

            Input = new InputModel { AppointmentId = appointmentId };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Recargar datos para mostrar la página con errores
                await OnGetAsync(Input.AppointmentId);
                return Page();
            }

            // Obtener el usuario actual
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Obtener el paciente actual
            CurrentPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (CurrentPatient == null)
            {
                return NotFound("Paciente no encontrado");
            }

            // Obtener la cita
            Appointment = await _context.Appointments
                .Include(a => a.Rating)
                .FirstOrDefaultAsync(a => a.Id == Input.AppointmentId);

            if (Appointment == null)
            {
                return NotFound("Cita no encontrada");
            }

            // Verificar permisos
            if (Appointment.PatientId != CurrentPatient.Id)
            {
                return Forbid("No tiene permiso para calificar esta cita");
            }

            try
            {
                // Crear o actualizar la calificación
                if (Appointment.Rating == null)
                {
                    // Crear nueva calificación
                    var rating = new Rating
                    {
                        AppointmentId = Input.AppointmentId,
                        PatientRating = Input.PatientRating,
                        PatientComment = Input.PatientComment,
                        PatientRatingDate = DateTime.UtcNow
                    };

                    _context.Ratings.Add(rating);
                }
                else
                {
                    // Actualizar calificación existente
                    Appointment.Rating.PatientRating = Input.PatientRating;
                    Appointment.Rating.PatientComment = Input.PatientComment;
                    Appointment.Rating.PatientRatingDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "¡Gracias por su calificación! Su opinión es muy valiosa para nosotros.";
                return RedirectToPage("/Appointments/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar la calificación. Por favor, inténtelo de nuevo.");
                await OnGetAsync(Input.AppointmentId);
                return Page();
            }
        }
    }
} 