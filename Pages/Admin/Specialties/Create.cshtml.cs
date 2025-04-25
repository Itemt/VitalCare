using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Specialties;

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
    public Specialty Specialty { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Normalize name for comparison (e.g., trim whitespace, consistent casing)
        var normalizedNewName = Specialty.Name.Trim();
        if (string.IsNullOrEmpty(normalizedNewName))
        {
             ModelState.AddModelError("Specialty.Name", "El nombre de la especialidad no puede estar vacío.");
             return Page();
        }

        // Check if specialty already exists (case-insensitive)
        bool specialtyExists = await _context.Specialties
            .AnyAsync(s => s.Name.ToUpper() == normalizedNewName.ToUpper());

        if (specialtyExists)
        {
            _logger.LogWarning("Intento de crear especialidad duplicada: {SpecialtyName}", normalizedNewName);
            ModelState.AddModelError("Specialty.Name", $"La especialidad '{normalizedNewName}' ya existe.");
            return Page();
        }

        // Assign normalized name before saving
        Specialty.Name = normalizedNewName;

        _context.Specialties.Add(Specialty);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nueva especialidad '{SpecialtyName}' (ID: {SpecialtyId}) creada exitosamente.", Specialty.Name, Specialty.Id);
            TempData["SuccessMessage"] = $"Especialidad '{Specialty.Name}' creada exitosamente.";
            return RedirectToPage("../ManageSpecialties");
        }
        catch (DbUpdateException ex)
        {
             _logger.LogError(ex, "Error al guardar la nueva especialidad '{SpecialtyName}'.", Specialty.Name);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la especialidad. Por favor, inténtelo de nuevo.");
            return Page();
        }
    }
}
