using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Patients;

[Authorize(Roles = "Admin")]
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
    public Patient Patient { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var patient = await _context.Patients
                                // .Include(p => p.User) // Include User if displaying associated account
                                .FirstOrDefaultAsync(m => m.Id == id);

        if (patient == null)
        {
            _logger.LogWarning("Intento de edición para Paciente ID {PatientId} no encontrado (GET).", id);
            return NotFound($"Paciente con ID {id} no encontrado.");
        }
        Patient = patient;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if DocumentId was changed and if the new one conflicts
        var existingPatientWithDoc = await _context.Patients
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(p => p.DocumentId == Patient.DocumentId && p.Id != Patient.Id);

        if (existingPatientWithDoc != null)
        {
            ModelState.AddModelError("Patient.DocumentId", "Este número de documento ya está registrado para otro paciente.");
            return Page();
        }

        // Optional: Check for email conflict if email should be unique among patients

        _context.Attach(Patient).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Paciente ID {PatientId} ({FullName}) actualizado exitosamente.", Patient.Id, Patient.FullName);
            TempData["SuccessMessage"] = $"Paciente '{Patient.FullName}' actualizado exitosamente.";
        }
        catch (DbUpdateConcurrencyException ex)
        {
             _logger.LogWarning(ex, "Error de concurrencia al actualizar paciente ID {PatientId}.", Patient.Id);
            ModelState.AddModelError(string.Empty, 
                "Los datos del paciente fueron modificados por otro usuario. Por favor, recargue la página e inténtelo de nuevo.");
             // Detach the conflicting entity so it can be reloaded if needed
            _context.Entry(Patient).State = EntityState.Detached; 
            return Page();
        }
         catch (DbUpdateException ex)
        {
             _logger.LogError(ex, "Error al actualizar paciente ID {PatientId}.", Patient.Id);
             ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los cambios. Por favor, inténtelo de nuevo.");
             return Page();
        }

        return RedirectToPage("../ManagePatients");
    }
}
