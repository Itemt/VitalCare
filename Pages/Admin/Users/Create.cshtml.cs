using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger<CreateModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public List<IdentityRole<int>> AllRoles { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        // Load roles for selection
        AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(string[] SelectedRoles)
    {
        if (!ModelState.IsValid)
        {
            // Repopulate roles if validation fails
            AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        var user = new User { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true }; // Consider EmailConfirmed status

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin created a new user {UserName} with password.", user.UserName);

            // Assign selected roles
            if (SelectedRoles != null && SelectedRoles.Any())
            {
                var rolesResult = await _userManager.AddToRolesAsync(user, SelectedRoles);
                if (!rolesResult.Succeeded)
                {
                     _logger.LogWarning("Could not add roles to user {UserName}: {Errors}", user.UserName, string.Join(", ", rolesResult.Errors.Select(e => e.Description)));
                     // Add role errors to ModelState, potentially delete user or let admin fix?
                     AddModelErrors(rolesResult); 
                     // User created but roles failed, delete user to keep things clean?
                     // await _userManager.DeleteAsync(user);
                     AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                     return Page(); 
                }
                _logger.LogInformation("Added roles {Roles} to user {UserName}", string.Join(", ", SelectedRoles), user.UserName);
            }

            TempData["SuccessMessage"] = $"Usuario '{user.UserName}' creado exitosamente.";
            return RedirectToPage("../ManageUsers");
        }
        else
        {
            AddModelErrors(result);
            // Repopulate roles if creation fails
            AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            return Page();
        }
    }

     private void AddModelErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
