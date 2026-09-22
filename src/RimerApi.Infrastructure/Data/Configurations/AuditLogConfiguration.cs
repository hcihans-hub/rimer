using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;

namespace RimerApi.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        
        // Sequential GUID for index health (Fragmentation prevention)
        builder.Property(a => a.Id)
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Max limits for safe strings
        builder.Property(a => a.TableName).HasMaxLength(150);
        builder.Property(a => a.RecordId).HasMaxLength(150);
        builder.Property(a => a.Action).HasMaxLength(20);
        builder.Property(a => a.Status).HasMaxLength(20);
        builder.Property(a => a.ErrorMessage).HasMaxLength(300);

        // Required index for Retention Job cleanup
        builder.HasIndex(a => a.CreatedAt);

        // Composite index for Recovery Job & Reporting (Senior Architecture Optimization)
        builder.HasIndex(a => new { a.Status, a.CreatedAt })
               .HasDatabaseName("IX_AuditLogs_Status_CreatedAt");

        // Idempotency guarantee index (Strictly per event correlation)
        builder.HasIndex(a => a.CorrelationId)
               .IsUnique();
    }
}
