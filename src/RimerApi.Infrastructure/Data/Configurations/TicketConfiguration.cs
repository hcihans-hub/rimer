using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the Ticket entity.
/// Supports both internal (authenticated) and external (public) tickets.
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        // ── Table ──────────────────────────────────────────────────
        builder.ToTable("Tickets");

        // ── Primary Key ────────────────────────────────────────────
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Properties ─────────────────────────────────────────────
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(t => t.ReferenceNo)
            .IsRequired()
            .HasMaxLength(20)
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.Property(t => t.Category)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasDefaultValue(RimerApi.Domain.Enums.TicketPriority.Normal);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // ── External ticket fields ─────────────────────────────────
        builder.Property(t => t.IsExternal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.TrackingCode)
            .HasMaxLength(20);

        builder.Property(t => t.GuestName)
            .HasMaxLength(150);

        builder.Property(t => t.GuestEmail)
            .HasMaxLength(200);

        builder.Property(t => t.GuestPhone)
            .HasMaxLength(20);

        // ── Indexes ────────────────────────────────────────────────
        builder.HasIndex(t => t.ReferenceNo)
            .IsUnique()
            .HasDatabaseName("UX_Tickets_ReferenceNo");

        builder.HasIndex(t => t.TrackingCode)
            .IsUnique()
            .HasFilter("[TrackingCode] IS NOT NULL")
            .HasDatabaseName("UX_Tickets_TrackingCode");

        builder.HasIndex(t => t.DepartmentId)
            .HasDatabaseName("IX_Tickets_DepartmentId");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Tickets_CreatedAt");

        builder.HasIndex(t => t.LastTransferredAt)
            .HasDatabaseName("IX_Tickets_LastTransferredAt");

        // Operator & Department Queues
        builder.HasIndex(t => new { t.AssignedToId, t.Status })
            .HasDatabaseName("IX_Tickets_AssignedToId_Status");

        builder.HasIndex(t => new { t.AssignedDepartmentId, t.Status })
            .HasDatabaseName("IX_Tickets_AssignedDept_Status");

        builder.HasIndex(t => new { t.AssignedDepartmentId, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Tickets_AssignedDept_CreatedAt");

        builder.HasIndex(t => new { t.DepartmentId, t.Status })
            .HasDatabaseName("IX_Tickets_DepartmentId_Status");

        // User's own tickets sorted
        builder.HasIndex(t => new { t.CreatorId, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Tickets_CreatorId_CreatedAt");

        // Dashboard & Analytics
        builder.HasIndex(t => new { t.Status, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Tickets_Status_CreatedAt");

        builder.HasIndex(t => new { t.Category, t.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Tickets_Category_CreatedAt");

        // ── Relationships ──────────────────────────────────────────
        // CreatorId is now NULLABLE — external tickets have no user account
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Department)
            .WithMany(d => d.Tickets)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.AssignedDepartment)
            .WithMany(d => d.AssignedTickets)
            .HasForeignKey(t => t.AssignedDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
