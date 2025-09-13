using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Tactical.Contracts;

namespace Darklands.Tactical.Infrastructure.Monitoring;

/// <summary>
/// Handles TurnProcessedEvent for monitoring and validation during parallel operation.
/// Part of TD_043 Strangler Fig migration - compares legacy vs new Tactical implementation.
/// </summary>
public sealed class TurnProcessedEventHandler : INotificationHandler<TurnProcessedEvent>
{
    public TurnProcessedEventHandler()
    {
    }

    public Task Handle(TurnProcessedEvent notification, CancellationToken cancellationToken)
    {
        // TODO: Log the event for validation and comparison
        // For now, just capturing the events for parallel operation validation

        // In production, this could:
        // - Store metrics for turn timing validation
        // - Compare scheduler behavior between systems
        // - Detect any discrepancies in turn order

        return Task.CompletedTask;
    }
}