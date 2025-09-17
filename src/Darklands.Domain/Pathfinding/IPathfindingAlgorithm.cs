using Darklands.Domain.Grid;
using LanguageExt;
using System.Collections.Immutable;

namespace Darklands.Domain.Pathfinding;

/// <summary>
/// Interface for pathfinding algorithms.
/// Defines the contract for finding optimal paths between positions on a grid.
/// Supports obstacle avoidance and returns Option for safe null handling.
/// </summary>
public interface IPathfindingAlgorithm
{
    /// <summary>
    /// Finds the optimal path between two positions on a grid.
    /// Uses the algorithm's specific heuristics and cost calculations.
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="end">Target position</param>
    /// <param name="obstacles">Set of positions that block movement</param>
    /// <returns>
    /// Some(path) if a path exists, None if no path is possible.
    /// Path includes start and end positions.
    /// </returns>
    Option<ImmutableList<Position>> FindPath(
        Position start,
        Position end,
        ImmutableHashSet<Position> obstacles);
}
