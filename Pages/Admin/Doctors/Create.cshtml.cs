using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Doctors;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;
    // Optionally Inject UserManager if linking Doctors to User accounts
    // private readonly UserManager<User> _userManager;

    public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public Models.Doctor Doctor { get; set; } = default!;

    public SelectList SpecialtySL { get; set; } = default!;
    // public SelectList UserSL { get; set; } = default!; // For linking User accounts

    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Manual validation if needed (e.g., check if license number is unique)

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return Page();
        }

        // Validar si ya existe un médico con el mismo número de licencia
        if (!string.IsNullOrEmpty(Doctor.MedicalLicenseNumber) && 
            await _context.Doctors.AnyAsync(d => d.MedicalLicenseNumber == Doctor.MedicalLicenseNumber))
        {
            ModelState.AddModelError("Doctor.MedicalLicenseNumber", "Ya existe un médico con este número de registro médico.");
            await PopulateDropdownsAsync();
            return Page();
        }

        _context.Doctors.Add(Doctor);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nuevo médico '{FirstName} {LastName}' creado exitosamente.", Doctor.FirstName, Doctor.LastName);
            TempData["SuccessMessage"] = $"Médico '{Doctor.FullName}' creado exitosamente.";
            return RedirectToPage("../ManageDoctors");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al guardar el nuevo médico.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el médico. Por favor, inténtelo de nuevo.");
            await PopulateDropdownsAsync();
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        var specialties = await _context.Specialties
                                    .OrderBy(s => s.Name)
                                    .ToListAsync();
        SpecialtySL = new SelectList(specialties, nameof(Specialty.Id), nameof(Specialty.Name), Doctor?.SpecialtyId);

        // Optional: Populate Users for linking
        /*
        var users = await _userManager.Users
                              .OrderBy(u => u.UserName)
                              .Select(u => new { u.Id, u.UserName })
                              .ToListAsync();
        UserSL = new SelectList(users, "Id", "UserName", Doctor?.UserId);
        */
    }
}
