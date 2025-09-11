using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Domain.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Vision.Queries
{
    /// <summary>
    /// Handler for CalculateFOVQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Calculates field of view using shadowcasting algorithm and manages vision state caching.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class CalculateFOVQueryHandler : IRequestHandler<CalculateFOVQuery, Fin<VisionState>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly IVisionStateService _visionStateService;
        private readonly ILogger _logger;

        public CalculateFOVQueryHandler(
            IGridStateService gridStateService,
            IVisionStateService visionStateService,
            ILogger logger)
        {
            _gridStateService = gridStateService;
            _visionStateService = visionStateService;
            _logger = logger;
        }

        public Task<Fin<VisionState>> Handle(CalculateFOVQuery request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing CalculateFOVQuery for Actor {ActorId} at {Position} with range {Range}",
                request.ViewerId.Value.ToString()[..8], request.Origin, request.Range.Value);

            var result = CalculateVisionState(request);

            return Task.FromResult(result.Match(
                Succ: visionState =>
                {
                    _logger?.Debug("Calculated FOV successfully: {Visible} visible tiles, {Explored} total explored",
                        visionState.CurrentlyVisible.Count, visionState.PreviouslyExplored.Count);
                    return result;
                },
                Fail: error =>
                {
                    _logger?.Warning("Failed to calculate FOV: {Error}", error.Message);
                    return result;
                }
            ));
        }

        /// <summary>
        /// Core FOV calculation logic using domain shadowcasting and vision state caching.
        /// </summary>
        private Fin<VisionState> CalculateVisionState(CalculateFOVQuery request)
        {
            // Get current grid state
            var gridResult = _gridStateService.GetCurrentGrid();
            if (gridResult.IsFail)
                return gridResult.Map<VisionState>(_ => throw new System.InvalidOperationException());

            var grid = gridResult.IfFail(_ => throw new System.InvalidOperationException());

            // Check if we have cached vision state and if it needs recalculation
            var cachedStateResult = _visionStateService.GetVisionState(request.ViewerId);
            var previousState = cachedStateResult.Match(
                Succ: state => state.NeedsRecalculation(request.CurrentTurn) ? state : null,
                Fail: _ => null
            );

            // If cached state is still valid, return it
            if (previousState != null && !previousState.NeedsRecalculation(request.CurrentTurn))
            {
                _logger?.Debug("Using cached vision state for Actor {ActorId}",
                    request.ViewerId.Value.ToString()[..8]);
                return previousState;
            }

            // Calculate new FOV using shadowcasting
            var newVisionStateResult = ShadowcastingFOV.CalculateVisionState(
                request.ViewerId,
                request.Origin,
                request.Range,
                grid,
                previousState,
                request.CurrentTurn
            );

            // Cache the new vision state
            return newVisionStateResult.Match(
                Succ: newState =>
                {
                    var cacheResult = _visionStateService.UpdateVisionState(newState);
                    cacheResult.Match(
                        Succ: _ => { },
                        Fail: error => _logger?.Warning("Failed to cache vision state: {Error}", error.Message)
                    );
                    // Always return the calculated state, regardless of caching result
                    return newState;
                },
                Fail: error => Fin<VisionState>.Fail(error)
            );
        }
    }
}
