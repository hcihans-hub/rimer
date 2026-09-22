using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the TicketHistory entity.
/// </summary>
public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        // ── Table ──────────────────────────────────────────────────
        builder.ToTable("TicketHistories");

        // ── Primary Key ────────────────────────────────────────────
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Properties ─────────────────────────────────────────────
        builder.Property(h => h.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.OldValue)
            .HasMaxLength(1000);

        builder.Property(h => h.NewValue)
            .HasMaxLength(1000);

        builder.Property(h => h.ChangedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(h => h.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(h => h.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // ── Indexes ────────────────────────────────────────────────
        builder.HasIndex(h => new { h.TicketId, h.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_TicketHistories_TicketId_CreatedAt");

        builder.HasIndex(h => h.ChangedAt)
            .HasDatabaseName("IX_TicketHistories_ChangedAt");

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.Histories)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
