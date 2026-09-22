using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;

namespace RimerApi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the TicketTransfer entity.
/// </summary>
public class TicketTransferConfiguration : IEntityTypeConfiguration<TicketTransfer>
{
    public void Configure(EntityTypeBuilder<TicketTransfer> builder)
    {
        builder.ToTable("TicketTransfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Note)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // ── Indexes ────────────────────────────────────────────────
        builder.HasIndex(t => t.TicketId)
            .HasDatabaseName("IX_TicketTransfers_TicketId");

        builder.HasIndex(t => t.FromDepartmentId)
            .HasDatabaseName("IX_TicketTransfers_FromDepartmentId");

        builder.HasIndex(t => t.ToDepartmentId)
            .HasDatabaseName("IX_TicketTransfers_ToDepartmentId");

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(t => t.Ticket)
            .WithMany(t => t.Transfers)
            .HasForeignKey(t => t.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.FromDepartment)
            .WithMany()
            .HasForeignKey(t => t.FromDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToDepartment)
            .WithMany()
            .HasForeignKey(t => t.ToDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
