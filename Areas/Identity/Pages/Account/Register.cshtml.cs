// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
        ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
        ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
        ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
        ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
            ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
            /// </summary>
            [Required(ErrorMessage = "El campo Correo Electrónico es obligatorio.")]
            [EmailAddress(ErrorMessage = "El formato del Correo Electrónico no es válido.")]
            [Display(Name = "Correo Electrónico")]
            public string Email { get; set; }

            /// <summary>
            ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
            ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
            /// </summary>
            [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            /// <summary>
            ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
            ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la contraseña de confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario creó una nueva cuenta con contraseña.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirma tu correo electrónico",
                        $"Por favor confirma tu cuenta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>haciendo clic aquí</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Patient");
                        _logger.LogInformation($"Usuario {user.UserName} asignado al rol 'Patient' por defecto.");

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Description));
                }
            }

            // Si llegamos hasta aquí, algo falló, volver a mostrar el formulario
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"No se puede crear una instancia de '{nameof(User)}'. " +
                    $"Asegúrese de que '{nameof(User)}' no es una clase abstracta y tiene un constructor sin parámetros, o alternativamente " +
                    $"sobrescriba la página de registro en /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("La UI por defecto requiere un almacén de usuarios con soporte para correo electrónico.");
            }
            return (IUserEmailStore<User>)_userStore;
        }

        private string TranslateIdentityError(string englishError)
        {
            if (englishError.Contains("Passwords must have at least one digit"))
                return "Las contraseñas deben tener al menos un dígito ('0'-'9').";
            if (englishError.Contains("Passwords must have at least one lowercase"))
                 return "Las contraseñas deben tener al menos una minúscula ('a'-'z').";
             if (englishError.Contains("Passwords must have at least one uppercase"))
                 return "Las contraseñas deben tener al menos una mayúscula ('A'-'Z').";
            if (englishError.Contains("Passwords must have at least one non alphanumeric character"))
                 return "Las contraseñas deben tener al menos un caracter no alfanumérico.";
            if (englishError.Contains("is already taken"))
                 return "El nombre de usuario o correo ya está en uso.";
            return englishError;
        }
    }
}
