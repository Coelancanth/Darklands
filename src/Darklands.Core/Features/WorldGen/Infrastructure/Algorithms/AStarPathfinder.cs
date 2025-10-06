using System;
using System.Collections.Generic;
using System.Linq;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// A* pathfinding algorithm for elevation-based river tracing.
/// Finds the lowest-cost path between two points on a heightmap.
///
/// Ported from WorldEngine's astar.py - Used by ErosionSimulation when
/// rivers can't find a simple downhill path to the ocean.
///
/// Cost model: Higher elevation = higher cost (water flows downhill)
/// </summary>
public static class AStarPathfinder
{
    private const int MaxIterations = 10000; // Bail-out to prevent infinite loops

    /// <summary>
    /// Finds the lowest-cost path from source to destination on a heightmap.
    /// Uses Manhattan distance heuristic for efficient pathfinding.
    /// </summary>
    /// <param name="heightmap">2D elevation data (higher values = higher cost)</param>
    /// <param name="source">Starting position (x, y)</param>
    /// <param name="destination">Target position (x, y)</param>
    /// <returns>List of positions forming path, or empty list if no path found</returns>
    public static List<(int x, int y)> FindPath(
        float[,] heightmap,
        (int x, int y) source,
        (int x, int y) destination)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Validate bounds
        if (!IsInBounds(source, width, height) || !IsInBounds(destination, width, height))
            return new List<(int, int)>();

        var openSet = new List<Node>();
        var closedSet = new HashSet<int>();

        // Start node
        var startNode = new Node(
            location: source,
            movementCost: heightmap[source.y, source.x],
            locationId: GetLocationId(source, width),
            parent: null);

        startNode.Score = CalculateHeuristic(source, destination);
        openSet.Add(startNode);

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MaxIterations)
        {
            iterations++;

            // Get node with lowest score
            var current = GetBestNode(openSet);

            // Reached destination?
            if (current.Location == destination)
                return ReconstructPath(current);

            // Move current from open to closed
            openSet.Remove(current);
            closedSet.Add(current.LocationId);

            // Check all 4-connected neighbors (N, S, E, W)
            foreach (var neighbor in GetNeighbors(current, destination, heightmap, width, height))
            {
                // Already evaluated?
                if (closedSet.Contains(neighbor.LocationId))
                    continue;

                // Find existing node in open set
                var existingNode = openSet.FirstOrDefault(n => n.LocationId == neighbor.LocationId);

                if (existingNode != null)
                {
                    // Better path to this node?
                    if (neighbor.MovementCost < existingNode.MovementCost)
                    {
                        openSet.Remove(existingNode);
                        openSet.Add(neighbor);
                    }
                }
                else
                {
                    // New node
                    openSet.Add(neighbor);
                }
            }
        }

        // No path found
        return new List<(int, int)>();
    }

    /// <summary>
    /// Gets the node with the lowest score (f = g + h) from the open set.
    /// </summary>
    private static Node GetBestNode(List<Node> openSet)
    {
        Node best = openSet[0];
        for (int i = 1; i < openSet.Count; i++)
        {
            if (openSet[i].Score < best.Score)
                best = openSet[i];
        }
        return best;
    }

    /// <summary>
    /// Gets valid 4-connected neighbors (N, S, E, W) for pathfinding.
    /// </summary>
    private static IEnumerable<Node> GetNeighbors(
        Node current,
        (int x, int y) destination,
        float[,] heightmap,
        int width,
        int height)
    {
        var (cx, cy) = current.Location;

        // 4-connected neighbors: North, South, East, West
        var directions = new[] { (0, -1), (0, 1), (1, 0), (-1, 0) };

        foreach (var (dx, dy) in directions)
        {
            int nx = cx + dx;
            int ny = cy + dy;

            if (!IsInBounds((nx, ny), width, height))
                continue;

            // Create neighbor node
            float elevationCost = heightmap[ny, nx];
            int locationId = GetLocationId((nx, ny), width);

            var neighbor = new Node(
                location: (nx, ny),
                movementCost: current.MovementCost + elevationCost,
                locationId: locationId,
                parent: current);

            // Calculate score: f = g + h
            int heuristic = CalculateHeuristic((nx, ny), destination);
            neighbor.Score = neighbor.MovementCost + heuristic;

            yield return neighbor;
        }
    }

    /// <summary>
    /// Manhattan distance heuristic (admissible and consistent).
    /// </summary>
    private static int CalculateHeuristic((int x, int y) from, (int x, int y) to)
    {
        return Math.Abs(to.x - from.x) + Math.Abs(to.y - from.y);
    }

    /// <summary>
    /// Reconstructs the path by following parent pointers from destination to source.
    /// </summary>
    private static List<(int x, int y)> ReconstructPath(Node destination)
    {
        var path = new List<(int x, int y)>();
        var current = destination;

        while (current != null)
        {
            path.Insert(0, current.Location);
            current = current.Parent;
        }

        // Remove first node (source, already in river path)
        if (path.Count > 0)
            path.RemoveAt(0);

        return path;
    }

    /// <summary>
    /// Converts 2D position to unique 1D location ID.
    /// </summary>
    private static int GetLocationId((int x, int y) position, int width)
    {
        return position.y * width + position.x;
    }

    /// <summary>
    /// Checks if position is within heightmap bounds.
    /// </summary>
    private static bool IsInBounds((int x, int y) position, int width, int height)
    {
        return position.x >= 0 && position.x < width &&
               position.y >= 0 && position.y < height;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Classes
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a node in the A* search graph.
    /// </summary>
    private class Node
    {
        public (int x, int y) Location { get; }
        public float MovementCost { get; }  // g: Cost from start to this node
        public float Score { get; set; }    // f: MovementCost + Heuristic
        public int LocationId { get; }      // Unique ID for this location
        public Node? Parent { get; }        // Parent node for path reconstruction

        public Node(
            (int x, int y) location,
            float movementCost,
            int locationId,
            Node? parent)
        {
            Location = location;
            MovementCost = movementCost;
            LocationId = locationId;
            Parent = parent;
        }
    }
}
