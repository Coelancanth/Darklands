using Darklands.Domain.Grid;
using LanguageExt;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Pathfinding;

/// <summary>
/// A* pathfinding algorithm implementation with deterministic tie-breaking.
/// Uses integer-only math for consistent results across platforms.
/// Implements IPathfindingAlgorithm for dependency injection.
/// </summary>
public sealed class AStarAlgorithm : IPathfindingAlgorithm
{
    /// <summary>
    /// Finds the optimal path using A* algorithm with Manhattan distance heuristic.
    /// Implements deterministic tie-breaking for consistent results.
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="end">Target position</param>
    /// <param name="obstacles">Positions that block movement</param>
    /// <returns>Some(path) if found, None if no path exists</returns>
    public Option<ImmutableList<Position>> FindPath(
        Position start,
        Position end,
        ImmutableHashSet<Position> obstacles)
    {
        // Handle trivial case
        if (start == end)
        {
            return Some(ImmutableList.Create(start));
        }

        // Skip if start or end is blocked
        if (obstacles.Contains(start) || obstacles.Contains(end))
        {
            return None;
        }

        // Initialize data structures with more deterministic approach
        var openSet = new SortedSet<PathfindingNode>();
        var closedSet = new System.Collections.Generic.HashSet<Position>();
        var nodeMap = new Dictionary<Position, PathfindingNode>();
        var openSetLookup = new System.Collections.Generic.HashSet<Position>();

        // Create start node
        var startHCost = PathfindingCostTable.CalculateHeuristic(start, end);
        var startNode = PathfindingNode.Create(start, 0, startHCost, None)
            .IfFail(_ => throw new System.InvalidOperationException("Start node creation should not fail"));

        openSet.Add(startNode);
        nodeMap[start] = startNode;
        openSetLookup.Add(start);

        while (openSet.Count > 0)
        {
            // Get node with lowest F cost (deterministic due to IComparable implementation)
            var currentNode = openSet.Min!;
            openSet.Remove(currentNode);
            openSetLookup.Remove(currentNode.Position);
            closedSet.Add(currentNode.Position);

            // Check if we reached the target
            if (currentNode.Position == end)
            {
                return Some(ReconstructPath(currentNode));
            }

            // Explore neighbors
            foreach (var neighbor in GetNeighbors(currentNode.Position))
            {
                // Skip if obstacle or already evaluated
                if (obstacles.Contains(neighbor) || closedSet.Contains(neighbor))
                {
                    continue;
                }

                try
                {
                    // Calculate costs
                    var movementCost = PathfindingCostTable.GetMovementCost(currentNode.Position, neighbor);
                    var tentativeGCost = currentNode.GCost + movementCost;
                    var hCost = PathfindingCostTable.CalculateHeuristic(neighbor, end);

                    // Check if this is a better path to the neighbor
                    var existingNode = nodeMap.GetValueOrDefault(neighbor);
                    if (existingNode != null && openSetLookup.Contains(neighbor))
                    {
                        if (tentativeGCost >= existingNode.GCost)
                        {
                            continue; // Not a better path
                        }

                        // Remove the old node from open set
                        openSet.Remove(existingNode);
                        openSetLookup.Remove(neighbor);
                    }
                    else if (existingNode != null)
                    {
                        // Node was already processed, skip
                        continue;
                    }

                    // Create new node with better path
                    var neighborNode = PathfindingNode.Create(neighbor, tentativeGCost, hCost, Some(currentNode))
                        .IfFail(_ => throw new System.InvalidOperationException("Neighbor node creation should not fail"));

                    nodeMap[neighbor] = neighborNode;
                    openSet.Add(neighborNode);
                    openSetLookup.Add(neighbor);
                }
                catch (System.ArgumentException)
                {
                    // Skip invalid movement (non-adjacent positions)
                    continue;
                }
            }
        }

        // No path found
        return None;
    }

    /// <summary>
    /// Gets all valid adjacent positions (8-directional movement).
    /// </summary>
    private static IEnumerable<Position> GetNeighbors(Position position)
    {
        var neighbors = new[]
        {
            new Position(position.X - 1, position.Y - 1), // NW
            new Position(position.X, position.Y - 1),     // N
            new Position(position.X + 1, position.Y - 1), // NE
            new Position(position.X - 1, position.Y),     // W
            new Position(position.X + 1, position.Y),     // E
            new Position(position.X - 1, position.Y + 1), // SW
            new Position(position.X, position.Y + 1),     // S
            new Position(position.X + 1, position.Y + 1)  // SE
        };

        return neighbors;
    }

    /// <summary>
    /// Reconstructs the path from target to start by following parent references.
    /// </summary>
    private static ImmutableList<Position> ReconstructPath(PathfindingNode targetNode)
    {
        var path = new List<Position>();
        var current = Some(targetNode);

        while (current.IsSome)
        {
            current.Match(
                Some: node =>
                {
                    path.Add(node.Position);
                    current = node.Parent;
                },
                None: () => current = None
            );
        }

        // Reverse to get start-to-end order
        path.Reverse();
        return path.ToImmutableList();
    }
}
