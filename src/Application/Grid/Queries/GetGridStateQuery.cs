using Darklands.Core.Application.Common;

namespace Darklands.Core.Application.Grid.Queries
{
    /// <summary>
    /// Query to retrieve the current grid state snapshot.
    /// Used for UI presentation and game state validation.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record GetGridStateQuery : IQuery<Domain.Grid.Grid>
    {
        /// <summary>
        /// Creates a new GetGridStateQuery instance.
        /// </summary>
        public static GetGridStateQuery Create() => new();
    }
}
