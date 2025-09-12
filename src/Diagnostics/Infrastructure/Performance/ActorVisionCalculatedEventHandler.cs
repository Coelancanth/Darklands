using MediatR;
using Microsoft.Extensions.Logging;
using Darklands.Tactical.Contracts;
using Darklands.Diagnostics.Domain.Performance;

namespace Darklands.Diagnostics.Infrastructure.Performance;

/// <summary>
/// Handles ActorVisionCalculatedEvent from Tactical context.
/// Converts contract event data and forwards to Diagnostics VisionPerformanceMonitor.
/// Part of Strangler Fig migration - enables parallel operation between old and new systems.
/// </summary>
public sealed class ActorVisionCalculatedEventHandler : MediatR.INotificationHandler<ActorVisionCalculatedEvent>
{
    private readonly IVisionPerformanceMonitor _diagnosticsMonitor;
    private readonly ILogger<ActorVisionCalculatedEventHandler> _logger;

    public ActorVisionCalculatedEventHandler(
        IVisionPerformanceMonitor diagnosticsMonitor,
        ILogger<ActorVisionCalculatedEventHandler> logger)
    {
        _diagnosticsMonitor = diagnosticsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Receives contract event and forwards to Diagnostics performance monitor.
    /// Converts integer ms back to double for internal processing.
    /// </summary>
    public Task Handle(ActorVisionCalculatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Convert contract event back to Diagnostics types
            _diagnosticsMonitor.RecordFOVCalculation(
                actorId: notification.ActorId,  // EntityId (SharedKernel type)
                calculationTimeMs: notification.CalculationTimeMs, // int -> double conversion
                tilesVisible: notification.TilesVisible,
                tilesChecked: notification.TilesChecked,
                wasFromCache: notification.WasFromCache
            );

            _logger.LogDebug("Processed vision event for Actor {ActorId}: {TimeMs}ms, {Visible} visible",
                notification.ActorId.Value.ToString()[..8],
                notification.CalculationTimeMs,
                notification.TilesVisible);
        }
        catch (Exception ex)
        {
            // Log but don't fail - diagnostics should not break tactical operations
            _logger.LogWarning(ex, "Failed to process vision event for Actor {ActorId}",
                notification.ActorId.Value.ToString()[..8]);
        }

        return Task.CompletedTask;
    }
}
