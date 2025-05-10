// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<User> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            _logger.LogInformation("ConfirmEmail OnGetAsync triggered. UserId: {UserId}, Code: {Code}", userId, code);

            if (userId == null || code == null)
            {
                _logger.LogWarning("ConfirmEmail OnGetAsync: UserId or Code is null. UserId: {UserId}, Code: {Code}", userId, code);
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmail OnGetAsync: User not found for UserId: {UserId}", userId);
                StatusMessage = "Error: No se pudo encontrar el usuario.";
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            string decodedCode = "";
            try
            {
                decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                _logger.LogInformation("ConfirmEmail OnGetAsync: Decoded code: {DecodedCode}", decodedCode);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmail OnGetAsync: Error decoding Base64Url code. Original code: {Code}", code);
                StatusMessage = "Error: El enlace de confirmación parece estar corrupto (código inválido).";
                return Page();
            }
            
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
            if (result.Succeeded)
            {
                _logger.LogInformation("ConfirmEmail OnGetAsync: Email confirmed successfully for UserId: {UserId}", userId);
                StatusMessage = "Gracias por confirmar tu correo electrónico.";
            }
            else
            {
                _logger.LogWarning("ConfirmEmail OnGetAsync: Email confirmation failed for UserId: {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                StatusMessage = "Error al confirmar tu correo electrónico.";
            }
            return Page();
        }
    }
}
