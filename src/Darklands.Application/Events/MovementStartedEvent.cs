using MediatR;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;
using System.Collections.Immutable;

namespace Darklands.Application.Events;

/// <summary>
/// Domain event published when an actor begins movement along a path.
/// Signals the start of step-by-step movement progression.
///
/// Used for:
/// - Game state management (transition to animating state)
/// - Movement animation initialization
/// - Path visualization updates
/// </summary>
/// <param name="ActorId">The ID of the actor starting movement</param>
/// <param name="StartPosition">The position where movement begins</param>
/// <param name="Path">The complete path the actor will follow</param>
public sealed record MovementStartedEvent(
    ActorId ActorId,
    Position StartPosition,
    ImmutableList<Position> Path
) : INotification
{
    /// <summary>
    /// Creates a MovementStartedEvent for the specified actor and path.
    /// </summary>
    /// <param name="actorId">The actor starting movement</param>
    /// <param name="startPosition">Position where movement begins</param>
    /// <param name="path">Complete path to follow</param>
    /// <returns>A new MovementStartedEvent</returns>
    public static MovementStartedEvent Create(ActorId actorId, Position startPosition, ImmutableList<Position> path) =>
        new(actorId, startPosition, path);

    /// <summary>
    /// Gets the destination position (last position in path).
    /// </summary>
    public Position Destination => Path.Last();

    /// <summary>
    /// Gets the total number of steps in the path.
    /// </summary>
    public int TotalSteps => Path.Count;

    public override string ToString() =>
        $"MovementStartedEvent(ActorId: {ActorId}, From: {StartPosition}, To: {Destination}, Steps: {TotalSteps})";
}
