using Darklands.Domain.Grid;
using System;

namespace Darklands.Domain.Pathfinding;

/// <summary>
/// Static cost table for pathfinding calculations using integer-only math.
/// Provides deterministic movement costs for A* algorithm.
/// Following ADR-004 determinism constraints with integer arithmetic.
/// </summary>
public static class PathfindingCostTable
{
    /// <summary>
    /// Cost for straight movement (horizontal/vertical).
    /// Base unit for all pathfinding calculations.
    /// </summary>
    public const int StraightCost = 100;

    /// <summary>
    /// Cost for diagonal movement.
    /// Approximates sqrt(2) * 100 = 141.42... as 141 for integer math.
    /// </summary>
    public const int DiagonalCost = 141;

    /// <summary>
    /// Calculates movement cost between two adjacent positions.
    /// </summary>
    /// <param name="from">Starting position</param>
    /// <param name="to">Destination position</param>
    /// <returns>Integer movement cost (100 for straight, 141 for diagonal)</returns>
    /// <exception cref="ArgumentException">Thrown when positions are not adjacent</exception>
    public static int GetMovementCost(Position from, Position to)
    {
        var deltaX = Math.Abs(to.X - from.X);
        var deltaY = Math.Abs(to.Y - from.Y);

        // Validate adjacency
        if (deltaX > 1 || deltaY > 1 || (deltaX == 0 && deltaY == 0))
        {
            throw new ArgumentException($"Positions must be adjacent: {from} -> {to}");
        }

        // Diagonal movement (both X and Y change)
        if (deltaX == 1 && deltaY == 1)
        {
            return DiagonalCost;
        }

        // Straight movement (only X or Y changes)
        return StraightCost;
    }

    /// <summary>
    /// Calculates Manhattan distance heuristic for A* algorithm.
    /// Uses straight movement cost as base unit.
    /// </summary>
    /// <param name="from">Starting position</param>
    /// <param name="to">Target position</param>
    /// <returns>Manhattan distance * StraightCost</returns>
    public static int CalculateHeuristic(Position from, Position to)
    {
        var deltaX = Math.Abs(to.X - from.X);
        var deltaY = Math.Abs(to.Y - from.Y);
        var manhattanDistance = deltaX + deltaY;

        return manhattanDistance * StraightCost;
    }
}
