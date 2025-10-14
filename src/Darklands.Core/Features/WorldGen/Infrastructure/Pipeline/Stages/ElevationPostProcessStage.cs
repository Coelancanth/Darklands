using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;

/// <summary>
/// Stage 1: Elevation post-processing (VS_024).
/// Applies 4 WorldEngine algorithms + Gaussian smoothing + calculates adaptive thresholds.
/// </summary>
/// <remarks>
/// Dependencies: RawHeightmap (from Stage 0)
/// Produces: PostProcessedHeightmap, Thresholds, MinElevation, MaxElevation, OceanMask, SeaDepth, SeaLevelNormalized
/// Performance: ~50-100ms for 512×512 map
///
/// Algorithms:
/// 0. Gaussian blur (sigma=1.5) - Remove high-frequency noise
/// 1. add_noise_to_elevation() - Add coherent variation
/// 2. fill_ocean() - BFS flood fill for ocean detection
/// 3. harmonize_ocean() - Smooth ocean floor
/// 4. sea_depth() - Calculate depth map
/// 5. calculate_thresholds() - Quantile-based elevation bands
/// </remarks>
public class ElevationPostProcessStage : IPipelineStage
{
    private readonly ILogger<ElevationPostProcessStage> _logger;

    public string StageName => "Elevation Post-Processing";

    public ElevationPostProcessStage(ILogger<ElevationPostProcessStage> logger)
    {
        _logger = logger;
    }

    public Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0)
    {
        // Validate required dependencies
        if (input.RawHeightmap == null)
        {
            return Result.Failure<PipelineContext>(
                "ERROR_WORLDGEN_STAGE_MISSING_DEPENDENCY: ElevationPostProcessStage requires RawHeightmap from PlateGenerationStage");
        }

        var iterationSuffix = iterationIndex > 0 ? $" (iteration {iterationIndex})" : "";
        _logger.LogInformation("Stage 1{Suffix}: {StageName}", iterationSuffix, StageName);

        // Step 0.5: Gaussian smoothing (remove native plate noise)
        var smoothedHeightmap = (float[,])input.RawHeightmap.Clone();
        ElevationPostProcessor.ApplyGaussianBlur(smoothedHeightmap, sigma: 1.5f);

        _logger.LogInformation(
            "Stage 1{Suffix}.0: Gaussian smoothing applied (sigma=1.5, removes high-frequency noise)",
            iterationSuffix);

        // Step 1-4: WorldEngine post-processing algorithms
        var postProcessed = ElevationPostProcessor.Process(
            originalHeightmap: smoothedHeightmap,
            seed: input.Seed,
            addNoise: false);  // Noise disabled to isolate Gaussian blur effect

        _logger.LogInformation(
            "Stage 1{Suffix}.1-4: Elevation post-processing complete (4 WorldEngine algorithms applied)",
            iterationSuffix);

        // Step 5: Calculate adaptive thresholds
        var thresholds = CalculateElevationThresholds(
            postProcessed.ProcessedHeightmap,
            postProcessed.OceanMask);

        // Step 6: Calculate min/max for meters mapping
        var (minElevation, maxElevation) = GetMinMax(postProcessed.ProcessedHeightmap);

        _logger.LogInformation(
            "Stage 1{Suffix} complete: Thresholds (Hill={Hill:F2}, Mountain={Mountain:F2}, Peak={Peak:F2}), Range=[{Min:F2}, {Max:F2}]",
            iterationSuffix, thresholds.HillLevel, thresholds.MountainLevel, thresholds.PeakLevel,
            minElevation, maxElevation);

        // Return updated context
        return Result.Success(input.WithElevationProcessing(
            postProcessedHeightmap: postProcessed.ProcessedHeightmap,
            thresholds: thresholds,
            minElevation: minElevation,
            maxElevation: maxElevation,
            oceanMask: postProcessed.OceanMask,
            seaDepth: postProcessed.SeaDepth,
            seaLevelNormalized: postProcessed.SeaLevelNormalized));
    }

    /// <summary>
    /// Calculates quantile-based elevation thresholds from heightmap distribution.
    /// Copied from GenerateWorldPipeline (unchanged algorithm).
    /// </summary>
    private static ElevationThresholds CalculateElevationThresholds(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Collect land elevations for quantile calculation
        var landElevations = new List<float>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x])  // Land only
                {
                    landElevations.Add(heightmap[y, x]);
                }
            }
        }

        // Sort for quantile calculation
        landElevations.Sort();

        // Calculate adaptive thresholds (land features only)
        float hillLevel = GetPercentile(landElevations, 0.70f);     // 70th percentile
        float mountainLevel = GetPercentile(landElevations, 0.85f); // 85th percentile
        float peakLevel = GetPercentile(landElevations, 0.95f);     // 95th percentile

        return new ElevationThresholds(hillLevel, mountainLevel, peakLevel);
    }

    /// <summary>
    /// Gets the value at a given percentile from a sorted list.
    /// Copied from GenerateWorldPipeline (unchanged algorithm).
    /// </summary>
    private static float GetPercentile(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0)
            return 0f;

        int index = (int)Math.Floor(percentile * (sortedValues.Count - 1));
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));

        return sortedValues[index];
    }

    /// <summary>
    /// Gets the minimum and maximum values from a 2D heightmap.
    /// Copied from GenerateWorldPipeline (unchanged algorithm).
    /// </summary>
    private static (float min, float max) GetMinMax(float[,] heightmap)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = heightmap[y, x];
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        return (min, max);
    }
}
