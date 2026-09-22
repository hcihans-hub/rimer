using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;

namespace RimerApi.Infrastructure.Data.Configurations;

public class UserChartPermissionConfiguration : IEntityTypeConfiguration<UserChartPermission>
{
    public void Configure(EntityTypeBuilder<UserChartPermission> builder)
    {
        builder.ToTable("UserChartPermissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();

        // Store enum as string for readability in the DB
        builder.Property(p => p.ChartKey)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(p => new { p.UserId, p.ChartKey }).IsUnique();
        builder.HasIndex(p => p.UserId);
    }
}
