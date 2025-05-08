using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Resend;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CitasEPS.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IResend _resend;

    public EmailSender(IConfiguration configuration, IResend resend)
    {
        _configuration = configuration;
        _resend = resend;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var fromEmail = _configuration["Resend:FromEmail"];

        if (string.IsNullOrEmpty(fromEmail))
        {
            Console.WriteLine("Resend From Email not configured. Falling back to console output.");
            Console.WriteLine($"Email to: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");
            return;
        }
        
        try
        {
            var message = new EmailMessage();
            message.From = fromEmail;
            message.To.Add(email);
            message.Subject = subject;
            message.HtmlBody = htmlMessage;

            await _resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email with Resend: {ex.Message}");
        }
    }
} 