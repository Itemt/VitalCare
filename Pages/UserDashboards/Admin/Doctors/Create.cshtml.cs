using CitasEPS.Data;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Required for Linq methods like Select
using DoctorModel = CitasEPS.Models.Modules.Users.Doctor;

namespace CitasEPS.Pages.UserDashboards.Admin.Doctors;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;
    private readonly UserManager<User> _userManager; // Descomentado e inyectado

    public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger, UserManager<User> userManager) // Añadido userManager
    {
        _context = context;
        _logger = logger;
        _userManager = userManager; // Asignado userManager
    }

    [BindProperty]
    public DoctorModel Doctor { get; set; } = default!;

    public SelectList SpecialtySL { get; set; } = default!;
    public SelectList UserSL { get; set; } = default!; // Descomentado

    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return Page();
        }
        
        // Validar que se haya seleccionado un Usuario
        if (Doctor.UserId == null || Doctor.UserId == 0)
        {
            ModelState.AddModelError("Doctor.UserId", "Debe seleccionar un usuario para vincularlo al médico.");
            await PopulateDropdownsAsync();
            return Page();
        }

        // Validar si el Usuario seleccionado ya está vinculado a otro Médico
        if (await _context.Doctors.AnyAsync(d => d.UserId == Doctor.UserId))
        {
            ModelState.AddModelError("Doctor.UserId", "Este usuario ya está vinculado a otro perfil de médico.");
            await PopulateDropdownsAsync();
            return Page();
        }

        if (!string.IsNullOrEmpty(Doctor.LicenseNumber) && 
            await _context.Doctors.AnyAsync(d => d.LicenseNumber == Doctor.LicenseNumber))
        {
            ModelState.AddModelError("Doctor.MedicalLicenseNumber", "Ya existe un médico con este número de registro médico.");
            await PopulateDropdownsAsync();
            return Page();
        }

        _context.Doctors.Add(Doctor);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Nuevo médico '{FirstName} {LastName}' creado exitosamente y vinculado al usuario ID {UserId}.", Doctor.FirstName, Doctor.LastName, Doctor.UserId);
            TempData["SuccessMessage"] = $"Médico '{Doctor.FullName}' creado y vinculado exitosamente.";
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

        // Obtener IDs de usuarios que ya están vinculados a un médico
        var linkedUserIds = await _context.Doctors
                                      .Where(d => d.UserId.HasValue)
                                      .Select(d => d.UserId.Value)
                                      .ToListAsync();

        // Obtener usuarios con el rol "Doctor" que NO están en la lista de linkedUserIds
        var usersInRoleDoctor = await _userManager.GetUsersInRoleAsync("Doctor");
        var availableUsersForDoctorRole = usersInRoleDoctor
                                          .Where(u => !linkedUserIds.Contains(u.Id))
                                          .OrderBy(u => u.UserName)
                                          .Select(u => new { u.Id, DisplayName = $"{u.FirstName} {u.LastName} ({u.Email})" })
                                          .ToList();

        UserSL = new SelectList(availableUsersForDoctorRole, "Id", "DisplayName", Doctor?.UserId);
    }
}





