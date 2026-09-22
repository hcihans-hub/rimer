using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the TicketReply entity.
/// </summary>
public class TicketReplyConfiguration : IEntityTypeConfiguration<TicketReply>
{
    public void Configure(EntityTypeBuilder<TicketReply> builder)
    {
        builder.ToTable("TicketReplies");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // ── Indexes ────────────────────────────────────────────────
        builder.HasIndex(r => new { r.TicketId, r.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_TicketReplies_TicketId_CreatedAt");

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(r => r.Ticket)
            .WithMany(t => t.Replies)
            .HasForeignKey(r => r.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
