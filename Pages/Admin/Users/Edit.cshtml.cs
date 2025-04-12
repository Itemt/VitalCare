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
public class EditModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger<EditModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<IdentityRole<int>> AllRoles { get; set; } = new();
    public IList<string> UserRoles { get; set; } = new List<string>();

    public class InputModel
    {
        [Required]
        public int Id { get; set; }

        public string? UserName { get; set; } // Display only

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
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
            Email = user.Email ?? ""
        };

        AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        UserRoles = await _userManager.GetRolesAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string[] SelectedRoles)
    {
        if (!ModelState.IsValid)
        {
             // Repopulate roles if validation fails
             var userForRoles = await _userManager.FindByIdAsync(Input.Id.ToString());
             if (userForRoles != null) {
                 AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                 UserRoles = await _userManager.GetRolesAsync(userForRoles);
             }
             Input.UserName = userForRoles?.UserName; // Need username for display
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.Id.ToString());
        if (user == null)
        {            
            return NotFound($"Usuario con ID {Input.Id} no encontrado durante el POST.");
        }

        // Update Email if changed (consider security implications - maybe confirm email?)
        var currentEmail = await _userManager.GetEmailAsync(user);
        if (Input.Email != currentEmail)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
            if (!setEmailResult.Succeeded)
            {
                AddModelErrors(setEmailResult);
                // Repopulate roles before returning page
                 AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                 UserRoles = await _userManager.GetRolesAsync(user);
                 Input.UserName = user.UserName;
                return Page();
            }
            // Optionally update UserName to match Email if that's your policy
            // await _userManager.SetUserNameAsync(user, Input.Email); 
        }

        // Update Roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = SelectedRoles.Except(currentRoles).ToArray();
        var rolesToRemove = currentRoles.Except(SelectedRoles).ToArray();

        IdentityResult addResult = IdentityResult.Success;
        IdentityResult removeResult = IdentityResult.Success;

        if (rolesToAdd.Any())
        {
            addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
        }
        if (!addResult.Succeeded)
        {
            AddModelErrors(addResult);
             // Repopulate roles before returning page
             AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
             UserRoles = await _userManager.GetRolesAsync(user);
             Input.UserName = user.UserName;
            return Page();
        }

        if (rolesToRemove.Any())
        {
            removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }
        if (!removeResult.Succeeded)
        {
            AddModelErrors(removeResult);
             // Repopulate roles before returning page
             AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
             UserRoles = await _userManager.GetRolesAsync(user);
             Input.UserName = user.UserName;
            return Page();
        }

        _logger.LogInformation("User {UserId} updated successfully.", user.Id);
        TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
        return RedirectToPage("../ManageUsers");
    }

    private void AddModelErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
