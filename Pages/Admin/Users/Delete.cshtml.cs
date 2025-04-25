using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(UserManager<User> userManager, ILogger<DeleteModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public IList<string> UserRoles { get; set; } = new List<string>();

    public class InputModel
    {
        [Required]
        public int Id { get; set; }
        public string? UserName { get; set; } // Display only
        public string? Email { get; set; } // Display only
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado.");
        }

        Input = new InputModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
        
        UserRoles = await _userManager.GetRolesAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Note: Consider adding checks here. 
        // Can the current admin delete themselves? 
        // Should a user associated with Patients/Doctors/Appointments be deleted? 
        // (Requires injecting DbContext and querying related tables)

        var user = await _userManager.FindByIdAsync(Input.Id.ToString());
        if (user == null)
        {
            // User might have been deleted between GET and POST
            TempData["ErrorMessage"] = $"Usuario con ID {Input.Id} no encontrado. Quizás ya fue eliminado.";
            return RedirectToPage("../ManageUsers");
        }

        // Prevent admin from deleting themselves (optional but recommended)
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && currentUser.Id == user.Id)
        {
            ModelState.AddModelError(string.Empty, "No puede eliminar su propia cuenta de administrador.");
            // Repopulate roles for display if returning Page
             UserRoles = await _userManager.GetRolesAsync(user);
             // Input properties are already bound
            return Page();
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} with UserName {UserName} was deleted.", user.Id, user.UserName);
            TempData["SuccessMessage"] = $"Usuario '{user.UserName}' eliminado exitosamente.";
            return RedirectToPage("../ManageUsers");
        }
        else
        {
            _logger.LogError("Error deleting user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            // Repopulate roles for display if returning Page
            UserRoles = await _userManager.GetRolesAsync(user);
            // Input properties are already bound
            return Page();
        }
    }
}
