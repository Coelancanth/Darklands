using Darklands.Application.Common;
using Darklands.Domain.Grid;

namespace Darklands.Application.Grid.Queries
{
    /// <summary>
    /// Query to validate if a movement from one position to another is legal.
    /// Used for UI feedback and move preview validation.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record ValidateMovementQuery : IQuery<bool>
    {
        /// <summary>
        /// The starting position of the movement.
        /// </summary>
        public required Position FromPosition { get; init; }

        /// <summary>
        /// The target position of the movement.
        /// </summary>
        public required Position ToPosition { get; init; }

        /// <summary>
        /// Creates a new ValidateMovementQuery with the specified parameters.
        /// </summary>
        public static ValidateMovementQuery Create(Position fromPosition, Position toPosition) =>
            new()
            {
                FromPosition = fromPosition,
                ToPosition = toPosition
            };
    }
}
