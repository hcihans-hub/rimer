using System;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Domain.Entities;

/// <summary>
/// Base entity providing common audit properties for all domain entities.
/// </summary>
public abstract class BaseEntity : ISoftDeletable
{
    /// <summary>Unique identifier for the entity.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Who created the entity.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Whether the entity is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Indicates if the entity is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the entity was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }
}
