using System.Collections.Generic;
using MediatR;
using Darklands.Domain.Grid;

namespace Darklands.Application.FogOfWar.Events
{
    /// <summary>
    /// Application layer notification published when an actor begins progressive reveal along a movement path.
    /// Wraps the domain RevealProgressionStarted event for MediatR integration.
    ///
    /// Used for:
    /// - FOV system initialization for progressive updates
    /// - UI state transitions (show movement progress indicators)
    /// - Game state coordination (lock input during movement)
    /// </summary>
    /// <param name="ActorId">The actor starting movement progression</param>
    /// <param name="Path">Complete movement path from start to destination</param>
    /// <param name="Turn">Current game turn for event context</param>
    public sealed record RevealProgressionStartedNotification(
        ActorId ActorId,
        IReadOnlyList<Position> Path,
        int Turn
    ) : INotification
    {
        /// <summary>
        /// Creates a RevealProgressionStartedNotification for the specified actor and path.
        /// </summary>
        /// <param name="actorId">The actor starting movement</param>
        /// <param name="path">Movement path to traverse</param>
        /// <param name="turn">Current game turn</param>
        /// <returns>A new RevealProgressionStartedNotification</returns>
        public static RevealProgressionStartedNotification Create(ActorId actorId, IReadOnlyList<Position> path, int turn) =>
            new(actorId, path, turn);

        /// <summary>
        /// Gets the starting position from the path.
        /// </summary>
        public Position StartPosition => Path.Count > 0 ? Path[0] : new Position(0, 0);

        /// <summary>
        /// Gets the destination position from the path.
        /// </summary>
        public Position Destination => Path.Count > 0 ? Path[^1] : new Position(0, 0);

        /// <summary>
        /// Gets the total number of steps in the movement.
        /// </summary>
        public int StepCount => Path.Count;

        public override string ToString() =>
            $"RevealProgressionStartedNotification(ActorId: {ActorId}, Steps: {StepCount}, Turn: {Turn})";
    }
}
