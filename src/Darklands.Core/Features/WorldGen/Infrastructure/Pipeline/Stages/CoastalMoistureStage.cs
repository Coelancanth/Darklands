using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 5: Coastal moisture enhancement (VS_028).
/// Applies maritime climate effects - coastal regions wetter than interior.
/// </summary>
/// <remarks>
/// Dependencies: WithRainShadowPrecipitationMap, OceanMask, PostProcessedHeightmap (from Stages 1+4)
/// Produces: PrecipitationFinal (THE final precipitation map used by erosion)
/// Performance: ~40-60ms for 512×512 map (BFS distance calculation)
///
/// Algorithm:
/// - BFS distance transform from ocean cells
/// - Exponential decay with distance (e^(-dist/30))
/// - Elevation resistance (mountains block maritime influence)
/// </remarks>
public class CoastalMoistureStage : IPipelineStage
{
    private readonly ILogger<CoastalMoistureStage> _logger;

    public string StageName => "Coastal Moisture";

    public CoastalMoistureStage(ILogger<CoastalMoistureStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.WithRainShadowPrecipitationMap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: CoastalMoistureStage requires WithRainShadowPrecipitationMap from RainShadowStage");
        }

        if (input.OceanMask == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: CoastalMoistureStage requires OceanMask from ElevationPostProcessStage");
        }

        if (input.PostProcessedHeightmap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: CoastalMoistureStage requires PostProcessedHeightmap from ElevationPostProcessStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 5{Suffix}: {StageName}", iterationSuffix, StageName);

        // Calculate coastal moisture enhancement
        var coastalMoistureResult = CoastalMoistureCalculator.Calculate(
            rainShadowPrecipitation: input.WithRainShadowPrecipitationMap,
            oceanMask: input.OceanMask,
            heightmap: input.PostProcessedHeightmap,
            width: input.WorldSize,
            height: input.WorldSize);

        _logger.LogInformation(
            "Stage 5{Suffix} complete: Coastal moisture enhancement applied (maritime vs continental climates)",
            iterationSuffix);

        // Return updated context with final precipitation
        return Result.Success(input.WithCoastalMoisture(coastalMoistureResult.FinalMap));
    }
}
