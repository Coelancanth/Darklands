using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 4: Rain shadow effect (VS_027).
/// Applies orographic blocking - mountains create leeward deserts based on prevailing winds.
/// </summary>
/// <remarks>
/// Dependencies: FinalPrecipitationMap, PostProcessedHeightmap, MaxElevation (from Stages 1+3)
/// Produces: WithRainShadowPrecipitationMap
/// Performance: ~30-50ms for 512×512 map
///
/// Algorithm:
/// - Latitude-dependent prevailing winds (trade winds, westerlies, polar easterlies)
/// - Ray-casting for orographic blocking detection
/// - Leeward precipitation reduction based on mountain height
/// </remarks>
public class RainShadowStage : IPipelineStage
{
    private readonly ILogger<RainShadowStage> _logger;

    public string StageName => "Rain Shadow";

    public RainShadowStage(ILogger<RainShadowStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.FinalPrecipitationMap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: RainShadowStage requires FinalPrecipitationMap from PrecipitationStage");
        }

        if (input.PostProcessedHeightmap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: RainShadowStage requires PostProcessedHeightmap from ElevationPostProcessStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 4{Suffix}: {StageName}", iterationSuffix, StageName);

        // Calculate rain shadow effect
        var rainShadowResult = RainShadowCalculator.Calculate(
            basePrecipitation: input.FinalPrecipitationMap,
            elevation: input.PostProcessedHeightmap,
            seaLevel: WorldGenConstants.SEA_LEVEL_RAW,
            maxElevation: input.MaxElevation,
            width: input.WorldSize,
            height: input.WorldSize);

        _logger.LogInformation(
            "Stage 4{Suffix} complete: Rain shadow effect applied (latitude-based prevailing winds)",
            iterationSuffix);

        // Return updated context with rain shadow data
        return Result.Success(input.WithRainShadow(rainShadowResult.WithRainShadowMap));
    }
}
