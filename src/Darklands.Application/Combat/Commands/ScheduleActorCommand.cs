using System;
using Darklands.Application.Common;
using Darklands.Domain.Combat;
using Darklands.Domain.Grid;

namespace Darklands.Application.Combat.Commands
{
    /// <summary>
    /// Command to schedule an actor to act at a specific time in the combat timeline.
    /// Used to add actors to the combat scheduler with their next turn time.
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record ScheduleActorCommand : ICommand
    {
        /// <summary>
        /// The unique identifier of the actor to schedule
        /// </summary>
        public required ActorId ActorId { get; init; }

        /// <summary>
        /// The position of the actor on the combat grid
        /// </summary>
        public required Position Position { get; init; }

        /// <summary>
        /// The time when the actor should next act
        /// </summary>
        public required TimeUnit NextTurn { get; init; }

        /// <summary>
        /// Creates a new ScheduleActorCommand with the specified parameters
        /// </summary>
        public static ScheduleActorCommand Create(ActorId actorId, Position position, TimeUnit nextTurn) =>
            new()
            {
                ActorId = actorId,
                Position = position,
                NextTurn = nextTurn
            };
    }
}
