namespace RimerApi.Domain.Enums;

/// <summary>
/// Classifies a user's identity type from the external identity service.
/// This is synced during login and used for analytics.
/// </summary>
public enum PersonType
{
    /// <summary>Academic staff (faculty, researchers).</summary>
    Academic = 0,

    /// <summary>Administrative/clerical staff.</summary>
    Administrative = 1,

    /// <summary>Blue-collar / maintenance workers.</summary>
    Worker = 2,

    /// <summary>Enrolled student.</summary>
    Student = 3,

    /// <summary>Other / external / unclassified.</summary>
    Other = 4
}
