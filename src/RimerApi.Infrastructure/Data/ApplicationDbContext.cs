using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Identity;
using System.Reflection;

namespace RimerApi.Infrastructure.Data;

/// <summary>
/// Application database context.
/// Uses ApplicationUser as the single user entity for both Identity and business relationships.
/// Domain entities reference ApplicationUser by Guid foreign keys.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ── DbSets ─────────────────────────────────────────────────────

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<TicketTransfer> TicketTransfers => Set<TicketTransfer>();
    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
    public DbSet<TicketReminder> TicketReminders => Set<TicketReminder>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserChartPermission> UserChartPermissions => Set<UserChartPermission>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<Announcement> Announcements => Set<Announcement>();


    // ── Model Configuration ────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        builder.Entity<TicketReminder>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Note).HasMaxLength(250);

            b.HasIndex(x => new { x.IsDismissed, x.ReminderAt })
             .HasDatabaseName("IX_TicketReminders_IsDismissed_ReminderAt");
        });

        builder.Entity<Announcement>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).HasMaxLength(150).IsRequired();
            b.Property(x => x.Content).HasMaxLength(1000).IsRequired();
            
            b.HasOne(a => a.TargetDepartment)
             .WithMany()
             .HasForeignKey(a => a.TargetDepartmentId)
             .OnDelete(DeleteBehavior.SetNull);
        });
        
        // ── Apply Soft Delete Global Query Filters Dynamically ─────────
        var configureMethod = GetType().GetMethod(nameof(ConfigureSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                configureMethod!.MakeGenericMethod(entityType.ClrType).Invoke(this, new object[] { builder });
            }
        }
    }

    private void ConfigureSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : class, ISoftDeletable
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    // ── Automatic Audit Timestamps ─────────────────────────────────

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        // Domain entities (Ticket, TicketHistory, Department)
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        // TicketReminder (not a BaseEntity anymore)
        foreach (var entry in ChangeTracker.Entries<TicketReminder>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        // ApplicationUser (not a BaseEntity)
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        // Announcement (not a BaseEntity)
        foreach (var entry in ChangeTracker.Entries<Announcement>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }
}
