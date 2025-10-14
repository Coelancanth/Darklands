using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Single-pass pipeline orchestrator (TD_027): Fast preview mode.
/// Stage order: Foundation → Feedback(single pass) → Analysis
/// Performance: ~2s for 512×512 map
/// </summary>
/// <remarks>
/// Pipeline Architecture:
/// ┌──────────────────────────────────────────────────────┐
/// │ FOUNDATION STAGES (always run once)                 │
/// ├──────────────────────────────────────────────────────┤
/// │ 0. Plate Generation   → Raw heightmap              │
/// │ 1. Elevation Process  → Post-processed heightmap    │
/// └──────────────────────────────────────────────────────┘
///         ↓
/// ┌──────────────────────────────────────────────────────┐
/// │ FEEDBACK STAGES (single pass - Climate → Erosion)  │
/// ├──────────────────────────────────────────────────────┤
/// │ 2. Temperature        → Climate baseline            │
/// │ 3. Precipitation      → Base precipitation          │
/// │ 4. Rain Shadow        → + Orographic blocking       │
/// │ 5. Coastal Moisture   → + Maritime influence       │
/// │ 6. D-8 Flow           → + Erosion effects          │
/// └──────────────────────────────────────────────────────┘
///
/// Trade-Offs:
/// ✅ Pros: Fast (2s), good approximation, real-time iteration
/// ❌ Cons: No feedback convergence (climate fixed, erosion one-shot)
///
/// Use Cases:
/// - Development/testing (fast feedback loops)
/// - World preview (quick iteration)
/// - Non-critical content (placeholder worlds)
///
/// Comparison to Iterative:
/// - Stage Order: Climate BEFORE erosion (vs Erosion → Climate loop)
/// - Iterations: 1 pass (vs 3-5 iterations)
/// - Convergence: None (vs iterative refinement)
/// </remarks>
public class SinglePassPipeline : IWorldGenerationPipeline
{
    private readonly IEnumerable<IPipelineStage> _stages;
    private readonly ILogger<SinglePassPipeline> _logger;

    /// <summary>
    /// Creates single-pass pipeline with ordered stage sequence.
    /// </summary>
    /// <param name="stages">
    /// Stages in execution order:
    /// [PlateGen, ElevationProcess, Temperature, Precipitation, RainShadow, CoastalMoisture, D8Flow]
    /// </param>
    /// <param name="logger">Logger for pipeline orchestration</param>
    public SinglePassPipeline(
        IEnumerable<IPipelineStage> stages,
        ILogger<SinglePassPipeline> logger)
    {
        _stages = stages;
        _logger = logger;
    }

    public Result<WorldGenerationResult> Generate(PlateSimulationParams parameters)
    {
        _logger.LogInformation(
            "Starting SinglePassPipeline (seed: {Seed}, size: {Size}x{Size}, mode: Fast Preview)",
            parameters.Seed, parameters.WorldSize, parameters.WorldSize);

        // Initialize pipeline context with configuration
        var context = new PipelineContext(
            seed: parameters.Seed,
            worldSize: parameters.WorldSize,
            plateCount: parameters.PlateCount,
            feedbackIterations: 1);  // Single-pass mode (no iteration)

        // Execute all stages in sequence (Foundation → Feedback → Analysis)
        foreach (var stage in _stages)
        {
            var result = stage.Execute(context, iterationIndex: 0);

            if (result.IsFailure)
            {
                _logger.LogError("Pipeline failed at stage '{StageName}': {Error}", stage.StageName, result.Error);
                return Result.Failure<WorldGenerationResult>(result.Error);
            }

            context = result.Value;
        }

        _logger.LogInformation(
            "SinglePassPipeline complete: {Width}x{Height} world generated (1 pass, ~2s)",
            context.WorldSize, context.WorldSize);

        // Assemble final result from pipeline context
        return Result.Success(AssembleResult(context));
    }

    /// <summary>
    /// Assembles WorldGenerationResult from completed pipeline context.
    /// Maps PipelineContext fields to WorldGenerationResult constructor.
    /// </summary>
    private static WorldGenerationResult AssembleResult(PipelineContext context)
    {
        // All fields should be populated by stages (validation happens in stages)
        return new WorldGenerationResult(
            heightmap: context.RawHeightmap!,
            platesMap: context.PlatesMap!,
            rawNativeOutput: context.RawNativeOutput!,
            postProcessedHeightmap: context.PostProcessedHeightmap,
            thresholds: context.Thresholds,
            minElevation: context.MinElevation,
            maxElevation: context.MaxElevation,
            oceanMask: context.OceanMask,
            seaDepth: context.SeaDepth,
            seaLevelNormalized: context.SeaLevelNormalized,
            temperatureLatitudeOnly: context.TemperatureLatitudeOnly,
            temperatureWithNoise: context.TemperatureWithNoise,
            temperatureWithDistance: context.TemperatureWithDistance,
            temperatureFinal: context.TemperatureFinal,
            axialTilt: context.AxialTilt,
            distanceToSun: context.DistanceToSun,
            baseNoisePrecipitationMap: context.BaseNoisePrecipitationMap,
            temperatureShapedPrecipitationMap: context.TemperatureShapedPrecipitationMap,
            finalPrecipitationMap: context.FinalPrecipitationMap,
            precipitationThresholds: context.PrecipitationThresholds,
            withRainShadowPrecipitationMap: context.WithRainShadowPrecipitationMap,
            precipitationFinal: context.PrecipitationFinal,
            precipitationMap: null,  // Deprecated field
            phase1Erosion: context.Phase1Erosion,
            preFillingLocalMinima: context.PreFillingLocalMinima);
    }
}
