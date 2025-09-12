using LanguageExt;
using MediatR;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Infrastructure.Vision;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Contracts;

namespace Darklands.Core.Infrastructure.Vision;

/// <summary>
/// TEMPORARY adapter for Strangler Fig migration (TD_042).
/// Wraps the existing VisionPerformanceMonitor and publishes contract events
/// to enable parallel operation between old and new Diagnostics context.
/// Will be removed in TD_045 when migration is complete.
/// </summary>
public sealed class VisionEventAdapter : IVisionPerformanceMonitor
{
    private readonly VisionPerformanceMonitor _legacyMonitor;
    private readonly IPublisher _publisher;

    public VisionEventAdapter(VisionPerformanceMonitor legacyMonitor, IPublisher publisher)
    {
        _legacyMonitor = legacyMonitor;
        _publisher = publisher;
    }

    /// <summary>
    /// Records FOV calculation in legacy monitor AND publishes contract event for new Diagnostics context.
    /// </summary>
    public void RecordFOVCalculation(ActorId actorId, double calculationTimeMs, int tilesVisible, int tilesChecked, bool wasFromCache)
    {
        // Call existing legacy monitor (unchanged behavior)
        _legacyMonitor.RecordFOVCalculation(actorId, calculationTimeMs, tilesVisible, tilesChecked, wasFromCache);

        // Publish contract event for new Diagnostics context
        // Convert ActorId to EntityId and double to int for cross-context compatibility
        var contractEvent = ActorVisionCalculatedEvent.Create(
            actorId: new EntityId(actorId.Value), // Convert ActorId.Value (Guid) to EntityId
            tilesVisible: tilesVisible,
            calculationTimeMs: (int)Math.Round(calculationTimeMs), // Convert double to int
            tilesChecked: tilesChecked,
            wasFromCache: wasFromCache
        );

        // Publish asynchronously (fire-and-forget for performance)
        _ = Task.Run(async () =>
        {
            try
            {
                await _publisher.Publish(contractEvent);
            }
            catch
            {
                // Swallow exceptions to avoid breaking existing functionality
                // New diagnostics system is optional and shouldn't break tactical operations
            }
        });
    }

    /// <summary>
    /// Delegates to legacy monitor.
    /// </summary>
    public Fin<VisionPerformanceReport> GetPerformanceReport() => _legacyMonitor.GetPerformanceReport();

    /// <summary>
    /// Delegates to legacy monitor.
    /// </summary>
    public void Reset() => _legacyMonitor.Reset();
}
