using Darklands.Application.Common;
using Darklands.Domain.Vision;
using Darklands.Domain.Grid;
using Darklands.Domain.Common;

namespace Darklands.Application.Vision.Queries
{
    /// <summary>
    /// Query to calculate field of view for an actor at a specific position.
    /// Returns a VisionState with currently visible and previously explored tiles.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record CalculateFOVQuery(
        ActorId ViewerId,
        Position Origin,
        VisionRange Range,
        int CurrentTurn
    ) : IQuery<VisionState>
    {
        /// <summary>
        /// Creates a new CalculateFOVQuery for an actor.
        /// </summary>
        public static CalculateFOVQuery Create(ActorId viewerId, Position origin, VisionRange range, int currentTurn) =>
            new(viewerId, origin, range, currentTurn);
    }
}
