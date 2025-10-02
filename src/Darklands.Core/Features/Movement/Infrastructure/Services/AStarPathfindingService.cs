using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Movement.Application.Services;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Movement.Infrastructure.Services;

/// <summary>
/// A* pathfinding service implementation with 8-directional movement.
/// Uses Chebyshev distance heuristic (max(|dx|, |dy|)) for diagonal movement.
/// </summary>
/// <remarks>
/// Per ADR-003: Returns Result&lt;T&gt; for functional error handling.
/// Per ADR-004: Infrastructure layer provides concrete implementation.
///
/// **Algorithm Details** (VS_006):
/// - 8-directional movement: N, S, E, W, NE, NW, SE, SW
/// - Diagonal cost = 1.0 (matches roguelike genre standard per Caves of Qud)
/// - Heuristic: Chebyshev distance = max(|dx|, |dy|)
///   - Admissible for diagonal movement (never overestimates)
///   - Consistent (monotonic) for A* optimality guarantee
/// - Data structures:
///   - Open set: PriorityQueue (O(log n) insert/extract)
///   - Closed set: HashSet (O(1) membership check)
///   - Parent tracking: Dictionary for path reconstruction
///
/// **Performance Target**: &lt;50ms for longest path on 30x30 grid
///
/// **Design Decision**: Checks isPassable BEFORE getCost (efficiency)
/// - Only compute cost for tiles we can actually enter
/// - Saves unnecessary cost calculations for walls
/// </remarks>
public class AStarPathfindingService : IPathfindingService
{
    private readonly ILogger<AStarPathfindingService> _logger;

    // 8 directions: Diagonals first for optimal pathfinding, then cardinals
    // Diagonal-first order ensures shortest paths on uniform-cost grids
    private static readonly Position[] Directions = new[]
    {
        new Position(1, -1),  // NE
        new Position(-1, -1), // NW
        new Position(1, 1),   // SE
        new Position(-1, 1),  // SW
        new Position(0, -1),  // N
        new Position(0, 1),   // S
        new Position(-1, 0),  // W
        new Position(1, 0)    // E
    };

    public AStarPathfindingService(ILogger<AStarPathfindingService> logger)
    {
        _logger = logger;
    }

    public Result<IReadOnlyList<Position>> FindPath(
        Position start,
        Position goal,
        Func<Position, bool> isPassable,
        Func<Position, int> getCost)
    {
        _logger.LogDebug(
            "A* pathfinding from ({StartX}, {StartY}) to ({GoalX}, {GoalY})",
            start.X,
            start.Y,
            goal.X,
            goal.Y);

        // Edge case: start == goal
        if (start == goal)
        {
            _logger.LogDebug("Start equals goal - returning single-position path");
            return Result.Success<IReadOnlyList<Position>>(new List<Position> { start });
        }

        // Validate start and goal are passable
        if (!isPassable(start))
        {
            return Result.Failure<IReadOnlyList<Position>>(
                $"Start position ({start.X}, {start.Y}) is impassable");
        }

        if (!isPassable(goal))
        {
            return Result.Failure<IReadOnlyList<Position>>(
                $"Goal position ({goal.X}, {goal.Y}) is impassable");
        }

        // A* data structures
        // Use double for costs to preserve diagonal movement precision (1.414 vs 1.0)
        var openSet = new PriorityQueue<Position, double>();
        var openSetHash = new HashSet<Position>(); // O(1) membership check
        var closedSet = new HashSet<Position>();
        var gScore = new Dictionary<Position, double>(); // Cost from start to position
        var parent = new Dictionary<Position, Position>(); // For path reconstruction

        // Initialize with start position
        gScore[start] = 0;
        openSet.Enqueue(start, ChebyshevDistance(start, goal));
        openSetHash.Add(start);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            openSetHash.Remove(current);

            // Skip if already processed (can happen with priority queue duplicates)
            if (closedSet.Contains(current))
                continue;

            // Goal reached - reconstruct path
            if (current == goal)
            {
                var path = ReconstructPath(parent, current);
                // HOT PATH: Called frequently during hover preview (20+ times/second)
                // A* internals are debug-level detail, not operational information
                _logger.LogDebug(
                    "A* found path: {Length} steps, cost={Cost}",
                    path.Count,
                    gScore[goal]);
                return Result.Success<IReadOnlyList<Position>>(path);
            }

            closedSet.Add(current);

            // Explore neighbors (8 directions)
            foreach (var direction in Directions)
            {
                var neighbor = new Position(current.X + direction.X, current.Y + direction.Y);

                // Skip if already evaluated
                if (closedSet.Contains(neighbor))
                    continue;

                // Skip if impassable (check BEFORE getCost - optimization)
                if (!isPassable(neighbor))
                    continue;

                // Calculate movement cost: diagonal = √2 ≈ 1.414, cardinal = 1.0
                // This ensures geometrically shortest paths are found
                // Use double precision to avoid truncation (e.g., 1.414 truncated to 1)
                bool isDiagonal = direction.X != 0 && direction.Y != 0;
                double baseCost = getCost(neighbor);
                double movementCost = isDiagonal ? baseCost * Math.Sqrt(2) : baseCost;

                var tentativeGScore = gScore[current] + movementCost;

                // If this path to neighbor is better than any previous one
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    // Record this path
                    parent[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    double fScoreValue = tentativeGScore + ChebyshevDistance(neighbor, goal);

                    // Add to open set (even if already there - priority queue will handle it)
                    openSet.Enqueue(neighbor, fScoreValue);
                    openSetHash.Add(neighbor); // Track membership
                }
            }
        }

        // No path found
        _logger.LogWarning(
            "A* failed: no path from ({StartX}, {StartY}) to ({GoalX}, {GoalY})",
            start.X,
            start.Y,
            goal.X,
            goal.Y);

        return Result.Failure<IReadOnlyList<Position>>(
            $"No path exists from ({start.X}, {start.Y}) to ({goal.X}, {goal.Y})");
    }

    /// <summary>
    /// Chebyshev distance heuristic for 8-directional movement.
    /// Formula: max(|dx|, |dy|)
    /// Admissible (never overestimates) and consistent (monotonic) for A*.
    /// </summary>
    private static int ChebyshevDistance(Position from, Position to)
    {
        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        return Math.Max(dx, dy);
    }

    /// <summary>
    /// Reconstructs path by walking backwards from goal using parent pointers.
    /// Returns path from start to goal (inclusive).
    /// </summary>
    private static IReadOnlyList<Position> ReconstructPath(
        Dictionary<Position, Position> parent,
        Position goal)
    {
        var path = new List<Position> { goal };
        var current = goal;

        while (parent.ContainsKey(current))
        {
            current = parent[current];
            path.Add(current);
        }

        path.Reverse(); // Reverse to get start → goal order
        return path;
    }
}
