using Microsoft.Extensions.DependencyInjection;
using MediatR;
using LanguageExt;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Infrastructure.Vision;
using Darklands.Tactical.Infrastructure.Adapters;
using Darklands.Tactical.Application.Features.Combat.Attack;
using Darklands.Tactical.Application.Features.Combat.Scheduling;
using Unit = LanguageExt.Unit;

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

    /// <summary>
    /// Registers Combat implementations based on configuration.
    /// During TD_043: Always runs parallel operation for validation
    /// Future: Will respect toggle to switch between implementations
    /// </summary>
    public static IServiceCollection AddCombatServices(
        this IServiceCollection services,
        StranglerFigConfiguration config)
    {
        // Always register both implementations during parallel phase

        // Legacy handlers are already registered in GameStrapper
        // The new Tactical handlers are registered in their own namespace

        // Register the actual Tactical handlers (not the adapter)
        // NOTE: These are for the Tactical namespace commands, not Core commands
        // They're used internally by TacticalContractAdapter
        services.AddTransient<ExecuteAttackCommandHandler>();
        services.AddTransient<ProcessNextTurnCommandHandler>();

        // Register the contract adapter for parallel operation
        services.AddTransient<TacticalContractAdapter>();

        // Feature toggle determines which implementation is exposed to Application layer
        // For Phase 4, both systems run in parallel with contract events for validation
        // In the future, the toggle will switch between implementations

        // Register parallel adapter for validation when logging is enabled
        // NOTE: Currently disabled to avoid ambiguous handler registration
        // To enable: Remove legacy handler registration and use ParallelCombatAdapter instead
        if (config.EnableValidationLogging && false) // Temporarily disabled
        {
            services.AddTransient<Core.Infrastructure.Combat.ParallelCombatAdapter>();
        }

        // Register contract event handlers for cross-context monitoring
        services.AddTransient<INotificationHandler<Tactical.Contracts.AttackExecutedEvent>,
            Tactical.Infrastructure.Monitoring.AttackExecutedEventHandler>();
        services.AddTransient<INotificationHandler<Tactical.Contracts.TurnProcessedEvent>,
            Tactical.Infrastructure.Monitoring.TurnProcessedEventHandler>();

        return services;
    }
}
