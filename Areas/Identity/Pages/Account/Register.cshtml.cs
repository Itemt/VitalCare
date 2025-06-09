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
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using CitasEPS.Data;
using Microsoft.Extensions.Configuration;
using CitasEPS.Models.Modules.Common;
using Microsoft.EntityFrameworkCore;

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
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context,
            IConfiguration configuration,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _configuration = configuration;
            _roleManager = roleManager;
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
            [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
            [Display(Name = "Nombres")]
            [StringLength(100, ErrorMessage = "El campo Nombres no puede exceder los 100 caracteres.")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
            [Display(Name = "Apellidos")]
            [StringLength(100, ErrorMessage = "El campo Apellidos no puede exceder los 100 caracteres.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "El campo Documento de Identidad es obligatorio.")]
            [Display(Name = "Documento de Identidad")]
            [StringLength(20, ErrorMessage = "El campo Documento de Identidad no puede exceder los 20 caracteres.")]
            public string DocumentId { get; set; }

            [Required(ErrorMessage = "El campo Género es obligatorio.")]
            [Display(Name = "Género")]
            public Gender Gender { get; set; }

            /// <summary>
            ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
            ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
            /// </summary>
            [Required(ErrorMessage = "El campo Correo Electrónico es obligatorio.")]
            [EmailAddress(ErrorMessage = "El formato del Correo Electrónico no es válido.")]
            [Display(Name = "Correo Electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
            [Phone(ErrorMessage = "El formato del Teléfono no es válido.")]
            [Display(Name = "Teléfono")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "El campo Fecha de Nacimiento es obligatorio.")]
            [Display(Name = "Fecha de Nacimiento")]
            [DataType(DataType.Date)]
            [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
            public DateTime DateOfBirth { get; set; }

            /// <summary>
            ///     Esta API soporta la infraestructura UI por defecto de ASP.NET Core Identity y no está pensada para ser usada
            ///     directamente desde tu código. Esta API puede cambiar o ser eliminada en futuras versiones.
            /// </summary>
            [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 4)]
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
                // Validaciones de unicidad antes de crear el usuario
                
                // Verificar si el email ya existe
                var existingUserByEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una cuenta con este correo electrónico.");
                    return Page();
                }

                // Verificar si el número de teléfono ya existe
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == Input.PhoneNumber);
                if (existingUserByPhone != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una cuenta con este número de teléfono.");
                    return Page();
                }

                // Verificar si el documento de identidad ya existe
                var existingUserByDocument = await _context.Users
                    .FirstOrDefaultAsync(u => u.DocumentId == Input.DocumentId);
                if (existingUserByDocument != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una cuenta con este documento de identidad.");
                    return Page();
                }

                var user = CreateUser();

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.PhoneNumber = Input.PhoneNumber;
                user.DateOfBirth = new DateTime(Input.DateOfBirth.Year, Input.DateOfBirth.Month, Input.DateOfBirth.Day, 0, 0, 0, DateTimeKind.Utc);
                user.DocumentId = Input.DocumentId;
                user.Gender = Input.Gender;
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario creó una nueva cuenta con contraseña.");

                    // Ensure "Paciente" role exists and assign it to the user
                    string roleName = "Paciente";
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        _logger.LogInformation($"El rol '{roleName}' no existe. Creándolo ahora.");
                        await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    }
                    await _userManager.AddToRoleAsync(user, roleName);
                    _logger.LogInformation($"Usuario {user.Email} asignado al rol '{roleName}'.");

                    // Create and save the associated Patient record
                    var patient = new Patient
                    {
                        UserId = user.Id, // Link to the newly created User
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        Email = Input.Email,
                        PhoneNumber = Input.PhoneNumber,
                        DateOfBirth = user.DateOfBirth, // Use the same UTC date
                        DocumentId = Input.DocumentId,
                        Gender = Input.Gender
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync(); // Save the new Patient to the database

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    string siteBaseUrl = _configuration["ApplicationSettings:SiteBaseUrl"];
                    string callbackUrl;

                    if (!string.IsNullOrEmpty(siteBaseUrl))
                    {
                        var publicUri = new Uri(siteBaseUrl);
                        callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: publicUri.Scheme,
                            host: publicUri.Authority);
                    }
                    else
                    {
                        // Fallback to original behavior if SiteBaseUrl is not configured
                        // This helps prevent errors if the configuration is missed, but logs a warning.
                        _logger.LogWarning("ApplicationSettings:SiteBaseUrl is not configured. Email links will use the current request's host.");
                        callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: Request.Scheme);
                    }

                    // Updated Email HTML Template
                    string userName = user.FirstName; // Or Input.FirstName
                    string emailSubject = "Confirma tu correo electrónico en VitalCare";
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
            background-color: #0d6efd; /* Primary button color */
            color: #ffffff !important; /* Ensure text is white */
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
            <p>Gracias por registrarte en VitalCare. Para completar tu registro y activar tu cuenta, por favor confirma tu dirección de correo electrónico haciendo clic en el siguiente botón:</p>
            <div class=""button-container"">
                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" class=""button"" style=""color: #ffffff !important;"">Confirmar mi Correo Electrónico</a>
            </div>
            <div class=""link-alternative"">
                <p>Si el botón no funciona, también puedes copiar y pegar el siguiente enlace en la barra de direcciones de tu navegador:</p>
                <p><a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""color: #0d6efd;"">{HtmlEncoder.Default.Encode(callbackUrl)}</a></p>
            </div>
            <p>Si no te registraste en VitalCare o crees que esto es un error, por favor ignora este correo electrónico.</p>
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

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
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
                return "Las contraseñas ya no requieren dígitos.";
            if (englishError.Contains("Passwords must have at least one lowercase"))
                 return "Las contraseñas ya no requieren minúsculas.";
             if (englishError.Contains("Passwords must have at least one uppercase"))
                 return "Las contraseñas ya no requieren mayúsculas.";
            if (englishError.Contains("Passwords must have at least one non alphanumeric character"))
                 return "Las contraseñas ya no requieren caracteres especiales.";
            if (englishError.Contains("is already taken"))
                 return "El nombre de usuario o correo ya está en uso.";
            return englishError;
        }
    }
}





