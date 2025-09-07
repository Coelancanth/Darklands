using System;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Combat.Common
{
    /// <summary>
    /// Simple implementation of ISchedulable that wraps an actor for combat scheduling.
    /// Used by the combat scheduler to track when actors should act.
    /// 
    /// This is a lightweight wrapper that provides the required ISchedulable interface
    /// for actors to participate in the combat timeline.
    /// </summary>
    public sealed record SchedulableActor : ISchedulable
    {
        /// <summary>
        /// Unique identifier for this schedulable entity.
        /// Used for deterministic tie-breaking when NextTurn values are equal.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The absolute time when this entity will next act.
        /// Used by the combat scheduler to determine turn order.
        /// </summary>
        public TimeUnit NextTurn { get; }

        /// <summary>
        /// The current position of this entity on the combat grid.
        /// Required for grid-based combat mechanics and positioning logic.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new SchedulableActor with the specified properties.
        /// </summary>
        /// <param name="id">Unique identifier (typically from ActorId)</param>
        /// <param name="nextTurn">When the actor should next act</param>
        /// <param name="position">Current position on the grid</param>
        public SchedulableActor(Guid id, TimeUnit nextTurn, Position position)
        {
            Id = id;
            NextTurn = nextTurn;
            Position = position;
        }

        /// <summary>
        /// Creates a SchedulableActor from an ActorId and other properties.
        /// </summary>
        public static SchedulableActor Create(ActorId actorId, TimeUnit nextTurn, Position position) =>
            new(actorId.Value, nextTurn, position);
    }
}
