using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 2: Temperature calculation (VS_025).
/// Computes global temperature distribution using 4-component WorldEngine algorithm.
/// </summary>
/// <remarks>
/// Dependencies: PostProcessedHeightmap, Thresholds (from Stage 1)
/// Produces: TemperatureLatitudeOnly, TemperatureWithNoise, TemperatureWithDistance, TemperatureFinal, AxialTilt, DistanceToSun
/// Performance: ~20-40ms for 512×512 map
///
/// Algorithm Components:
/// 1. Latitude factor (92% weight) - with axial tilt
/// 2. Coherent noise (8% weight) - climate variation
/// 3. Distance to sun - per-world hot/cold multiplier
/// 4. Mountain cooling - elevation-based temperature drop
/// </remarks>
public class TemperatureStage : IPipelineStage
{
    private readonly ILogger<TemperatureStage> _logger;

    public string StageName => "Temperature";

    public TemperatureStage(ILogger<TemperatureStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.PostProcessedHeightmap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: TemperatureStage requires PostProcessedHeightmap from ElevationPostProcessStage");
        }

        if (input.Thresholds == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: TemperatureStage requires Thresholds from ElevationPostProcessStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 2{Suffix}: {StageName}", iterationSuffix, StageName);

        // Calculate temperature maps (4-stage WorldEngine algorithm)
        var tempResult = TemperatureCalculator.Calculate(
            postProcessedHeightmap: input.PostProcessedHeightmap,
            mountainLevelThreshold: input.Thresholds.MountainLevel,
            width: input.WorldSize,
            height: input.WorldSize,
            seed: input.Seed);

        _logger.LogInformation(
            "Stage 2{Suffix} complete: Temperature calculated (axialTilt={Tilt:F3}, distanceToSun={Distance:F3})",
            iterationSuffix, tempResult.AxialTilt, tempResult.DistanceToSun);

        // Return updated context with temperature data
        return Result.Success(input.WithTemperature(
            latitudeOnly: tempResult.LatitudeOnlyMap,
            withNoise: tempResult.WithNoiseMap,
            withDistance: tempResult.WithDistanceMap,
            final: tempResult.FinalMap,
            axialTilt: tempResult.AxialTilt,
            distanceToSun: tempResult.DistanceToSun));
    }
}
