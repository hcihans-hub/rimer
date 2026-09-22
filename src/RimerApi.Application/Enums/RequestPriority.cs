namespace RimerApi.Application.Enums;

/// <summary>
/// Defines the priority of an API request for Adaptive Load Shedding.
/// </summary>
public enum RequestPriority
{
    /// <summary>
    /// Critical operations that must never be dropped (e.g. Authentication, critical payment processing).
    /// </summary>
    Critical = 0,

    /// <summary>
    /// Normal data mutations and essential reads. (Default)
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Non-essential operations (e.g. bulk queries, reporting, heavy exports) that can be dropped under system pressure.
    /// </summary>
    Low = 2
}
