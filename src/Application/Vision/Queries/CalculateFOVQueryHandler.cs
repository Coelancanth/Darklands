using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Domain.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Vision.Queries
{
    /// <summary>
    /// Handler for CalculateFOVQuery - Enhanced Phase 3 implementation with performance monitoring.
    /// Calculates field of view using shadowcasting algorithm with comprehensive metrics collection.
    /// Integrates with PersistentVisionStateService and VisionPerformanceMonitor for enhanced infrastructure.
    /// </summary>
    public class CalculateFOVQueryHandler : IRequestHandler<CalculateFOVQuery, Fin<VisionState>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly IVisionStateService _visionStateService;
        private readonly IVisionPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public CalculateFOVQueryHandler(
            IGridStateService gridStateService,
            IVisionStateService visionStateService,
            IVisionPerformanceMonitor performanceMonitor,
            ILogger logger)
        {
            _gridStateService = gridStateService;
            _visionStateService = visionStateService;
            _performanceMonitor = performanceMonitor;
            _logger = logger;
        }

        public Task<Fin<VisionState>> Handle(CalculateFOVQuery request, CancellationToken cancellationToken)
        {
            var overallStopwatch = Stopwatch.StartNew();

            _logger?.Debug("Processing enhanced FOV calculation for Actor {ActorId} at {Position} with range {Range}",
                request.ViewerId.Value.ToString()[..8], request.Origin, request.Range.Value);

            var result = CalculateVisionStateWithMetrics(request);

            overallStopwatch.Stop();

            return Task.FromResult(result.Match(
                Succ: visionState =>
                {
                    _logger?.Debug("Enhanced FOV calculation completed in {TotalMs}ms: {Visible} visible tiles, {Explored} total explored",
                        overallStopwatch.Elapsed.TotalMilliseconds,
                        visionState.CurrentlyVisible.Count,
                        visionState.PreviouslyExplored.Count);
                    return result;
                },
                Fail: error =>
                {
                    _logger?.Warning("Enhanced FOV calculation failed after {TotalMs}ms: {Error}",
                        overallStopwatch.Elapsed.TotalMilliseconds, error.Message);
                    return result;
                }
            ));
        }

        /// <summary>
        /// Core FOV calculation with comprehensive performance monitoring and enhanced caching.
        /// </summary>
        private Fin<VisionState> CalculateVisionStateWithMetrics(CalculateFOVQuery request)
        {
            var calculationStopwatch = Stopwatch.StartNew();

            try
            {
                // Get current grid state
                var gridResult = _gridStateService.GetCurrentGrid();
                if (gridResult.IsFail)
                    return gridResult.Map<VisionState>(_ => throw new InvalidOperationException());

                var grid = gridResult.IfFail(_ => throw new InvalidOperationException());

                // Check cached vision state with performance tracking
                var cacheStopwatch = Stopwatch.StartNew();
                var cachedStateResult = _visionStateService.GetVisionState(request.ViewerId);
                cacheStopwatch.Stop();

                var previousState = cachedStateResult.Match(
                    Succ: state => state,
                    Fail: _ => VisionState.CreateEmpty(request.ViewerId)
                );

                // Check if cached state is still valid
                if (previousState != null && !previousState.NeedsRecalculation(request.CurrentTurn))
                {
                    calculationStopwatch.Stop();

                    // Record cache hit in performance monitor
                    _performanceMonitor.RecordFOVCalculation(
                        request.ViewerId,
                        calculationStopwatch.Elapsed.TotalMilliseconds,
                        previousState.CurrentlyVisible.Count,
                        0, // No tiles checked for cache hit
                        wasFromCache: true
                    );

                    _logger?.Debug("Using cached vision state for Actor {ActorId} (cache lookup: {CacheMs}ms)",
                        request.ViewerId.Value.ToString()[..8], cacheStopwatch.Elapsed.TotalMilliseconds);

                    return previousState;
                }

                // Calculate new FOV using shadowcasting with detailed metrics
                var fovStopwatch = Stopwatch.StartNew();
                var newVisionStateResult = ShadowcastingFOV.CalculateVisionState(
                    request.ViewerId,
                    request.Origin,
                    request.Range,
                    grid,
                    previousState,
                    request.CurrentTurn
                );
                fovStopwatch.Stop();

                return newVisionStateResult.Match(
                    Succ: newState =>
                    {
                        calculationStopwatch.Stop();

                        // Estimate tiles checked (for performance metrics)
                        var estimatedTilesChecked = EstimateTilesChecked(request.Range.Value);

                        // Record performance metrics
                        _performanceMonitor.RecordFOVCalculation(
                            request.ViewerId,
                            fovStopwatch.Elapsed.TotalMilliseconds,
                            newState.CurrentlyVisible.Count,
                            estimatedTilesChecked,
                            wasFromCache: false
                        );

                        // Cache the new vision state with enhanced persistence
                        var cacheStopwatch2 = Stopwatch.StartNew();
                        var cacheResult = _visionStateService.UpdateVisionState(newState);
                        cacheStopwatch2.Stop();

                        cacheResult.Match(
                            Succ: _ =>
                            {
                                _logger?.Debug("Cached new vision state for Actor {ActorId} (cache update: {CacheMs}ms)",
                                    request.ViewerId.Value.ToString()[..8], cacheStopwatch2.Elapsed.TotalMilliseconds);
                            },
                            Fail: error =>
                            {
                                _logger?.Warning("Failed to cache vision state for Actor {ActorId}: {Error}",
                                    request.ViewerId.Value.ToString()[..8], error.Message);
                            }
                        );

                        _logger?.Debug("Calculated new FOV for Actor {ActorId}: {FOVMs}ms calculation, {CacheMs}ms cache lookup, {UpdateMs}ms cache update",
                            request.ViewerId.Value.ToString()[..8],
                            fovStopwatch.Elapsed.TotalMilliseconds,
                            cacheStopwatch.Elapsed.TotalMilliseconds,
                            cacheStopwatch2.Elapsed.TotalMilliseconds);

                        // Always return the calculated state, regardless of caching result
                        return newState;
                    },
                    Fail: error =>
                    {
                        calculationStopwatch.Stop();

                        // Record failed calculation in performance monitor
                        _performanceMonitor.RecordFOVCalculation(
                            request.ViewerId,
                            calculationStopwatch.Elapsed.TotalMilliseconds,
                            0, // No tiles visible on failure
                            0, // No tiles checked on failure
                            wasFromCache: false
                        );

                        return Fin<VisionState>.Fail(error);
                    }
                );
            }
            catch (Exception ex)
            {
                calculationStopwatch.Stop();

                // Record exception in performance monitor
                _performanceMonitor.RecordFOVCalculation(
                    request.ViewerId,
                    calculationStopwatch.Elapsed.TotalMilliseconds,
                    0, // No tiles visible on exception
                    0, // No tiles checked on exception
                    wasFromCache: false
                );

                var error = Error.New("Enhanced FOV calculation failed with exception", ex);
                _logger?.Error(ex, "Enhanced FOV calculation failed for Actor {ActorId}", request.ViewerId.Value);
                return Fin<VisionState>.Fail(error);
            }
        }

        /// <summary>
        /// Estimates the number of tiles checked during FOV calculation for performance metrics.
        /// Based on the theoretical maximum tiles within range using circle approximation.
        /// </summary>
        private static int EstimateTilesChecked(int range)
        {
            // Rough estimate: π × r² for circle, but shadowcasting checks more due to octants
            // Add 20% overhead for algorithm complexity
            var circleArea = Math.PI * range * range;
            var estimatedTiles = (int)(circleArea * 1.2);
            return Math.Max(estimatedTiles, 1);
        }
    }
}
