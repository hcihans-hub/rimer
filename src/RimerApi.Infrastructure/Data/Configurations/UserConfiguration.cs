using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Enums;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for ApplicationUser (Identity table).
/// Extends the default AspNetUsers table with custom columns.
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ── Table ──────────────────────────────────────────────────
        builder.ToTable("Users");

        // ── Properties ─────────────────────────────────────────────
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(75);

        // FullName is a computed C# property — not mapped to DB
        builder.Ignore(u => u.FullName);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()  // Store enum as string in DB
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(256);

        builder.Property(u => u.RefreshTokenExpiry);

        // ── Indexes ────────────────────────────────────────────────
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
