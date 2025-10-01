using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Movement.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Movement.Application.Queries;

/// <summary>
/// Handler for FindPathQuery.
/// Delegates to IPathfindingService for A* pathfinding algorithm.
/// </summary>
/// <remarks>
/// Per ADR-003: Railway-oriented programming with Result&lt;T&gt;.
/// Per ADR-004: Application layer handler delegates to Infrastructure service.
///
/// Phase 2: Handler structure (service interface call)
/// Phase 3: Service implementation (A* algorithm with Chebyshev heuristic)
/// </remarks>
public class FindPathQueryHandler : IRequestHandler<FindPathQuery, Result<IReadOnlyList<Position>>>
{
    private readonly IPathfindingService _pathfindingService;
    private readonly ILogger<FindPathQueryHandler> _logger;

    public FindPathQueryHandler(
        IPathfindingService pathfindingService,
        ILogger<FindPathQueryHandler> logger)
    {
        _pathfindingService = pathfindingService;
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<Position>>> Handle(
        FindPathQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Finding path from ({StartX}, {StartY}) to ({GoalX}, {GoalY})",
            request.Start.X,
            request.Start.Y,
            request.Goal.X,
            request.Goal.Y);

        // Validate input (programmer errors - fail fast per ADR-003)
        if (request.IsPassable == null)
            throw new ArgumentNullException(nameof(request.IsPassable));
        if (request.GetCost == null)
            throw new ArgumentNullException(nameof(request.GetCost));

        // Delegate to pathfinding service (Phase 3 will implement A* algorithm)
        var result = _pathfindingService.FindPath(
            request.Start,
            request.Goal,
            request.IsPassable,
            request.GetCost);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Path found: {Length} steps from ({StartX}, {StartY}) to ({GoalX}, {GoalY})",
                result.Value.Count,
                request.Start.X,
                request.Start.Y,
                request.Goal.X,
                request.Goal.Y);
        }
        else
        {
            _logger.LogWarning(
                "No path found from ({StartX}, {StartY}) to ({GoalX}, {GoalY}): {Error}",
                request.Start.X,
                request.Start.Y,
                request.Goal.X,
                request.Goal.Y,
                result.Error);
        }

        return Task.FromResult(result);
    }
}
