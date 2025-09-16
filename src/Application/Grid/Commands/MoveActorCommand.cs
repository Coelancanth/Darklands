using Darklands.Application.Common;
using Darklands.Domain.Grid;

namespace Darklands.Application.Grid.Commands
{
    /// <summary>
    /// Command to move an actor from its current position to a new position on the grid.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record MoveActorCommand : ICommand
    {
        /// <summary>
        /// The unique identifier of the actor to move.
        /// </summary>
        public required ActorId ActorId { get; init; }

        /// <summary>
        /// The target position where the actor should be moved.
        /// Note: FromPosition is intentionally omitted as grid state service is the source of truth.
        /// </summary>
        public required Position ToPosition { get; init; }

        /// <summary>
        /// Creates a new MoveActorCommand with the specified parameters.
        /// </summary>
        public static MoveActorCommand Create(ActorId actorId, Position toPosition) =>
            new()
            {
                ActorId = actorId,
                ToPosition = toPosition
            };
    }
}
