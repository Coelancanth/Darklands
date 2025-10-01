using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Movement.Application.Services;

/// <summary>
/// Service for calculating optimal paths between positions on a grid.
/// </summary>
/// <remarks>
/// Per ADR-004: Application layer service interface.
/// Infrastructure layer provides concrete implementation (A* algorithm in Phase 3).
///
/// Design Decision (VS_006): Interface accepts cost function NOW to prevent breaking changes
/// when variable action costs are introduced in future vertical slices.
/// </remarks>
public interface IPathfindingService
{
    /// <summary>
    /// Finds optimal path from start to goal using A* pathfinding algorithm.
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="goal">Target destination</param>
    /// <param name="isPassable">
    /// Function to check if a position is passable.
    /// Should validate terrain passability and actor occupancy.
    /// </param>
    /// <param name="getCost">
    /// Function to get movement cost for a passable position.
    /// Example: floor=1, smoke=2, water=3.
    /// Only called for positions where isPassable returns true.
    /// </param>
    /// <returns>
    /// Success: Ordered list of positions from start to goal (inclusive).
    /// Failure: Error message if no path exists or invalid input.
    /// </returns>
    /// <remarks>
    /// Implementation Notes (Phase 3):
    /// - A* algorithm checks isPassable BEFORE calling getCost (efficiency)
    /// - 8-directional movement (N/S/E/W + NE/NW/SE/SW)
    /// - Diagonal cost = 1.0 (matches roguelike genre standard per Caves of Qud)
    /// - Chebyshev distance heuristic: max(|dx|, |dy|)
    /// - Performance target: &lt;50ms for 30x30 grid
    ///
    /// Phase 4 Usage (uniform cost for VS_006):
    /// ```
    /// var path = await _pathfinding.FindPath(
    ///     start: currentPos,
    ///     goal: clickedPos,
    ///     isPassable: pos => _gridMap.IsPassable(pos).IsSuccess && _gridMap.IsPassable(pos).Value,
    ///     getCost: pos => 1  // Uniform cost - infrastructure ready for future variable costs
    /// );
    /// ```
    /// </remarks>
    Result<IReadOnlyList<Position>> FindPath(
        Position start,
        Position goal,
        Func<Position, bool> isPassable,
        Func<Position, int> getCost);
}
