using System.Linq;
using System.Threading.Tasks;
using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Pages.Admin.Doctors;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<EditModel> _logger;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public EditModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<EditModel> logger, RoleManager<IdentityRole<int>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _roleManager = roleManager;
    }

    [BindProperty]
    public Models.Doctor Doctor { get; set; } = default!;

    public SelectList? SpecialtySL { get; set; }
    public SelectList? UserSL { get; set; }
    public string? UnlinkedUserMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctorData = await _context.Doctors
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (doctorData == null)
        {
            return NotFound();
        }
        Doctor = doctorData;

        await PopulateSelectListsAsync(Doctor.UserId, Doctor.SpecialtyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(Doctor.UserId, Doctor.SpecialtyId);
            return Page();
        }
        
        if (Doctor.UserId.HasValue)
        {
            var doctorWithSameUser = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == Doctor.UserId && d.Id != Doctor.Id);

            if (doctorWithSameUser != null)
            {
                ModelState.AddModelError("Doctor.UserId", "Este usuario ya está asignado a otro perfil de doctor.");
                await PopulateSelectListsAsync(Doctor.UserId, Doctor.SpecialtyId);
                return Page();
            }
        }

        _context.Attach(Doctor).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Doctor ID {Doctor.Id} ({Doctor.FullName}) updated. Linked UserID: {Doctor.UserId?.ToString() ?? "None"}.");
            TempData["SuccessMessage"] = "Doctor actualizado exitosamente.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DoctorExists(Doctor.Id))
            {
                return NotFound();
            }
            else
            {
                _logger.LogError($"Concurrency error updating Doctor ID {Doctor.Id}.");
                ModelState.AddModelError(string.Empty, "Error de concurrencia. El registro fue modificado por otro usuario.");
                await PopulateSelectListsAsync(Doctor.UserId, Doctor.SpecialtyId);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating Doctor ID {Doctor.Id}.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el doctor.");
            await PopulateSelectListsAsync(Doctor.UserId, Doctor.SpecialtyId);
            return Page();
        }

        return RedirectToPage("/Admin/ManageDoctors");
    }

    private bool DoctorExists(int id)
    {
        return _context.Doctors.Any(e => e.Id == id);
    }

    private async Task PopulateSelectListsAsync(int? selectedUserId, int? selectedSpecialtyId)
    {
        var doctorRole = await _roleManager.FindByNameAsync("Doctor");
        if (doctorRole == null)
        {
            UserSL = new SelectList(Enumerable.Empty<SelectListItem>());
            SpecialtySL = new SelectList(_context.Specialties, "Id", "Name", selectedSpecialtyId);
            return;
        }

        var usersInDoctorRole = await _userManager.GetUsersInRoleAsync(doctorRole.Name!);

        var assignedUserIds = await _context.Doctors
            .Where(d => d.UserId.HasValue && d.Id != Doctor.Id)
            .Select(d => d.UserId)
            .ToListAsync();

        var availableUsers = usersInDoctorRole
            .Where(u => u.Id == selectedUserId || !assignedUserIds.Contains(u.Id))
            .Select(u => new { u.Id, DisplayName = $"{u.FirstName} {u.LastName} (ID: {u.Id})" })
            .ToList();
        
        UserSL = new SelectList(availableUsers, "Id", "DisplayName", selectedUserId);
        SpecialtySL = new SelectList(_context.Specialties.AsNoTracking(), "Id", "Name", selectedSpecialtyId);
    }
}
