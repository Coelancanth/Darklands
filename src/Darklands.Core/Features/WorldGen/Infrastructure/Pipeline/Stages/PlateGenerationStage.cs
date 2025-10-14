using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 0: Native plate tectonics simulation (wraps IPlateSimulator strategy).
/// Produces raw heightmap and plates map from plate tectonics physics.
/// </summary>
/// <remarks>
/// TD_027: This stage demonstrates the Strategy pattern integration.
/// IPlateSimulator can be swapped (NativePlateSimulator, WorldEngineSimulator, etc.)
/// without modifying pipeline orchestration logic.
///
/// Dependencies: None (foundation stage)
/// Produces: RawHeightmap, PlatesMap, RawNativeOutput
/// Performance: ~1-2s for 512×512 map (native C++ simulation)
/// </remarks>
public class PlateGenerationStage : IPipelineStage
{
    private readonly IPlateSimulator _plateSimulator;
    private readonly ILogger<PlateGenerationStage> _logger;

    public string StageName => "Plate Generation";

    public PlateGenerationStage(
        IPlateSimulator plateSimulator,
        ILogger<PlateGenerationStage> logger)
    {
        _plateSimulator = plateSimulator;
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Log iteration-aware message
        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation(
            "Stage 0{Suffix}: {StageName} (seed: {Seed}, size: {Size}x{Size}, plates: {Plates})",
            iterationSuffix, StageName, input.Seed, input.WorldSize, input.WorldSize, input.PlateCount);

        // Create simulation parameters from context
        var parameters = new PlateSimulationParams(
            seed: input.Seed,
            worldSize: input.WorldSize,
            plateCount: input.PlateCount);

        // Execute native plate simulation (Strategy pattern!)
        var result = _plateSimulator.Generate(parameters);

        if (result.IsFailure)
        {
            _logger.LogError("Plate simulation failed: {Error}", result.Error);
            return Result.Failure<PipelineContext>(result.Error);
        }

        var nativeResult = result.Value;

        _logger.LogInformation(
            "Stage 0{Suffix} complete: {Width}x{Height} heightmap generated",
            iterationSuffix, nativeResult.Width, nativeResult.Height);

        // Return updated context with plate simulation results
        return Result.Success(input.WithPlateSimulation(nativeResult));
    }
}
