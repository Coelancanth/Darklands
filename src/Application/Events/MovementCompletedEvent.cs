using MediatR;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;

namespace Darklands.Application.Events;

/// <summary>
/// Domain event published when an actor completes their movement path.
/// Signals the end of movement progression and return to ready state.
///
/// Used for:
/// - Game state management (transition back to ready state)
/// - Scheduler notification (actor action complete)
/// - Final animation cleanup
/// - Turn progression logic
/// </summary>
/// <param name="ActorId">The ID of the actor that completed movement</param>
/// <param name="FinalPosition">The final position where movement ended</param>
public sealed record MovementCompletedEvent(
    ActorId ActorId,
    Position FinalPosition
) : INotification
{
    /// <summary>
    /// Creates a MovementCompletedEvent for the specified actor.
    /// </summary>
    /// <param name="actorId">The actor that completed movement</param>
    /// <param name="finalPosition">The final position reached</param>
    /// <returns>A new MovementCompletedEvent</returns>
    public static MovementCompletedEvent Create(ActorId actorId, Position finalPosition) =>
        new(actorId, finalPosition);

    public override string ToString() =>
        $"MovementCompletedEvent(ActorId: {ActorId}, FinalPosition: {FinalPosition})";
}
