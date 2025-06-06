using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Pages
{
    public class ContactModel : PageModel
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(IEmailSender emailSender, ILogger<ContactModel> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        
        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El apellido es obligatorio")]
            [Display(Name = "Apellido")]
            [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo electrónico es obligatorio")]
            [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
            [Display(Name = "Correo Electrónico")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Teléfono")]
            [Phone(ErrorMessage = "El formato del teléfono no es válido")]
            public string? Phone { get; set; }

            [Required(ErrorMessage = "El asunto es obligatorio")]
            [Display(Name = "Asunto")]
            public string Subject { get; set; } = string.Empty;

            [Required(ErrorMessage = "El mensaje es obligatorio")]
            [Display(Name = "Mensaje")]
            [StringLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
            public string Message { get; set; } = string.Empty;

            [Required(ErrorMessage = "Debe aceptar la política de privacidad")]
            [Display(Name = "Acepto la política de privacidad")]
            public bool AcceptPrivacy { get; set; }
        }

        public void OnGet()
        {
            // Método OnGet vacío - se ejecuta cuando se carga la página
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Crear el contenido del email HTML
                var emailSubject = $"Nuevo mensaje de contacto: {Input.Subject}";
                var emailBody = CreateEmailBody();

                // Enviar el email
                await _emailSender.SendEmailAsync("cramos5@udi.edu.co", emailSubject, emailBody);

                _logger.LogInformation("Email de contacto enviado exitosamente desde {Email} con asunto: {Subject}", 
                    Input.Email, Input.Subject);

                StatusMessage = "¡Mensaje enviado exitosamente! Nos comunicaremos contigo pronto.";
                
                // Limpiar el formulario después del envío exitoso
                Input = new InputModel();
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de contacto desde {Email}", Input.Email);
                
                StatusMessage = "Error: Hubo un problema al enviar tu mensaje. Por favor, intenta nuevamente o contactanos directamente por teléfono.";
                return Page();
            }
        }

        private string CreateEmailBody()
        {
            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #003366, #002244); color: white; padding: 20px; border-radius: 10px 10px 0 0; text-align: center;'>
                        <h2 style='margin: 0; font-size: 24px;'>🏥 Nuevo Mensaje de Contacto - VitalCare</h2>
                    </div>
                    
                    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <div style='border-left: 4px solid #003366; padding-left: 15px; margin-bottom: 25px;'>
                            <h3 style='color: #003366; margin: 0; font-size: 18px;'>Información del Contacto</h3>
                        </div>
                        
                        <table style='width: 100%; border-collapse: collapse; margin-bottom: 25px;'>
                            <tr>
                                <td style='padding: 10px; background-color: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold; color: #003366; width: 30%;'>👤 Nombre:</td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'>{Input.FirstName} {Input.LastName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; background-color: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold; color: #003366;'>📧 Email:</td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'><a href='mailto:{Input.Email}' style='color: #007bff; text-decoration: none;'>{Input.Email}</a></td>
                            </tr>";

            if (!string.IsNullOrEmpty(Input.Phone))
            {
                emailBody += $@"
                            <tr>
                                <td style='padding: 10px; background-color: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold; color: #003366;'>📞 Teléfono:</td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'><a href='tel:{Input.Phone}' style='color: #007bff; text-decoration: none;'>{Input.Phone}</a></td>
                            </tr>";
            }

            emailBody += $@"
                            <tr>
                                <td style='padding: 10px; background-color: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold; color: #003366;'>📝 Asunto:</td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'>{Input.Subject}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; background-color: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold; color: #003366;'>🕒 Fecha:</td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'>{DateTime.Now:dd/MM/yyyy HH:mm:ss} (Hora Colombia)</td>
                            </tr>
                        </table>
                        
                        <div style='border-left: 4px solid #22c55e; padding-left: 15px; margin-bottom: 15px;'>
                            <h3 style='color: #22c55e; margin: 0 0 10px 0; font-size: 18px;'>💬 Mensaje</h3>
                        </div>
                        
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; border: 1px solid #dee2e6; line-height: 1.6; color: #333;'>
                            {Input.Message.Replace("\n", "<br/>")}
                        </div>
                        
                        <div style='margin-top: 30px; padding: 20px; background: linear-gradient(135deg, #e3f2fd, #f3e5f5); border-radius: 8px; text-align: center;'>
                            <p style='margin: 0; color: #666; font-size: 14px;'>
                                <strong>📧 Responder directamente:</strong> 
                                <a href='mailto:{Input.Email}?subject=Re: {Input.Subject}' style='color: #007bff; text-decoration: none;'>Hacer clic para responder</a>
                            </p>";

            if (!string.IsNullOrEmpty(Input.Phone))
            {
                emailBody += $@"
                            <p style='margin: 5px 0 0 0; color: #666; font-size: 14px;'>
                                <strong>📞 Llamar:</strong> 
                                <a href='tel:{Input.Phone}' style='color: #007bff; text-decoration: none;'>{Input.Phone}</a>
                            </p>";
            }

            emailBody += @"
                        </div>
                    </div>
                    
                    <div style='text-align: center; margin-top: 20px; color: #666; font-size: 12px;'>
                        <p style='margin: 0;'>Este mensaje fue enviado desde el formulario de contacto de VitalCare</p>
                        <p style='margin: 5px 0 0 0;'>🌐 <a href='https://vitalcare.itemt.tech' style='color: #007bff; text-decoration: none;'>vitalcare.itemt.tech</a></p>
                    </div>
                </div>";

            return emailBody;
        }
    }
} 