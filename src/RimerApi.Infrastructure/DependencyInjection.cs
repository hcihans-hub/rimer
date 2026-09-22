using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;
using RimerApi.Infrastructure.Identity;
using RimerApi.Infrastructure.Repositories;
using RimerApi.Infrastructure.Services;
using RimerApi.Infrastructure.WebSockets;

namespace RimerApi.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services: DbContext, Identity, and Repositories.
    /// Call from Program.cs: <code>builder.Services.AddInfrastructure(builder.Configuration);</code>
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RimerApi.Application.Common.AuditOptions>(
            configuration.GetSection("AuditOptions"));

        services.AddHttpContextAccessor(); // Required for AuditInterceptor

        // ── Interceptors ───────────────────────────────────────────────
        services.AddSingleton<RimerApi.Infrastructure.Data.Interceptors.SoftDeleteInterceptor>();
        services.AddScoped<RimerApi.Infrastructure.Data.Interceptors.AuditInterceptor>();

        // ── Database ───────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var softDeleteInterceptor = sp.GetRequiredService<RimerApi.Infrastructure.Data.Interceptors.SoftDeleteInterceptor>();
            var auditInterceptor = sp.GetRequiredService<RimerApi.Infrastructure.Data.Interceptors.AuditInterceptor>();

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
            
            // Note: Order matters! Build SoftDelete (Deleted -> Modified) then AuditLog sees Modified+IsDeleted -> DELETE
            options.AddInterceptors(softDeleteInterceptor, auditInterceptor);
        });

        // ── ASP.NET Identity ───────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Password policy
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;

                // Lockout policy
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ── Repositories ───────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // ── Application abstractions implemented here ──────────────
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IChartPermissionService, RimerApi.Application.Services.ChartPermissionService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IBackgroundJobService, HangfireJobService>();
        services.AddScoped<IEventBackgroundWorker, EventBackgroundWorker>();
        services.AddScoped<IAuditLogQueueService, AuditLogQueueService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IAuditArchiveProvider, LocalGzipAuditArchiveProvider>();
        services.AddScoped<IIdentityService, MockIdentityService>();
        services.AddScoped<RimerApi.Application.Services.PublicTicketService>();

        // ── WebSocket Services ─────────────────────────────────────
        services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        services.AddSingleton<IMetricsProvider, MockMetricsProvider>();

        return services;
    }
}
