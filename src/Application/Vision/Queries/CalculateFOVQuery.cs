using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Application.Vision.Queries
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
