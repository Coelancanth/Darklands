using Darklands.Domain.Grid;
using System.Collections.Immutable;
using System.Linq;

namespace Darklands.Domain.Pathfinding;

/// <summary>
/// Value object representing the result of a pathfinding operation.
/// Encapsulates success/failure state, path data, and cost information.
/// Immutable for consistent domain modeling.
/// </summary>
public sealed record PathfindingResult
{
    /// <summary>
    /// Whether a path was successfully found.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The found path as an ordered sequence of positions.
    /// Empty if no path was found.
    /// </summary>
    public ImmutableList<Position> Path { get; }

    /// <summary>
    /// Total cost of the path using pathfinding cost units.
    /// Zero if no path was found.
    /// </summary>
    public int TotalCost { get; }

    /// <summary>
    /// Convenience property for checking if a valid path exists.
    /// </summary>
    public bool IsPathFound => IsSuccess && Path.Any();

    private PathfindingResult(bool isSuccess, ImmutableList<Position> path, int totalCost)
    {
        IsSuccess = isSuccess;
        Path = path;
        TotalCost = totalCost;
    }

    /// <summary>
    /// Creates a successful pathfinding result with a valid path.
    /// </summary>
    /// <param name="path">The found path as ordered positions</param>
    /// <param name="totalCost">Total movement cost of the path</param>
    /// <returns>Success result with path data</returns>
    public static PathfindingResult Success(ImmutableList<Position> path, int totalCost) =>
        new(isSuccess: true, path, totalCost);

    /// <summary>
    /// Creates a failed pathfinding result when no path exists.
    /// </summary>
    /// <returns>Failure result with empty path</returns>
    public static PathfindingResult NoPath() =>
        new(isSuccess: false, ImmutableList<Position>.Empty, totalCost: 0);

    /// <summary>
    /// Creates a string representation for debugging.
    /// </summary>
    public override string ToString() =>
        IsSuccess
            ? $"PathFound({Path.Count} steps, cost={TotalCost})"
            : "NoPath";
}
