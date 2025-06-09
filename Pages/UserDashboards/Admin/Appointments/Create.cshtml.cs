using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.UserDashboards.Admin.Appointments;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public Appointment Appointment { get; set; } = new();

    public SelectList PatientSL { get; set; } = default!;
    public SelectList DoctorSL { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await PopulateDropdownsAsync(null, null);
        // Set a default future date/time? Optionally.
        // Appointment.AppointmentDateTime = DateTime.Now.AddDays(1);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Remove potential validation errors for navigation properties if they are not needed
                    // ModelState.Remove("Appointment.Patient");
            // ModelState.Remove("Appointment.Doctor");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Creación de cita fallida debido a errores de modelo.");
            await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
            return Page();
        }

                    // Optional: Add checks for scheduling conflicts (e.g., Doctor availability)
        // bool conflictExists = await _context.Appointments.AnyAsync(a => 
        //     a.DoctorId == Appointment.DoctorId && 
        //     a.AppointmentDateTime == Appointment.AppointmentDateTime);
        // if (conflictExists) {
        //     ModelState.AddModelError(string.Empty, "El médico ya tiene una cita programada en esta fecha y hora.");
        //     await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
        //     return Page();
        // }

        _context.Appointments.Add(Appointment);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nueva cita creada (ID: {AppointmentId}) para Paciente ID {PatientId} con Médico ID {DoctorId} el {DateTime}.",
                Appointment.Id, Appointment.PatientId, Appointment.DoctorId, Appointment.AppointmentDateTime);
            TempData["SuccessMessage"] = "Nueva cita creada exitosamente.";
            return RedirectToPage("../ManageAppointments");
        }
        catch (DbUpdateException ex)
        {
             _logger.LogError(ex, "Error al guardar la nueva cita.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la cita. Por favor, inténtelo de nuevo.");
            await PopulateDropdownsAsync(Appointment.PatientId, Appointment.DoctorId);
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync(int? selectedPatientId, int? selectedDoctorId)
    {
        var patients = await _context.Patients
                                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                                .Select(p => new { p.Id, p.FullName })
                                .ToListAsync();
        PatientSL = new SelectList(patients, "Id", "FullName", selectedPatientId);

        var doctors = await _context.Doctors
                                .Include(d => d.Specialty)
                                .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
                                .Select(d => new { d.Id, FullNameWithSpec = $"{d.FullName} ({d.Specialty.Name})" })
                                .ToListAsync();
        DoctorSL = new SelectList(doctors, "Id", "FullNameWithSpec", selectedDoctorId);
    }
}




