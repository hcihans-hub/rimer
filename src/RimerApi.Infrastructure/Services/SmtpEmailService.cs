using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;

namespace RimerApi.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        
        var host = emailSettings["SmtpHost"];
        var port = int.Parse(emailSettings["SmtpPort"] ?? "587");
        var user = emailSettings["SmtpUser"];
        var pass = emailSettings["SmtpPass"];
        var fromEmail = emailSettings["FromEmail"] ?? "noreply@rimer.com";
        var fromName = emailSettings["FromName"] ?? "RİMER Destek";

        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("SMTP Host is not configured. Email will not be sent.");
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to {To}", to);
            // Don't throw to prevent blocking the user flow. Just log it.
        }
    }
}
