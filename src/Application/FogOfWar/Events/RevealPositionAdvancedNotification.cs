using MediatR;
using Darklands.Domain.Grid;

namespace Darklands.Application.FogOfWar.Events
{
    /// <summary>
    /// Application layer notification published when an actor's reveal position advances to the next step.
    /// Wraps the domain RevealPositionAdvanced event for MediatR integration.
    ///
    /// Used for:
    /// - Progressive FOV updates as actor moves cell-by-cell
    /// - Visual position synchronization (teleport sprite to logical position)
    /// - Audio/visual feedback for each step
    /// - Game state validation (position conflicts, collision detection)
    /// </summary>
    /// <param name="ActorId">The actor whose position advanced</param>
    /// <param name="NewRevealPosition">The new logical position for FOV calculation</param>
    /// <param name="PreviousPosition">The previous position before advancement</param>
    /// <param name="Turn">Current game turn for event context</param>
    public sealed record RevealPositionAdvancedNotification(
        ActorId ActorId,
        Position NewRevealPosition,
        Position PreviousPosition,
        int Turn
    ) : INotification
    {
        /// <summary>
        /// Creates a RevealPositionAdvancedNotification for the specified position advancement.
        /// </summary>
        /// <param name="actorId">The actor that moved</param>
        /// <param name="newPosition">New logical position</param>
        /// <param name="previousPosition">Previous position</param>
        /// <param name="turn">Current game turn</param>
        /// <returns>A new RevealPositionAdvancedNotification</returns>
        public static RevealPositionAdvancedNotification Create(
            ActorId actorId,
            Position newPosition,
            Position previousPosition,
            int turn) =>
            new(actorId, newPosition, previousPosition, turn);

        /// <summary>
        /// Gets the movement vector from previous to new position.
        /// </summary>
        public Position MovementVector => new(
            NewRevealPosition.X - PreviousPosition.X,
            NewRevealPosition.Y - PreviousPosition.Y);

        /// <summary>
        /// Gets the Manhattan distance moved in this step.
        /// </summary>
        public int StepDistance => Math.Abs(MovementVector.X) + Math.Abs(MovementVector.Y);

        /// <summary>
        /// Checks if this was a diagonal movement step.
        /// </summary>
        public bool IsDiagonalMove => MovementVector.X != 0 && MovementVector.Y != 0;

        public override string ToString() =>
            $"RevealPositionAdvancedNotification(ActorId: {ActorId}, {PreviousPosition} → {NewRevealPosition}, Turn: {Turn})";
    }
}
