using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Grid.Services;
using Darklands.Domain.Pathfinding;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Application.Grid.Queries
{
    /// <summary>
    /// Handler for CalculatePathQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides path calculation for movement planning and UI preview.
    /// Uses A* pathfinding algorithm with obstacle avoidance.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class CalculatePathQueryHandler : IRequestHandler<CalculatePathQuery, Fin<Seq<Position>>>
    {
        private readonly ICategoryLogger _logger;
        private readonly IPathfindingAlgorithm _pathfindingAlgorithm;
        private readonly IGridStateService _gridStateService;

        public CalculatePathQueryHandler(
            ICategoryLogger logger,
            IPathfindingAlgorithm pathfindingAlgorithm,
            IGridStateService gridStateService)
        {
            _logger = logger;
            _pathfindingAlgorithm = pathfindingAlgorithm;
            _gridStateService = gridStateService;
        }

        public Task<Fin<Seq<Position>>> Handle(CalculatePathQuery request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Processing CalculatePathQuery from {FromPosition} to {ToPosition}",
                request.FromPosition, request.ToPosition);

            // Inline validation using Fin<T> pattern (no separate validator per Tech Lead decision)
            var result = ValidatePositions(request.FromPosition, request.ToPosition)
                .Bind(_ => CalculatePathWithAlgorithm(request.FromPosition, request.ToPosition));

            return Task.FromResult(result);
        }

        private Fin<LanguageExt.Unit> ValidatePositions(Position from, Position to)
        {
            if (!_gridStateService.IsValidPosition(from))
                return FinFail<LanguageExt.Unit>(Error.New(400, $"Invalid start position: {from}"));

            if (!_gridStateService.IsValidPosition(to))
                return FinFail<LanguageExt.Unit>(Error.New(400, $"Invalid target position: {to}"));

            return FinSucc(LanguageExt.Unit.Default);
        }

        private Fin<Seq<Position>> CalculatePathWithAlgorithm(Position from, Position to)
        {
            // Get current obstacles (actors + impassable terrain)
            var allObstacles = _gridStateService.GetObstacles();

            // CRITICAL FIX: Remove the start position from obstacles
            // The actor at the start position shouldn't block their own pathfinding!
            var obstacles = allObstacles.Remove(from);

            _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Pathfinding with {ObstacleCount} obstacles (excluded start position)", obstacles.Count);

            // Use A* algorithm to find path
            var pathOption = _pathfindingAlgorithm.FindPath(from, to, obstacles);

            return pathOption.Match(
                Some: path =>
                {
                    var pathSeq = toSeq(path);
                    _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Found path with {PathLength} positions", pathSeq.Count);
                    return FinSucc(pathSeq);
                },
                None: () =>
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Pathfinding, "No path found from {From} to {To}", from, to);
                    return FinFail<Seq<Position>>(Error.New(404, $"No path found from {from} to {to}"));
                }
            );
        }
    }
}
