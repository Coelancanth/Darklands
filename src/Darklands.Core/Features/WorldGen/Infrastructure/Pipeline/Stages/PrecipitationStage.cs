using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 3: Base precipitation calculation (VS_026).
/// Computes global precipitation distribution using noise + temperature shaping.
/// </summary>
/// <remarks>
/// Dependencies: TemperatureFinal (from Stage 2)
/// Produces: BaseNoisePrecipitationMap, TemperatureShapedPrecipitationMap, FinalPrecipitationMap, PrecipitationThresholds
/// Performance: ~20-40ms for 512×512 map
///
/// Algorithm Components:
/// 1. Base noise (6 octaves) - Random wet/dry patterns
/// 2. Temperature gamma curve - Cold = less evaporation
/// 3. Renormalization - Restore full dynamic range
/// </remarks>
public class PrecipitationStage : IPipelineStage
{
    private readonly ILogger<PrecipitationStage> _logger;

    public string StageName => "Precipitation";

    public PrecipitationStage(ILogger<PrecipitationStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.TemperatureFinal == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: PrecipitationStage requires TemperatureFinal from TemperatureStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 3{Suffix}: {StageName}", iterationSuffix, StageName);

        // Calculate precipitation maps (3-stage WorldEngine algorithm)
        var precipResult = PrecipitationCalculator.Calculate(
            temperatureMap: input.TemperatureFinal,
            width: input.WorldSize,
            height: input.WorldSize,
            seed: input.Seed);

        _logger.LogInformation(
            "Stage 3{Suffix} complete: Precipitation calculated (thresholds: Low={Low:F3}, Med={Med:F3}, High={High:F3})",
            iterationSuffix,
            precipResult.Thresholds.LowThreshold,
            precipResult.Thresholds.MediumThreshold,
            precipResult.Thresholds.HighThreshold);

        // Return updated context with base precipitation data
        return Result.Success(input.WithBasePrecipitation(
            baseNoise: precipResult.NoiseOnlyMap,
            temperatureShaped: precipResult.TemperatureShapedMap,
            final: precipResult.FinalMap,
            thresholds: precipResult.Thresholds));
    }
}
