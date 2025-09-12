using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Infrastructure.Vision;

namespace Darklands.Core.Infrastructure.Configuration;

/// <summary>
/// Service registrar for Strangler Fig pattern implementations.
/// Handles feature toggle switching between legacy and new bounded contexts.
/// </summary>
public static class StranglerFigServiceRegistrar
{
    /// <summary>
    /// Registers VisionPerformanceMonitor implementations based on configuration.
    /// During TD_042: Always runs parallel operation for validation
    /// Future: Will respect toggle to switch between implementations
    /// </summary>
    public static IServiceCollection AddVisionPerformanceMonitoring(
        this IServiceCollection services,
        StranglerFigConfiguration config)
    {
        // Always register both implementations during parallel phase

        // Legacy monitor (Infrastructure context)
        services.AddSingleton<Infrastructure.Vision.VisionPerformanceMonitor>();

        // New monitor (Diagnostics context)
        services.AddSingleton<Diagnostics.Domain.Performance.IVisionPerformanceMonitor,
            Diagnostics.Infrastructure.Performance.VisionPerformanceMonitor>();

        // Contract event handler for cross-context communication
        services.AddTransient<INotificationHandler<Tactical.Contracts.ActorVisionCalculatedEvent>,
            Diagnostics.Infrastructure.Performance.ActorVisionCalculatedEventHandler>();

        // Feature toggle determines which implementation is exposed to Application layer
        if (config.UseDiagnosticsContext)
        {
            // TODO: For future phases - direct Diagnostics implementation
            // Currently not implemented as we need adapter for ActorId/EntityId conversion
            services.AddSingleton<IVisionPerformanceMonitor>(provider =>
                new VisionEventAdapter(
                    provider.GetRequiredService<Infrastructure.Vision.VisionPerformanceMonitor>(),
                    provider.GetRequiredService<IPublisher>()));
        }
        else
        {
            // Default: Legacy with event publishing for parallel validation
            services.AddSingleton<IVisionPerformanceMonitor>(provider =>
                new VisionEventAdapter(
                    provider.GetRequiredService<Infrastructure.Vision.VisionPerformanceMonitor>(),
                    provider.GetRequiredService<IPublisher>()));
        }

        return services;
    }
}
