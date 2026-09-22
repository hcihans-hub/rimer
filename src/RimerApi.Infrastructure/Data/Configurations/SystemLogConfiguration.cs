using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RimerApi.Domain.Entities;

namespace RimerApi.Infrastructure.Data.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Entity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.UserId)
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);
            
        builder.HasIndex(x => x.Timestamp);
    }
}
