using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Admin.Patients;

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
    public CitasEPS.Models.Patient Patient { get; set; } = new();

    // Add SelectList properties here if needed for dropdowns (e.g., UserSL)

    public IActionResult OnGet()
    {
        // Pre-populate dropdowns if needed (e.g., for linking to User accounts)
        // await PopulateDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // await PopulateDropdownsAsync(); // Repopulate if dropdowns exist
            return Page();
        }

        // Check if DocumentId already exists
        bool documentExists = await _context.Patients.AnyAsync(p => p.DocumentId == Patient.DocumentId);
        if (documentExists)
        {
            ModelState.AddModelError("Patient.DocumentId", "Este número de documento ya está registrado.");
            // await PopulateDropdownsAsync();
            return Page();
        }

        // Check if Email already exists (optional, depends on requirements)
        // bool emailExists = await _context.Patients.AnyAsync(p => p.Email == Patient.Email);
        // if (emailExists)
        // {
        //     ModelState.AddModelError("Patient.Email", "Este correo electrónico ya está registrado.");
        //     // await PopulateDropdownsAsync();
        //     return Page();
        // }

        _context.Patients.Add(Patient);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nuevo paciente '{FirstName} {LastName}' (ID: {PatientId}) creado exitosamente.", 
                Patient.FirstName, Patient.LastName, Patient.Id);
            TempData["SuccessMessage"] = $"Paciente '{Patient.FullName}' creado exitosamente.";
            return RedirectToPage("../ManagePatients");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al guardar el nuevo paciente.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el paciente. Por favor, inténtelo de nuevo.");
            // await PopulateDropdownsAsync();
            return Page();
        }
    }

    // Helper method to populate dropdowns if needed
    // private async Task PopulateDropdownsAsync()
    // {
    //     // Example: Load users
    //     var users = await _userManager.Users.OrderBy(u => u.UserName).Select(u => new { u.Id, u.UserName }).ToListAsync();
    //     UserSL = new SelectList(users, "Id", "UserName", Patient?.UserId);
    // }
}
