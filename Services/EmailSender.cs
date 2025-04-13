using Microsoft.AspNetCore.Identity.UI.Services;

namespace CitasEPS.Services;

public class EmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // TODO: Implement actual email sending logic here.
        // For now, just log to console or do nothing.
        Console.WriteLine($"Email to: {email}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine($"Message: {htmlMessage}");
        return Task.CompletedTask;
    }
} 