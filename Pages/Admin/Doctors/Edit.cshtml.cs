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

    public EditModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<EditModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
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

        Doctor = await _context.Doctors
            .Include(d => d.Specialty)
            .Include(d => d.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (Doctor == null)
        {
            return NotFound();
        }

        SpecialtySL = new SelectList(await _context.Specialties.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", Doctor.SpecialtyId);

        if (Doctor.UserId == null || Doctor.User == null)
        {
            UnlinkedUserMessage = Doctor.User == null ? 
                "Este perfil de doctor no está vinculado a una cuenta de usuario del sistema." : 
                $"Este perfil de doctor está vinculado a un UserId ({Doctor.UserId}) que no corresponde a un usuario existente.";

            var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
            if (doctorRole != null)
            {
                var linkedUserIds = await _context.Doctors
                    .Where(d => d.UserId.HasValue && d.Id != Doctor.Id)
                    .Select(d => d.UserId.Value)
                    .ToListAsync();

                var unlinkedUsers = await _context.UserRoles
                    .Where(ur => ur.RoleId == doctorRole.Id && !linkedUserIds.Contains(ur.UserId))
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                
                var availableUsersForLinking = await _userManager.Users
                    .Where(u => unlinkedUsers.Contains(u.Id))
                    .OrderBy(u => u.Email)
                    .Select(u => new { u.Id, DisplayName = u.Email + (string.IsNullOrEmpty(u.FirstName) && string.IsNullOrEmpty(u.LastName) ? "" : " (" + u.FirstName + " " + u.LastName + ")") })
                    .ToListAsync();
                
                UserSL = new SelectList(availableUsersForLinking, "Id", "DisplayName");
            }
        }
        else
        {
            _logger.LogInformation($"Doctor ID {Doctor.Id} ({Doctor.FullName}) is currently linked to User ID {Doctor.UserId} ({Doctor.User.Email}).");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(Doctor?.SpecialtyId, true);
            return Page();
        }
        
        var doctorToUpdate = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);

        if (doctorToUpdate == null)
        {
            return NotFound();
        }

        doctorToUpdate.FirstName = Doctor.FirstName;
        doctorToUpdate.LastName = Doctor.LastName;
        doctorToUpdate.Email = Doctor.Email;
        doctorToUpdate.PhoneNumber = Doctor.PhoneNumber;
        doctorToUpdate.MedicalLicenseNumber = Doctor.MedicalLicenseNumber;
        doctorToUpdate.SpecialtyId = Doctor.SpecialtyId;

        if (Request.Form.ContainsKey("Doctor.UserId") && int.TryParse(Request.Form["Doctor.UserId"], out int selectedUserId) && selectedUserId > 0)
        {
            var existingDoctorWithUser = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == selectedUserId && d.Id != doctorToUpdate.Id);

            if (existingDoctorWithUser != null)
            {
                ModelState.AddModelError("Doctor.UserId", $"El usuario seleccionado ya está vinculado al doctor {existingDoctorWithUser.FullName}.");
                await PopulateSelectListsAsync(doctorToUpdate.SpecialtyId, true);
                return Page();
            }
            doctorToUpdate.UserId = selectedUserId;
        }

        _context.Attach(doctorToUpdate).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Doctor ID {doctorToUpdate.Id} ({doctorToUpdate.FullName}) updated. Linked UserID: {doctorToUpdate.UserId?.ToString() ?? "None"}.");
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
                await PopulateSelectListsAsync(doctorToUpdate.SpecialtyId, doctorToUpdate.UserId == null);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating Doctor ID {Doctor.Id}.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el doctor.");
            await PopulateSelectListsAsync(doctorToUpdate.SpecialtyId, doctorToUpdate.UserId == null);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private bool DoctorExists(int id)
    {
        return _context.Doctors.Any(e => e.Id == id);
    }

    private async Task PopulateSelectListsAsync(int? specialtyId, bool populateUserListIfNeeded)
    {
        SpecialtySL = new SelectList(await _context.Specialties.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", specialtyId);
        if (populateUserListIfNeeded)
        {
            var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
            if (doctorRole != null)
            {
                var linkedUserIds = await _context.Doctors
                    .Where(d => d.UserId.HasValue && d.Id != Doctor.Id)
                    .Select(d => d.UserId.Value)
                    .ToListAsync();
                var unlinkedUsers = await _context.UserRoles
                    .Where(ur => ur.RoleId == doctorRole.Id && !linkedUserIds.Contains(ur.UserId))
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                var availableUsersForLinking = await _userManager.Users
                    .Where(u => unlinkedUsers.Contains(u.Id) || (Doctor.UserId.HasValue && u.Id == Doctor.UserId.Value))
                    .OrderBy(u => u.Email)
                    .Select(u => new { u.Id, DisplayName = u.Email + (string.IsNullOrEmpty(u.FirstName) && string.IsNullOrEmpty(u.LastName) ? "" : " (" + u.FirstName + " " + u.LastName + ")") })
                    .ToListAsync();
                UserSL = new SelectList(availableUsersForLinking, "Id", "DisplayName", Doctor?.UserId);
            }
        }
    }
}
