using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Movement.Application.Queries;

/// <summary>
/// Query to find optimal path between two positions on the grid.
/// Returns ordered list of positions from start to goal (inclusive).
/// </summary>
/// <param name="Start">Starting position</param>
/// <param name="Goal">Target destination</param>
/// <param name="IsPassable">Function to check if position is passable (terrain + occupancy)</param>
/// <param name="GetCost">Function to get movement cost for passable tiles (e.g., floor=1, smoke=2)</param>
/// <remarks>
/// Per ADR-003: Returns Result&lt;T&gt; for functional error handling.
/// Per ADR-004: Query pattern for read-only pathfinding operation.
///
/// Phase 2: Query definition
/// Phase 3: Handler implementation delegates to IPathfindingService (A* algorithm)
/// Phase 4: Presentation calls this before MoveAlongPathCommand for path preview
/// </remarks>
public record FindPathQuery(
    Position Start,
    Position Goal,
    Func<Position, bool> IsPassable,
    Func<Position, int> GetCost
) : IRequest<Result<IReadOnlyList<Position>>>;
