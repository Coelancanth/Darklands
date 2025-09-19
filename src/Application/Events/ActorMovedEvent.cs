using MediatR;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;
using System.Collections.Immutable;

namespace Darklands.Application.Events;

/// <summary>
/// Domain event published when an actor moves one step along their path.
/// This event represents the truth of step-by-step movement progression.
///
/// Used for:
/// - Position updates in GridStateService
/// - FOV calculation triggers
/// - Step-by-step animation coordination
/// - Movement progress tracking
/// </summary>
/// <param name="ActorId">The ID of the actor that moved</param>
/// <param name="FromPosition">The position the actor moved from</param>
/// <param name="ToPosition">The position the actor moved to</param>
/// <param name="RemainingPath">Remaining steps in the movement path</param>
public sealed record ActorMovedEvent(
    ActorId ActorId,
    Position FromPosition,
    Position ToPosition,
    ImmutableList<Position> RemainingPath
) : INotification
{
    /// <summary>
    /// Creates an ActorMovedEvent for a single step of movement.
    /// </summary>
    /// <param name="actorId">The actor that moved</param>
    /// <param name="fromPosition">Position moved from</param>
    /// <param name="toPosition">Position moved to</param>
    /// <param name="remainingPath">Remaining steps in path</param>
    /// <returns>A new ActorMovedEvent</returns>
    public static ActorMovedEvent Create(ActorId actorId, Position fromPosition, Position toPosition, ImmutableList<Position> remainingPath) =>
        new(actorId, fromPosition, toPosition, remainingPath);

    /// <summary>
    /// Indicates if this is the final step in the movement.
    /// </summary>
    public bool IsMovementComplete => RemainingPath.IsEmpty;

    /// <summary>
    /// Gets the number of steps remaining after this move.
    /// </summary>
    public int StepsRemaining => RemainingPath.Count;

    public override string ToString() =>
        $"ActorMovedEvent(ActorId: {ActorId}, {FromPosition} → {ToPosition}, Remaining: {StepsRemaining})";
}
