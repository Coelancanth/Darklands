using System.Collections.Generic;
using Darklands.Application.Common;
using Darklands.Domain.Combat;

namespace Darklands.Application.Combat.Queries
{
    /// <summary>
    /// Query to get the current turn order from the combat scheduler.
    /// Returns a read-only list of all scheduled entities in turn order.
    /// 
    /// This query does not modify the scheduler state - it's purely for observation.
    /// Useful for UI display of turn order and debugging combat state.
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record GetSchedulerQuery : IQuery<IReadOnlyList<ISchedulable>>
    {
        /// <summary>
        /// Creates a new GetSchedulerQuery
        /// </summary>
        public static GetSchedulerQuery Create() => new();
    }
}
