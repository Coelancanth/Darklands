using Darklands.Domain.Grid;
using LanguageExt;
using LanguageExt.Common;
using System;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Pathfinding;

/// <summary>
/// Represents a node in the A* pathfinding algorithm.
/// Immutable record containing position, costs, and parent reference.
/// Implements deterministic comparison for consistent pathfinding results.
/// </summary>
public sealed record PathfindingNode : IComparable<PathfindingNode>
{
    /// <summary>
    /// Grid position of this node.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// G cost - actual cost from start to this node.
    /// </summary>
    public int GCost { get; }

    /// <summary>
    /// H cost - heuristic cost from this node to target.
    /// </summary>
    public int HCost { get; }

    /// <summary>
    /// F cost - total estimated cost (G + H).
    /// </summary>
    public int FCost => GCost + HCost;

    /// <summary>
    /// Parent node in the path, used for path reconstruction.
    /// </summary>
    public Option<PathfindingNode> Parent { get; }

    private PathfindingNode(Position position, int gCost, int hCost, Option<PathfindingNode> parent)
    {
        Position = position;
        GCost = gCost;
        HCost = hCost;
        Parent = parent;
    }

    /// <summary>
    /// Creates a new PathfindingNode with validation.
    /// </summary>
    /// <param name="position">Grid position</param>
    /// <param name="gCost">Actual cost from start (must be non-negative)</param>
    /// <param name="hCost">Heuristic cost to target (must be non-negative)</param>
    /// <param name="parent">Parent node in path</param>
    /// <returns>Success with node or failure with error</returns>
    public static Fin<PathfindingNode> Create(Position position, int gCost, int hCost, Option<PathfindingNode> parent)
    {
        if (gCost < 0)
        {
            return FinFail<PathfindingNode>(Error.New("GCost cannot be negative"));
        }

        if (hCost < 0)
        {
            return FinFail<PathfindingNode>(Error.New("HCost cannot be negative"));
        }

        return FinSucc(new PathfindingNode(position, gCost, hCost, parent));
    }

    /// <summary>
    /// Deterministic comparison for consistent A* behavior.
    /// Order: F cost (ascending), then H cost (ascending), then X (ascending), then Y (ascending).
    /// This ensures identical paths for identical input conditions.
    /// </summary>
    /// <param name="other">Node to compare with</param>
    /// <returns>Comparison result for sorting</returns>
    public int CompareTo(PathfindingNode? other)
    {
        if (other is null) return 1;

        // Primary: F cost (lower is better)
        var fComparison = FCost.CompareTo(other.FCost);
        if (fComparison != 0) return fComparison;

        // Secondary: H cost (lower is better)
        var hComparison = HCost.CompareTo(other.HCost);
        if (hComparison != 0) return hComparison;

        // Tertiary: X coordinate (lower is better)
        var xComparison = Position.X.CompareTo(other.Position.X);
        if (xComparison != 0) return xComparison;

        // Quaternary: Y coordinate (lower is better)
        return Position.Y.CompareTo(other.Position.Y);
    }

    /// <summary>
    /// Creates a string representation for debugging.
    /// </summary>
    public override string ToString() =>
        $"Node({Position}, G={GCost}, H={HCost}, F={FCost})";
}
