using System;

namespace RimerApi.Domain.Interfaces;

/// <summary>
/// Interface dictating that the entity can be soft-deleted.
/// Entities implementing this will not be physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
