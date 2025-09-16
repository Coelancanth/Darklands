using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Darklands.Application.Grid.Services;
using Darklands.Application.Vision.Services;
using Darklands.Domain.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Application.Vision.Commands
{
    /// <summary>
    /// Handler for CalculateFOVConsoleCommand - Provides debug output for FOV testing.
    /// Returns formatted console output showing vision calculation results.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class CalculateFOVConsoleCommandHandler : IRequestHandler<CalculateFOVConsoleCommand, Fin<string>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly IVisionStateService _visionStateService;
        private readonly ICategoryLogger _logger;

        public CalculateFOVConsoleCommandHandler(
            IGridStateService gridStateService,
            IVisionStateService visionStateService,
            ICategoryLogger logger)
        {
            _gridStateService = gridStateService;
            _visionStateService = visionStateService;
            _logger = logger;
        }

        public Task<Fin<string>> Handle(CalculateFOVConsoleCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, LogCategory.Vision, "Processing FOV Console Command for Actor {ActorId} at {Position} with range {Range}",
                request.ViewerId.Value.ToString()[..8], request.Origin, request.Range.Value);

            var result = GenerateConsoleOutput(request);

            return Task.FromResult(result.Match(
                Succ: output =>
                {
                    _logger.Log(LogLevel.Information, LogCategory.Vision, "Generated FOV console output ({Length} characters)", output.Length);
                    return result;
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Vision, "Failed to generate FOV console output: {Error}", error.Message);
                    return result;
                }
            ));
        }

        /// <summary>
        /// Generates formatted console output for FOV calculation.
        /// </summary>
        private Fin<string> GenerateConsoleOutput(CalculateFOVConsoleCommand request)
        {
            try
            {
                var output = new StringBuilder();

                // Header
                output.AppendLine("=== FOV CALCULATION RESULTS ===");
                output.AppendLine($"Actor: {request.ViewerId.Value.ToString()[..8]}");
                output.AppendLine($"Origin: {request.Origin}");
                output.AppendLine($"Range: {request.Range.Value} tiles");
                output.AppendLine($"Turn: {request.CurrentTurn}");
                output.AppendLine();

                // Get grid for wall checking
                var gridResult = _gridStateService.GetCurrentGrid();
                if (gridResult.IsFail)
                {
                    return FinFail<string>(Error.New("Cannot access grid state for FOV calculation"));
                }

                var grid = gridResult.IfFail(_ => throw new System.InvalidOperationException());

                // Calculate FOV using shadowcasting
                var fovResult = ShadowcastingFOV.CalculateFOV(request.Origin, request.Range.Value, grid);
                if (fovResult.IsFail)
                {
                    return fovResult.Map<string>(_ => throw new System.InvalidOperationException());
                }

                var visibleTiles = fovResult.IfFail(_ => throw new System.InvalidOperationException());

                // Basic statistics
                output.AppendLine($"Visible tiles: {visibleTiles.Count}");
                output.AppendLine($"Theoretical maximum: {CalculateTheoreticalMax(request.Range.Value)}");
                output.AppendLine($"Coverage: {(double)visibleTiles.Count / CalculateTheoreticalMax(request.Range.Value):P1}");
                output.AppendLine();

                // Wall analysis
                var wallCount = 0;
                foreach (var pos in visibleTiles)
                {
                    var tileResult = grid.GetTile(pos);
                    if (tileResult.Match(
                        Succ: tile => tile.BlocksLineOfSight,
                        Fail: _ => false))
                    {
                        wallCount++;
                    }
                }

                output.AppendLine($"Walls visible: {wallCount}");
                output.AppendLine($"Empty tiles visible: {visibleTiles.Count - wallCount}");
                output.AppendLine();

                // Get vision state from service
                var visionStateResult = _visionStateService.GetVisionState(request.ViewerId);
                if (visionStateResult.IsSucc)
                {
                    var visionState = visionStateResult.IfFail(_ => throw new System.InvalidOperationException());
                    output.AppendLine($"Previously explored: {visionState.PreviouslyExplored.Count}");
                    output.AppendLine($"Last calculated turn: {visionState.LastCalculatedTurn}");
                    output.AppendLine($"Cache valid: {!visionState.NeedsRecalculation(request.CurrentTurn)}");
                    output.AppendLine();
                }

                // Debug output if requested
                if (request.ShowDebugOutput)
                {
                    output.AppendLine("=== DEBUG: VISIBLE POSITIONS ===");
                    var sortedPositions = visibleTiles.OrderBy(p => p.Y).ThenBy(p => p.X);
                    foreach (var pos in sortedPositions)
                    {
                        var tileResult = grid.GetTile(pos);
                        var tileType = tileResult.Match(
                            Succ: tile => tile.BlocksLineOfSight ? "WALL" : "FLOOR",
                            Fail: _ => "OOB"
                        );
                        var distance = CalculateDistance(request.Origin, pos);
                        output.AppendLine($"  {pos} - {tileType} (dist: {distance:F1})");
                    }
                    output.AppendLine();
                }

                // Performance test
                var startTime = DateTime.UtcNow;
                for (int i = 0; i < 100; i++)
                {
                    ShadowcastingFOV.CalculateFOV(request.Origin, request.Range.Value, grid);
                }
                var endTime = DateTime.UtcNow;
                var avgTime = (endTime - startTime).TotalMilliseconds / 100.0;

                output.AppendLine($"Performance: {avgTime:F2}ms average (100 iterations)");
                output.AppendLine("=== END FOV RESULTS ===");

                return FinSucc(output.ToString());
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to generate FOV console output", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error generating FOV console output: {Exception}", ex.Message);
                return FinFail<string>(error);
            }
        }

        /// <summary>
        /// Calculates theoretical maximum visible tiles for a given range.
        /// </summary>
        private static int CalculateTheoreticalMax(int range)
        {
            // Approximate circle area: π * r²
            return (int)(Math.PI * range * range);
        }

        /// <summary>
        /// Calculates Euclidean distance between two positions.
        /// </summary>
        private static double CalculateDistance(Darklands.Domain.Grid.Position from, Darklands.Domain.Grid.Position to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
