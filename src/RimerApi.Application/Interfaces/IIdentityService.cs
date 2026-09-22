using RimerApi.Domain.Enums;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Represents identity data retrieved from the external identity service.
/// </summary>
public class ExternalIdentityInfo
{
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PersonType PersonType { get; set; } = PersonType.Other;
    public string? Department { get; set; }
}

/// <summary>
/// Abstracts the external identity provider.
/// Implementations: Mock (dev), LDAP, SSO, e-Devlet, university API.
/// </summary>
public interface IIdentityService
{
    /// <summary>Look up a user by email from the external identity system.</summary>
    Task<ExternalIdentityInfo?> GetByEmailAsync(string email);

    /// <summary>Look up a user by external ID.</summary>
    Task<ExternalIdentityInfo?> GetByExternalIdAsync(string externalId);
}
