// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CitasEPS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace CitasEPS.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;

        public EmailModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "El campo Nuevo correo electrónico es obligatorio.")]
            [EmailAddress(ErrorMessage = "El formato del Nuevo correo electrónico no es válido.")]
            [Display(Name = "Nuevo correo electrónico")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var emailText = await _userManager.GetEmailAsync(user);
            Email = emailText;

            Input = new InputModel
            {
                NewEmail = emailText,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != currentEmail)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);

                string userName = user.FirstName ?? Input.NewEmail; // Use user's first name or new email as fallback
                string emailSubject = "Confirma tu Cambio de Correo Electrónico en VitalCare";
                string htmlMessageBody = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{emailSubject}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; color: #333333; }}
        .email-wrapper {{ padding: 20px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 0 15px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; padding-bottom: 20px; border-bottom: 1px solid #eeeeee; }}
        .header h2 {{ color: #0d6efd; /* VitalCare primary color */ margin:0; font-size: 24px; }}
        .content {{ padding: 25px 5px; line-height: 1.6; }}
        .content p {{ margin-bottom: 18px; font-size: 16px; }}
        .button-container {{ text-align: center; margin-top: 30px; margin-bottom: 30px; }}
        .button {{
            background-color: #0d6efd; /* VitalCare primary color */
            color: #ffffff !important; 
            padding: 14px 28px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            display: inline-block;
            font-size: 16px;
        }}
        .link-alternative p {{ font-size: 0.9em; word-break: break-all; }}
        .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #eeeeee; font-size: 0.9em; color: #777777; }}
        .footer p {{ margin-bottom: 5px; }}
    </style>
</head>
<body>
    <div class=""email-wrapper"">
    <div class=""container"">
        <div class=""header"">
            <svg width=""50px"" height=""50px"" viewBox=""0 0 32 32"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" style=""fill:#0d6efd; margin-bottom:10px;"">
                 <path d=""M23.6,0c-3.4,0-6.3,2.7-7.6,5.6C14.7,2.7,11.8,0,8.4,0C3.8,0,0,3.8,0,8.4c0,9.4,9.5,11.9,16,21.2 c6.1-9.3,16-12.1,16-21.2C32,3.8,28.2,0,23.6,0z""/>
            </svg>
            <h2>VitalCare</h2>
        </div>
        <div class=""content"">
            <p>¡Hola {userName}!</p>
            <p>Has solicitado cambiar la dirección de correo electrónico asociada a tu cuenta en VitalCare a <strong>{Input.NewEmail}</strong>.</p>
            <p>Para confirmar este cambio, por favor haz clic en el siguiente botón:</p>
            <div class=""button-container"">
                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" class=""button"" style=""color: #ffffff !important;"">Confirmar Nuevo Correo</a>
            </div>
            <div class=""link-alternative"">
                <p>Si el botón no funciona, también puedes copiar y pegar el siguiente enlace en la barra de direcciones de tu navegador:</p>
                <p><a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""color: #0d6efd;"">{HtmlEncoder.Default.Encode(callbackUrl)}</a></p>
            </div>
            <p>Si no solicitaste cambiar tu correo electrónico, por favor ignora este mensaje o contacta a nuestro soporte si tienes alguna preocupación.</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} VitalCare. Todos los derechos reservados.</p>
            <p>Este es un mensaje automático, por favor no respondas directamente a este correo.</p>
        </div>
    </div>
    </div>
</body>
</html>
";
                await _emailSender.SendEmailAsync(
                    Input.NewEmail,
                    emailSubject, // Updated subject
                    htmlMessageBody); // Use the HTML body

                StatusMessage = "Enlace de confirmación para cambiar el correo electrónico enviado. Por favor, revisa tu nuevo correo electrónico.";
                return RedirectToPage();
            }

            StatusMessage = "Tu correo electrónico no ha cambiado.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var emailForVerification = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                emailForVerification,
                "Confirma tu correo electrónico",
                $"Por favor confirma tu cuenta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>haciendo clic aquí</a>.");

            StatusMessage = "Correo de verificación enviado. Por favor, revisa tu correo.";
            return RedirectToPage();
        }
    }
}
