using System;

namespace RimerApi.Domain.Entities;

/// <summary>
/// Represents an audit log entry for keeping track of entity changes (Create, Update, Delete) globally.
/// </summary>
public class AuditLog : BaseEntity
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    
    /// <summary>Original values stored as JSON</summary>
    public string OldValues { get; set; } = string.Empty;
    
    /// <summary>New values stored as JSON</summary>
    public string NewValues { get; set; } = string.Empty;
    
    /// <summary>Id of the user who initiated the change (nullable if System).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Status of the transaction (Attempted, Success, Failed).</summary>
    public string Status { get; set; } = "Success";

    /// <summary>Any recorded error if the transaction failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Unique identifier tying all records from the same EF Core transaction cycle.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Strict sequence order index within the exact transaction block.</summary>
    public int Sequence { get; set; }

    /// <summary>Absolute timestamp marking generation precision.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Flag to prevent cleanup race conditions during inflight processing.</summary>
    public bool Processing { get; set; } = true;

    /// <summary>Indicates if the record was securely offloaded to cold storage (e.g., S3/Blob).</summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>Timestamp marking when the log was physically archived.</summary>
    public DateTime? ArchivedAt { get; set; }
}
