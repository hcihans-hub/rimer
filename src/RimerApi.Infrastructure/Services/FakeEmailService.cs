using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;

namespace RimerApi.Infrastructure.Services;

public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("--- FAKE EMAIL SENT ---");
        _logger.LogInformation("To: {To}", to);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body:\n{Body}", body);
        _logger.LogInformation("-----------------------");
        
        return Task.CompletedTask;
    }
}
