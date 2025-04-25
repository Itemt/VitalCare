using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Doctors;

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
    public Models.Doctor Doctor { get; set; } = default!;

    public SelectList SpecialtySL { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var doctor = await _context.Doctors
                                 .Include(d => d.Specialty) // Include specialty for display/initial selection
                                 .FirstOrDefaultAsync(m => m.Id == id);

        if (doctor == null)
        {
            return NotFound($"Médico con ID {id} no encontrado.");
        }
        Doctor = doctor;
        await PopulateDropdownsAsync(Doctor.SpecialtyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(Doctor.SpecialtyId);
            return Page();
        }

        // Load original doctor data to compare license number
        var originalDoctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == Doctor.Id);
        if (originalDoctor == null) return NotFound();
        var originalLicenseNumber = originalDoctor.MedicalLicenseNumber;

        // Check if another doctor already has the new license number (if it changed)
        if (originalLicenseNumber != Doctor.MedicalLicenseNumber && 
            !string.IsNullOrEmpty(Doctor.MedicalLicenseNumber) &&
            await _context.Doctors.AnyAsync(d => d.Id != Doctor.Id && d.MedicalLicenseNumber == Doctor.MedicalLicenseNumber))
        {
            ModelState.AddModelError("Doctor.MedicalLicenseNumber", "Ya existe otro médico con este número de registro médico.");
            await PopulateDropdownsAsync(Doctor.SpecialtyId);
            return Page();
        }

        // Update doctor in the database
        _context.Attach(Doctor).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Médico {DoctorId} actualizado exitosamente.", Doctor.Id);
            TempData["SuccessMessage"] = $"Médico '{Doctor.FullName}' actualizado exitosamente.";
        }
        catch (DbUpdateConcurrencyException ex) // Optional: Handle concurrency
        {
             _logger.LogWarning(ex, "Error de concurrencia al actualizar médico {DoctorId}.", Doctor.Id);
            // Handle concurrency - reload data, show error, etc.
            // For now, just adding a model error
            ModelState.AddModelError(string.Empty, 
                "Los datos del médico fueron modificados por otro usuario. Por favor, recargue la página e inténtelo de nuevo.");
            // Detach the entity so it can be reloaded if needed
            _context.Entry(Doctor).State = EntityState.Detached; 
            await PopulateDropdownsAsync(Doctor.SpecialtyId);
            return Page();

        }
        catch (DbUpdateException ex)
        {
             _logger.LogError(ex, "Error al actualizar médico {DoctorId}.", Doctor.Id);
             ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los cambios. Por favor, inténtelo de nuevo.");
             await PopulateDropdownsAsync(Doctor.SpecialtyId);
             return Page();
        }

        return RedirectToPage("../ManageDoctors");
    }

     private async Task PopulateDropdownsAsync(int? selectedSpecialtyId)
    {
        var specialties = await _context.Specialties
                                    .OrderBy(s => s.Name)
                                    .ToListAsync();
        SpecialtySL = new SelectList(specialties, nameof(Specialty.Id), nameof(Specialty.Name), selectedSpecialtyId);
    }
}
