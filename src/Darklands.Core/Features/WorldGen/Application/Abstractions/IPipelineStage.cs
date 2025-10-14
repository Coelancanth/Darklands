using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Core.Features.WorldGen.Application.Abstractions;

/// <summary>
/// Represents a single stage in the world generation pipeline.
/// Each stage transforms input context and returns updated context (immutable flow).
/// </summary>
/// <remarks>
/// Design Principles (TD_027):
/// - Single Responsibility: Each stage wraps ONE algorithm/calculator
/// - Open/Closed: Add new stages without modifying orchestrators
/// - Functional: Stages are pure functions (input → output, no side effects except logging)
/// - Iteration-aware: iterationIndex parameter enables feedback loop logging
///
/// Architecture:
/// - Application/Abstractions: Interface definition (this file)
/// - Infrastructure/Pipeline/Stages: Concrete implementations (7 stages)
/// - Infrastructure/Pipeline: Orchestrators (SinglePassPipeline, IterativePipeline)
///
/// Example Stage Implementation:
/// <code>
/// public class TemperatureStage : IPipelineStage
/// {
///     public string StageName => "Temperature";
///
///     public Result&lt;PipelineContext&gt; Execute(PipelineContext input, int iterationIndex = 0)
///     {
///         if (input.PostProcessedHeightmap == null)
///             return Result.Failure&lt;PipelineContext&gt;("Temperature requires PostProcessedHeightmap");
///
///         var result = TemperatureCalculator.Calculate(...);
///         return Result.Success(input.WithTemperatureMap(result.FinalMap));
///     }
/// }
/// </code>
///
/// Usage in Orchestrator:
/// <code>
/// var stages = new IPipelineStage[] { elevationStage, temperatureStage, ... };
/// var context = new PipelineContext(seed, worldSize, ...);
///
/// foreach (var stage in stages)
/// {
///     var result = stage.Execute(context);
///     if (result.IsFailure) return Result.Failure(result.Error);
///     context = result.Value;
/// }
/// </code>
/// </remarks>
public interface IPipelineStage
{
    /// <summary>
    /// Human-readable stage name for logging and diagnostics.
    /// Examples: "Elevation Post-Processing", "Temperature", "Precipitation", "D-8 Flow"
    /// </summary>
    string StageName { get; }

    /// <summary>
    /// Executes the stage transformation on the input context.
    /// </summary>
    /// <param name="input">Current pipeline context (immutable input)</param>
    /// <param name="iterationIndex">
    /// Iteration index for feedback loops (0-based).
    /// - 0 = Initial pass or single-pass mode
    /// - 1+ = Subsequent iterations in iterative mode
    /// Used for logging (e.g., "Temperature Stage (iteration 2/5)")
    /// </param>
    /// <returns>
    /// Success: Updated context with stage output (immutable copy).
    /// Failure: ERROR_WORLDGEN_* translation key with reason.
    /// </returns>
    /// <remarks>
    /// Contract:
    /// - MUST check required input fields (return Failure if missing)
    /// - MUST return new context (immutable update, not mutation)
    /// - SHOULD log progress (use iterationIndex for loop-aware messages)
    /// - MAY log diagnostics (performance, data statistics, etc.)
    ///
    /// Error handling:
    /// - Missing required fields → "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY"
    /// - Algorithm failure → "ERROR_WORLDGEN_STAGE_FAILED" (with details)
    /// - Invalid data → "ERROR_WORLDGEN_STAGE_INVALID_INPUT"
    /// </remarks>
    Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0);
}
