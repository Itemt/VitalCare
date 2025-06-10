using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Data;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ConfirmEmailsModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfirmEmailsModel> _logger;

        public ConfirmEmailsModel(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<ConfirmEmailsModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public List<User> UnconfirmedUsers { get; set; } = new List<User>();
        public int TotalUsers { get; set; }
        public int ConfirmedUsers { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            // Obtener todos los usuarios
            var allUsers = await _context.Users.ToListAsync();
            TotalUsers = allUsers.Count;

            // Filtrar usuarios sin confirmar email
            UnconfirmedUsers = allUsers.Where(u => !u.EmailConfirmed).ToList();
            ConfirmedUsers = TotalUsers - UnconfirmedUsers.Count;

            _logger.LogInformation("Admin Email Confirmation Page: {TotalUsers} total users, {UnconfirmedCount} unconfirmed", 
                TotalUsers, UnconfirmedUsers.Count);
        }

        public async Task<IActionResult> OnPostAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                StatusMessage = "Error: Usuario no encontrado.";
                return RedirectToPage();
            }

            try
            {
                // Confirmar email directamente
                user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin manually confirmed email for user {UserId} ({Email})", user.Id, user.Email);
                    StatusMessage = $"✅ Email confirmado exitosamente para {user.Email}";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to confirm email for user {UserId}: {Errors}", user.Id, errors);
                    StatusMessage = $"❌ Error confirmando email: {errors}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while confirming email for user {UserId}", user.Id);
                StatusMessage = $"❌ Error inesperado: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmAllAsync()
        {
            try
            {
                var unconfirmedUsers = await _context.Users
                    .Where(u => !u.EmailConfirmed)
                    .ToListAsync();

                if (!unconfirmedUsers.Any())
                {
                    StatusMessage = "ℹ️ No hay usuarios con emails pendientes de confirmación.";
                    return RedirectToPage();
                }

                int successCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                foreach (var user in unconfirmedUsers)
                {
                    try
                    {
                        user.EmailConfirmed = true;
                        var result = await _userManager.UpdateAsync(user);

                        if (result.Succeeded)
                        {
                            successCount++;
                            _logger.LogInformation("Admin bulk confirmed email for user {UserId} ({Email})", user.Id, user.Email);
                        }
                        else
                        {
                            errorCount++;
                            var userErrors = string.Join(", ", result.Errors.Select(e => e.Description));
                            errors.Add($"{user.Email}: {userErrors}");
                            _logger.LogError("Failed to bulk confirm email for user {UserId}: {Errors}", user.Id, userErrors);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"{user.Email}: {ex.Message}");
                        _logger.LogError(ex, "Exception during bulk email confirmation for user {UserId}", user.Id);
                    }
                }

                if (errorCount == 0)
                {
                    StatusMessage = $"✅ {successCount} emails confirmados exitosamente.";
                }
                else if (successCount > 0)
                {
                    StatusMessage = $"⚠️ {successCount} emails confirmados, {errorCount} errores: {string.Join("; ", errors.Take(3))}";
                }
                else
                {
                    StatusMessage = $"❌ Error confirmando todos los emails: {string.Join("; ", errors.Take(3))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during bulk email confirmation");
                StatusMessage = $"❌ Error inesperado durante confirmación masiva: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
} 