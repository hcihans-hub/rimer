using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RimerApi.Application.Interfaces;
using RimerApi.Application.Services;

namespace RimerApi.Application;

/// <summary>
/// Extension methods for registering Application layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application services: AutoMapper, FluentValidation, and business services.
    /// Call from Program.cs: <code>builder.Services.AddApplication();</code>
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper — scan this assembly for Profile subclasses
        services.AddAutoMapper(assembly);

        // FluentValidation — scan this assembly for AbstractValidator<T> subclasses
        services.AddValidatorsFromAssembly(assembly);

        // MediatR — registers IPublisher, INotificationHandler, etc.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Business services
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();

        return services;
    }
}
