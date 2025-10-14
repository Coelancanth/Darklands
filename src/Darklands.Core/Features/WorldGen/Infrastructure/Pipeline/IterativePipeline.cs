using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Iterative pipeline orchestrator (TD_027): High-quality mode with feedback loops.
/// Stage order: Foundation → Loop { Erosion → Climate } × N → Analysis
/// Performance: ~6-10s for 512×512 map (3-5 iterations)
/// </summary>
/// <remarks>
/// Pipeline Architecture:
/// ┌──────────────────────────────────────────────────────┐
/// │ FOUNDATION STAGES (run once)                        │
/// ├──────────────────────────────────────────────────────┤
/// │ 0. Plate Generation   → Raw heightmap              │
/// │ 1. Elevation Process  → Post-processed heightmap    │
/// └──────────────────────────────────────────────────────┘
///         ↓
/// ┌──────────────────────────────────────────────────────┐
/// │ FEEDBACK LOOP (repeat N times)                      │
/// ├──────────────────────────────────────────────────────┤
/// │ ╔════════════════════════════════════════════════╗  │
/// │ ║ Iteration 1, 2, 3, ... N                       ║  │
/// │ ║─────────────────────────────────────────────────║  │
/// │ ║ 2. Temperature      → Climate (responds to     ║  │
/// │ ║                        eroded terrain)          ║  │
/// │ ║ 3. Precipitation    → Base precipitation        ║  │
/// │ ║ 4. Rain Shadow      → Orographic blocking       ║  │
/// │ ║                        (improves each iteration)║  │
/// │ ║ 5. Coastal Moisture → Maritime influence        ║  │
/// │ ║ 6. D-8 Flow         → Erosion (modifies terrain║  │
/// │ ║                        for next iteration)      ║  │
/// │ ╚════════════════════════════════════════════════╝  │
/// │                     ↓ Loop back                     │
/// └──────────────────────────────────────────────────────┘
///
/// Feedback Loop Mechanics:
/// - Iteration 1: Initial climate → initial erosion
/// - Iteration 2: Eroded terrain → updated rain shadows → refined erosion
/// - Iteration 3+: Convergence (diminishing changes)
///
/// Convergence Behavior:
/// - Iterations 1-2: Significant changes (climate adapts to erosion)
/// - Iterations 3-4: Stabilization (minor refinements)
/// - Iteration 5+: Diminishing returns (<5% change)
///
/// Trade-Offs:
/// ✅ Pros: Maximum quality, converges to equilibrium, physically accurate
/// ❌ Cons: Slower (6-10s), complex orchestration, overkill for preview
///
/// Use Cases:
/// - Final production worlds (maximum fidelity)
/// - Research/experimentation (study feedback dynamics)
/// - Content validation (verify quality standards)
///
/// Comparison to SinglePass:
/// - Stage Order: Erosion → Climate loop (vs Climate → Erosion once)
/// - Iterations: 3-5 passes (vs 1 pass)
/// - Convergence: Iterative refinement (vs none)
/// </remarks>
public class IterativePipeline : IWorldGenerationPipeline
{
    private readonly IPipelineStage _plateGenerationStage;
    private readonly IPipelineStage _elevationPostProcessStage;
    private readonly IEnumerable<IPipelineStage> _feedbackStages;  // Stages 2-6 (Temperature → D8Flow)
    private readonly ILogger<IterativePipeline> _logger;

    /// <summary>
    /// Creates iterative pipeline with foundation + feedback loop stages.
    /// </summary>
    /// <param name="plateGenerationStage">Stage 0: Plate tectonics (run once)</param>
    /// <param name="elevationPostProcessStage">Stage 1: Elevation processing (run once)</param>
    /// <param name="feedbackStages">
    /// Stages 2-6 in loop order:
    /// [Temperature, Precipitation, RainShadow, CoastalMoisture, D8Flow]
    /// These stages repeat N times for feedback convergence.
    /// </param>
    /// <param name="logger">Logger for pipeline orchestration</param>
    public IterativePipeline(
        IPipelineStage plateGenerationStage,
        IPipelineStage elevationPostProcessStage,
        IEnumerable<IPipelineStage> feedbackStages,
        ILogger<IterativePipeline> logger)
    {
        _plateGenerationStage = plateGenerationStage;
        _elevationPostProcessStage = elevationPostProcessStage;
        _feedbackStages = feedbackStages;
        _logger = logger;
    }

    public Result<WorldGenerationResult> Generate(PlateSimulationParams parameters)
    {
        _logger.LogInformation(
            "Starting IterativePipeline (seed: {Seed}, size: {Size}x{Size}, iterations: {Iterations}, mode: High Quality)",
            parameters.Seed, parameters.WorldSize, parameters.WorldSize, parameters.FeedbackIterations);

        // Initialize pipeline context with configuration
        var context = new PipelineContext(
            seed: parameters.Seed,
            worldSize: parameters.WorldSize,
            plateCount: parameters.PlateCount,
            feedbackIterations: parameters.FeedbackIterations);

        // ═══════════════════════════════════════════════════════════════════════
        // FOUNDATION STAGES (run once, never repeat)
        // ═══════════════════════════════════════════════════════════════════════

        // Stage 0: Plate Generation
        var result = _plateGenerationStage.Execute(context, iterationIndex: 0);
        if (result.IsFailure)
        {
            _logger.LogError("Pipeline failed at foundation stage '{StageName}': {Error}",
                _plateGenerationStage.StageName, result.Error);
            return Result.Failure<WorldGenerationResult>(result.Error);
        }
        context = result.Value;

        // Stage 1: Elevation Post-Processing
        result = _elevationPostProcessStage.Execute(context, iterationIndex: 0);
        if (result.IsFailure)
        {
            _logger.LogError("Pipeline failed at foundation stage '{StageName}': {Error}",
                _elevationPostProcessStage.StageName, result.Error);
            return Result.Failure<WorldGenerationResult>(result.Error);
        }
        context = result.Value;

        _logger.LogInformation("Foundation stages complete (Stages 0-1)");

        // ═══════════════════════════════════════════════════════════════════════
        // FEEDBACK LOOP (repeat N times for convergence)
        // ═══════════════════════════════════════════════════════════════════════

        for (int iteration = 1; iteration <= parameters.FeedbackIterations; iteration++)
        {
            _logger.LogInformation(
                "═══ Feedback Iteration {Iteration}/{Total} ═══",
                iteration, parameters.FeedbackIterations);

            // Execute all feedback stages (Stages 2-6: Temperature → D8Flow)
            foreach (var stage in _feedbackStages)
            {
                result = stage.Execute(context, iterationIndex: iteration);

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "Pipeline failed at iteration {Iteration} stage '{StageName}': {Error}",
                        iteration, stage.StageName, result.Error);
                    return Result.Failure<WorldGenerationResult>(result.Error);
                }

                context = result.Value;
            }

            _logger.LogInformation(
                "Feedback iteration {Iteration}/{Total} complete (Stages 2-6 executed)",
                iteration, parameters.FeedbackIterations);

            // TODO (Future): Convergence detection
            // - Compare iteration N vs N-1 (e.g., precipitation map difference)
            // - Early exit if change < 5% (diminishing returns)
            // - Log convergence metrics for research
        }

        _logger.LogInformation(
            "IterativePipeline complete: {Width}x{Height} world generated ({Iterations} iterations, ~{Time}s)",
            context.WorldSize, context.WorldSize, parameters.FeedbackIterations,
            parameters.FeedbackIterations * 2);  // Rough estimate: 2s per iteration

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
