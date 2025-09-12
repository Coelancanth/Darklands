namespace Darklands.SharedKernel.Contracts;

/// <summary>
/// Marker interface for events that cross bounded context boundaries.
/// These are integration events used for communication between contexts.
/// Contract events should only contain primitive types and EntityIds.
/// </summary>
public interface IContractEvent
{
    /// <summary>
    /// The time when this event occurred (in simulation time).
    /// </summary>
    DateTime OccurredAt { get; }
    
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// </summary>
    Guid EventId { get; }
}