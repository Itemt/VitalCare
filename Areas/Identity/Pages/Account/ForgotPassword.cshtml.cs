// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CitasEPS.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<User> userManager, 
            IEmailSender emailSender, 
            IConfiguration configuration,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

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
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    Console.WriteLine($"Password reset attempt for non-existent user or user with unconfirmed email (if check was active): {Input.Email}"); // Temporary log
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                string siteBaseUrl = _configuration["ApplicationSettings:SiteBaseUrl"];
                string callbackUrl;

                if (!string.IsNullOrEmpty(siteBaseUrl))
                {
                    var publicUri = new Uri(siteBaseUrl);
                    callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: publicUri.Scheme,
                        host: publicUri.Authority);
                }
                else
                {
                    _logger.LogWarning("ApplicationSettings:SiteBaseUrl is not configured. Email links will use the current request's host.");
                    callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);
                }

                // Updated Email HTML Template for Password Reset
                string userName = user?.FirstName ?? "Usuario"; // Use user's first name if available
                string emailSubject = "Restablece tu contraseña de VitalCare";
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
        .header h2 {{ color: #0d6efd; margin:0; font-size: 24px; }}
        .content {{ padding: 25px 5px; line-height: 1.6; }}
        .content p {{ margin-bottom: 18px; font-size: 16px; }}
        .button-container {{ text-align: center; margin-top: 30px; margin-bottom: 30px; }}
        .button {{
            background-color: #0d6efd;
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
            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en VitalCare. Si no solicitaste esto, puedes ignorar este correo de forma segura.</p>
            <p>Para restablecer tu contraseña, haz clic en el siguiente botón:</p>
            <div class=""button-container"">
                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" class=""button"" style=""color: #ffffff !important;"">Restablecer mi Contraseña</a>
            </div>
            <div class=""link-alternative"">
                <p>Si el botón no funciona, copia y pega el siguiente enlace en la barra de direcciones de tu navegador:</p>
                <p><a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""color: #0d6efd;"">{HtmlEncoder.Default.Encode(callbackUrl)}</a></p>
            </div>
            <p>Este enlace para restablecer la contraseña expirará en un tiempo limitado por razones de seguridad.</p>
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
                await _emailSender.SendEmailAsync(Input.Email, emailSubject, htmlMessageBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}




