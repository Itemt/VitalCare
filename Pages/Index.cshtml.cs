using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using CitasEPS.Models;

namespace CitasEPS.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public IndexModel(ILogger<IndexModel> logger, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        // Si el usuario ya está logueado, redirigir según su rol
        if (_signInManager.IsSignedIn(User))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogInformation($"Admin {user.UserName} accessing root, redirecting to admin dashboard.");
                    return RedirectToPage("/UserDashboards/Admin/Index");
                }
                else if (await _userManager.IsInRoleAsync(user, "Doctor"))
                {
                    _logger.LogInformation($"Doctor {user.UserName} accessing root, redirecting to doctor dashboard.");
                    return RedirectToPage("/UserDashboards/Doctor/Index");
                }
                else if (await _userManager.IsInRoleAsync(user, "Paciente"))
                {
                    _logger.LogInformation($"Patient {user.UserName} accessing root, redirecting to patient dashboard.");
                    return RedirectToPage("/UserDashboards/Patient/Index");
                }
            }
        }
        
        // Si no está logueado o no tiene rol específico, redirigir a la página principal pública
        return RedirectToPage("/Public/Index");
    }
}



