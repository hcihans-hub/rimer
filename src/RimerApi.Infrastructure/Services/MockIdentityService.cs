using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Enums;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Mock identity provider for development.
/// Returns simulated identity data based on email domain/prefix.
/// Replace with LDAP/SSO/e-Devlet implementation in production.
/// </summary>
public class MockIdentityService : IIdentityService
{
    private readonly ILogger<MockIdentityService> _logger;

    public MockIdentityService(ILogger<MockIdentityService> logger)
    {
        _logger = logger;
    }

    public Task<ExternalIdentityInfo?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("[MOCK IDENTITY] Looking up user by email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<ExternalIdentityInfo?>(null);

        // Simulate identity lookup based on email patterns
        var personType = email.ToLower() switch
        {
            var e when e.Contains("admin") => PersonType.Administrative,
            var e when e.Contains("staff") => PersonType.Administrative,
            var e when e.Contains("prof") || e.Contains("dr") || e.Contains("academic") => PersonType.Academic,
            var e when e.Contains("worker") || e.Contains("tech") => PersonType.Worker,
            var e when e.Contains("student") || e.Contains("stu") => PersonType.Student,
            _ => PersonType.Other
        };

        var info = new ExternalIdentityInfo
        {
            ExternalId = $"EXT-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            FullName = email.Split('@')[0].Replace('.', ' ').Replace('_', ' '),
            Email = email,
            PersonType = personType,
            Department = null // Would come from real identity service
        };

        _logger.LogInformation("[MOCK IDENTITY] Resolved: {FullName} → {PersonType}", info.FullName, info.PersonType);
        return Task.FromResult<ExternalIdentityInfo?>(info);
    }

    public Task<ExternalIdentityInfo?> GetByExternalIdAsync(string externalId)
    {
        _logger.LogInformation("[MOCK IDENTITY] Looking up user by external ID: {Id}", externalId);
        return Task.FromResult<ExternalIdentityInfo?>(null);
    }
}
