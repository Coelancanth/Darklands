namespace Darklands.Core.Features.Combat.Domain.Events;

/// <summary>
/// Type of change that occurred in the turn queue.
/// Used by TurnQueueChangedEvent to communicate what happened.
/// </summary>
public enum TurnQueueChangeType
{
    /// <summary>
    /// An actor was added to the queue (enemy detection, reinforcements).
    /// </summary>
    ActorScheduled,

    /// <summary>
    /// An actor was removed from the queue (defeated, fled, incapacitated).
    /// </summary>
    ActorRemoved
}
