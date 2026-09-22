namespace RimerApi.Domain.Enums;

/// <summary>
/// Categorizes the type of communication a ticket represents.
/// </summary>
public enum TicketCategory
{
    /// <summary>A complaint about a service or experience.</summary>
    Complaint = 0,

    /// <summary>A suggestion for improvement.</summary>
    Suggestion = 1,

    /// <summary>A formal request for action or information.</summary>
    Request = 2,

    /// <summary>An expression of thanks or appreciation.</summary>
    Thanks = 3,

    /// <summary>A question or query.</summary>
    Question = 4,

    /// <summary>A formal information request.</summary>
    InfoRequest = 5
}
