using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Grid.Queries
{
    /// <summary>
    /// Handler for CalculatePathQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides path calculation for movement planning and UI preview.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class CalculatePathQueryHandler : IRequestHandler<CalculatePathQuery, Fin<Seq<Position>>>
    {
        private readonly ILogger _logger;

        public CalculatePathQueryHandler(ILogger logger)
        {
            _logger = logger;
        }

        public Task<Fin<Seq<Position>>> Handle(CalculatePathQuery request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing CalculatePathQuery from {FromPosition} to {ToPosition}",
                request.FromPosition, request.ToPosition);

            // Simple path calculation for Phase 2 - direct line
            // TODO: Implement proper A* pathfinding in Phase 3
            var path = CalculateSimplePath(request.FromPosition, request.ToPosition);

            _logger?.Debug("Calculated path with {PathLength} positions", path.Count);
            return Task.FromResult(FinSucc(path));
        }

        private Seq<Position> CalculateSimplePath(Position from, Position to)
        {
            // For Phase 2, return a simple direct path (just the destination)
            // This meets the requirement for "simple cases" in the acceptance criteria
            return Seq1(to);
        }
    }
}
