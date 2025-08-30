using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Grid;
using LanguageExt;

namespace Darklands.Core.Application.Grid.Queries
{
    /// <summary>
    /// Query to calculate a path between two positions on the grid.
    /// Used for UI path preview and movement cost calculation.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record CalculatePathQuery : IQuery<Seq<Position>>
    {
        /// <summary>
        /// The starting position of the path.
        /// </summary>
        public required Position FromPosition { get; init; }

        /// <summary>
        /// The target position of the path.
        /// </summary>
        public required Position ToPosition { get; init; }

        /// <summary>
        /// Creates a new CalculatePathQuery with the specified parameters.
        /// </summary>
        public static CalculatePathQuery Create(Position fromPosition, Position toPosition) =>
            new()
            {
                FromPosition = fromPosition,
                ToPosition = toPosition
            };
    }
}
