using MediatR;

namespace Darklands.SharedKernel.Contracts;

/// <summary>
/// Base interface for events that cross bounded context boundaries.
/// These are integration events used for communication between contexts.
/// Contract events should only contain primitive types and EntityIds.
/// Implements INotification for MediatR pipeline integration.
/// </summary>
public interface IContractEvent : INotification
{
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// The time when this event occurred (in simulation time).
    /// </summary>
    DateTime OccurredAt { get; }
    
    /// <summary>
    /// Version of the contract for evolution support.
    /// Allows backward compatibility when contract changes.
    /// </summary>
    int Version { get; }
}