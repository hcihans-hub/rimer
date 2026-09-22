using RimerApi.Domain.Enums;

namespace RimerApi.Domain.Entities;

/// <summary>
/// Maps a user to a specific chart they are allowed to view.
/// </summary>
public class UserChartPermission : BaseEntity
{
    /// <summary>FK to the user (ApplicationUser.Id).</summary>
    public Guid UserId { get; set; }

    /// <summary>The chart this permission grants access to.</summary>
    public ChartKey ChartKey { get; set; }
}
