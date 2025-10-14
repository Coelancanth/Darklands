using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 6: D-8 flow calculation (VS_029 Phase 1).
/// Computes pit filling, flow directions, flow accumulation, and river sources.
/// </summary>
/// <remarks>
/// Dependencies: PostProcessedHeightmap, OceanMask, PrecipitationFinal, Thresholds (from Stages 1+5)
/// Produces: Phase1Erosion, PreFillingLocalMinima
/// Performance: ~100-200ms for 512×512 map (pit filling dominates at O(n log n))
///
/// Algorithm Pipeline (VS_029):
/// 1a. Selective pit filling → FilledHeightmap, PreservedBasins
/// 1b. Flow direction computation → FlowDirections (D-8)
/// 1c. Topological sort → (internal, hydrologically correct order)
/// 1d. Flow accumulation → FlowAccumulation (drainage basins)
/// 1e. River source detection → RiverSources (major rivers only)
/// </remarks>
public class D8FlowStage : IPipelineStage
{
    private readonly ILogger<D8FlowStage> _logger;

    public string StageName => "D-8 Flow";

    public D8FlowStage(ILogger<D8FlowStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.PostProcessedHeightmap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: D8FlowStage requires PostProcessedHeightmap from ElevationPostProcessStage");
        }

        if (input.OceanMask == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: D8FlowStage requires OceanMask from ElevationPostProcessStage");
        }

        if (input.PrecipitationFinal == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: D8FlowStage requires PrecipitationFinal from CoastalMoistureStage");
        }

        if (input.Thresholds == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: D8FlowStage requires Thresholds from ElevationPostProcessStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 6{Suffix}: {StageName}", iterationSuffix, StageName);

        // Detect local minima BEFORE pit-filling (diagnostic baseline)
        var preFillingMinima = LocalMinimaDetector.Detect(
            input.PostProcessedHeightmap,
            input.OceanMask);

        _logger.LogInformation(
            "Pre-filling sinks detected: {Count} local minima ({Percentage:F1}% of land cells)",
            preFillingMinima.Count,
            CalculateLandPercentage(preFillingMinima.Count, input.PostProcessedHeightmap, input.OceanMask));

        // Run Phase 1 erosion pipeline (pit filling + D-8 flow)
        var phase1Erosion = HydraulicErosionProcessor.ProcessPhase1(
            heightmap: input.PostProcessedHeightmap,
            oceanMask: input.OceanMask,
            precipitation: input.PrecipitationFinal,
            thresholds: input.Thresholds,
            logger: _logger);

        // Calculate pit-filling effectiveness
        var postFillingMinima = phase1Erosion.PreservedBasins.Count;
        var sinkReductionPercent = preFillingMinima.Count > 0
            ? ((preFillingMinima.Count - postFillingMinima) / (float)preFillingMinima.Count) * 100f
            : 0f;

        _logger.LogInformation(
            "Stage 6{Suffix} complete: Phase 1 erosion (pit-filling: {PreCount} -> {PostCount} sinks, {Reduction:F1}% reduction | rivers: {RiverCount} sources)",
            iterationSuffix,
            preFillingMinima.Count,
            postFillingMinima,
            sinkReductionPercent,
            phase1Erosion.RiverSources.Count);

        // Return updated context with D-8 flow data
        return Result.Success(input.WithD8Flow(phase1Erosion, preFillingMinima));
    }

    /// <summary>
    /// Calculates the percentage of land cells that a given count represents.
    /// Copied from GenerateWorldPipeline (unchanged algorithm).
    /// </summary>
    private static float CalculateLandPercentage(int count, float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        int landCells = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x])
                    landCells++;
            }
        }

        return landCells > 0 ? (count / (float)landCells) * 100f : 0f;
    }
}
