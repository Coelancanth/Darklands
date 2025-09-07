using System;
using System.Collections.Generic;
using Darklands.Core.Domain.Combat;

namespace Darklands.Core.Application.Combat.Common
{
    /// <summary>
    /// Comparer for ISchedulable entities that provides deterministic ordering.
    /// Primary sort: NextTurn (earlier times first)
    /// Tie-breaker: Guid Id (lexicographically for determinism)
    /// 
    /// This ensures consistent turn order across runs and prevents non-deterministic
    /// behavior when multiple entities have the same NextTurn time.
    /// </summary>
    public class TimeComparer : IComparer<ISchedulable>
    {
        /// <summary>
        /// Singleton instance for performance and consistency
        /// </summary>
        public static readonly TimeComparer Instance = new();

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private TimeComparer() { }

        /// <summary>
        /// Compares two ISchedulable entities for ordering in the combat timeline.
        /// 
        /// Comparison logic:
        /// 1. Primary: Compare NextTurn times (earlier = higher priority)
        /// 2. Tie-breaker: Compare Guid Ids lexicographically
        /// 
        /// Returns:
        /// - Negative: x comes before y (x has higher priority)
        /// - Zero: Never returned due to Guid tie-breaking
        /// - Positive: x comes after y (y has higher priority)
        /// </summary>
        public int Compare(ISchedulable? x, ISchedulable? y)
        {
            // Null handling - nulls come last
            if (x is null && y is null) return 0;
            if (x is null) return 1;
            if (y is null) return -1;

            // Primary comparison: NextTurn (earlier times have higher priority)
            var timeComparison = x.NextTurn.Value.CompareTo(y.NextTurn.Value);
            if (timeComparison != 0)
                return timeComparison;

            // Tie-breaker: Compare Guid Ids lexicographically for determinism
            // This ensures consistent ordering when NextTurn values are identical
            return x.Id.CompareTo(y.Id);
        }
    }
}
