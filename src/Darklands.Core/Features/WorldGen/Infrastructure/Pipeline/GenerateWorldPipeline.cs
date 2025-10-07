using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Orchestrates world generation pipeline stages.
/// Currently: Pass-through to native simulator (foundation only).
/// Future: Incremental post-processing stages (VS_022 phases).
/// </summary>
public class GenerateWorldPipeline : IWorldGenerationPipeline
{
    private readonly IPlateSimulator _nativeSimulator;
    private readonly ILogger<GenerateWorldPipeline> _logger;

    public GenerateWorldPipeline(
        IPlateSimulator nativeSimulator,
        ILogger<GenerateWorldPipeline> logger)
    {
        _nativeSimulator = nativeSimulator;
        _logger = logger;
    }

    public Result<WorldGenerationResult> Generate(PlateSimulationParams parameters)
    {
        _logger.LogInformation(
            "Starting world generation pipeline (seed: {Seed}, size: {Size}x{Size})",
            parameters.Seed, parameters.WorldSize, parameters.WorldSize);

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 0: Native Plate Tectonics Simulation
        // ═══════════════════════════════════════════════════════════════════════

        var nativeResult = _nativeSimulator.Generate(parameters);

        if (nativeResult.IsFailure)
        {
            _logger.LogError("Native simulation failed: {Error}", nativeResult.Error);
            return Result.Failure<WorldGenerationResult>(nativeResult.Error);
        }

        _logger.LogInformation(
            "Native simulation complete: {Width}x{Height} heightmap generated",
            nativeResult.Value.Width, nativeResult.Value.Height);

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 1: Post-Processing (Future - VS_022 Phase 1)
        // ═══════════════════════════════════════════════════════════════════════
        // TODO: Normalize elevation to [0, 1]
        // TODO: Calculate ocean mask (sea level threshold)

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 2: Climate - Temperature (Future - VS_022 Phase 2)
        // ═══════════════════════════════════════════════════════════════════════
        // TODO: Calculate temperature map (latitude + elevation cooling)

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 3: Climate - Precipitation (Future - VS_022 Phase 3)
        // ═══════════════════════════════════════════════════════════════════════
        // TODO: Calculate precipitation map (with rain shadow)

        // ═══════════════════════════════════════════════════════════════════════
        // ASSEMBLE RESULT (Currently: Pass-through with nulls)
        // ═══════════════════════════════════════════════════════════════════════

        var result = new WorldGenerationResult(
            heightmap: nativeResult.Value.Heightmap,      // Raw from native (Phase 1 will normalize)
            platesMap: nativeResult.Value.PlatesMap,
            rawNativeOutput: nativeResult.Value,
            oceanMask: null,         // TODO: Phase 1
            temperatureMap: null,    // TODO: Phase 2
            precipitationMap: null   // TODO: Phase 3
        );

        _logger.LogInformation(
            "Pipeline complete: {Width}x{Height} world generated (native-only, no post-processing)",
            result.Width, result.Height);

        return Result.Success(result);
    }
}
