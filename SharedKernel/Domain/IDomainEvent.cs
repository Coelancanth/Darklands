namespace Darklands.SharedKernel.Domain;

/// <summary>
/// Marker interface for events that occur within a bounded context's domain.
/// These are internal events that stay within a single bounded context.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The time when this event occurred (in simulation time, not wall-clock time).
    /// </summary>
    DateTime OccurredAt { get; }
}