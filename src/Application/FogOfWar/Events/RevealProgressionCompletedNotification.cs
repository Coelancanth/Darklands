using MediatR;
using Darklands.Domain.Grid;

namespace Darklands.Application.FogOfWar.Events
{
    /// <summary>
    /// Application layer notification published when an actor completes their reveal progression.
    /// Wraps the domain RevealProgressionCompleted event for MediatR integration.
    ///
    /// Used for:
    /// - FOV system finalization (complete reveal at destination)
    /// - Game state transitions (return to normal input handling)
    /// - UI cleanup (hide movement progress indicators)
    /// - Resource cleanup (free movement progression memory)
    /// </summary>
    /// <param name="ActorId">The actor who completed movement</param>
    /// <param name="FinalPosition">The final destination position reached</param>
    /// <param name="Turn">Current game turn for event context</param>
    public sealed record RevealProgressionCompletedNotification(
        ActorId ActorId,
        Position FinalPosition,
        int Turn
    ) : INotification
    {
        /// <summary>
        /// Creates a RevealProgressionCompletedNotification for the specified completion.
        /// </summary>
        /// <param name="actorId">The actor that completed movement</param>
        /// <param name="finalPosition">Final destination position</param>
        /// <param name="turn">Current game turn</param>
        /// <returns>A new RevealProgressionCompletedNotification</returns>
        public static RevealProgressionCompletedNotification Create(
            ActorId actorId,
            Position finalPosition,
            int turn) =>
            new(actorId, finalPosition, turn);

        public override string ToString() =>
            $"RevealProgressionCompletedNotification(ActorId: {ActorId}, FinalPosition: {FinalPosition}, Turn: {Turn})";
    }
}
