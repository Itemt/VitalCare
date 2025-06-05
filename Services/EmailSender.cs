using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CitasEPS.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IResend _resend;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, IResend resend, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _resend = resend;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var fromEmail = _configuration["Resend:FromEmail"];
        var apiKey = _configuration["Resend:ApiKey"];

        _logger.LogInformation($"Attempting to send email to {email} using from address: {fromEmail}");

        if (string.IsNullOrEmpty(fromEmail))
        {
            _logger.LogError("Resend From Email not configured. Cannot send email.");
            throw new InvalidOperationException("Resend From Email configuration is missing.");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Resend API Key not configured. Cannot send email.");
            throw new InvalidOperationException("Resend API Key configuration is missing.");
        }
        
        try
        {
            var message = new EmailMessage();
            message.From = fromEmail;
            message.To.Add(email);
            message.Subject = subject;
            message.HtmlBody = htmlMessage;

            _logger.LogInformation($"Calling Resend API to send email to {email}");
            var response = await _resend.EmailSendAsync(message);
            
            if (response != null)
            {
                _logger.LogInformation($"Email sent successfully to {email}. Message ID: {response}");
            }
            else
            {
                _logger.LogWarning($"Email sending may have failed to {email}. No response received.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending email to {email} via Resend: {ex.Message}");
            throw; // Re-throw to let the calling code handle it
        }
    }
} 