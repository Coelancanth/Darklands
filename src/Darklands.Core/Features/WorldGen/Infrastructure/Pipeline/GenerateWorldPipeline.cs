using System;
using System.Collections.Generic;
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
        // STAGE 1: Elevation Post-Processing (VS_024)
        // ═══════════════════════════════════════════════════════════════════════
        // WorldEngine algorithms: add_noise, fill_ocean, harmonize_ocean, sea_depth
        // Produces: PostProcessedHeightmap (raw [0-20]) + NormalizedHeightmap ([0,1]) + OceanMask + SeaDepth

        var postProcessed = ElevationPostProcessor.Process(
            originalHeightmap: nativeResult.Value.Heightmap,  // Clone internally (preserves original!)
            seaLevel: parameters.SeaLevel,
            seed: parameters.Seed);

        _logger.LogInformation(
            "Stage 1 complete: Elevation post-processing (4 WorldEngine algorithms applied)");

        // Calculate quantile-based thresholds (adaptive to each world's terrain distribution)
        var thresholds = CalculateElevationThresholds(postProcessed.ProcessedHeightmap, postProcessed.OceanMask);

        // Calculate min/max for realistic meters mapping (fixes 50km ocean depth bug)
        var (minElevation, maxElevation) = GetMinMax(postProcessed.ProcessedHeightmap);

        _logger.LogInformation(
            "Stage 1 complete: Elevation thresholds calculated (SeaLevel={SeaLevel:F2}, HillLevel={HillLevel:F2}, MountainLevel={MountainLevel:F2}, PeakLevel={PeakLevel:F2}, Range=[{Min:F2}, {Max:F2}])",
            thresholds.SeaLevel, thresholds.HillLevel, thresholds.MountainLevel, thresholds.PeakLevel, minElevation, maxElevation);

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 2: Climate - Temperature (VS_025 - Future)
        // ═══════════════════════════════════════════════════════════════════════
        // TODO: Calculate temperature map (latitude + noise + elevation cooling)
        // Uses: raw PostProcessedHeightmap + MountainLevel threshold (WorldEngine approach)

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 3: Climate - Precipitation (Future)
        // ═══════════════════════════════════════════════════════════════════════
        // TODO: Calculate precipitation map (with rain shadow)

        // ═══════════════════════════════════════════════════════════════════════
        // ASSEMBLE RESULT (VS_024: Dual-heightmap + thresholds architecture)
        // ═══════════════════════════════════════════════════════════════════════

        var result = new WorldGenerationResult(
            heightmap: nativeResult.Value.Heightmap,                   // ORIGINAL raw [0.1-20] (SACRED!)
            platesMap: nativeResult.Value.PlatesMap,
            rawNativeOutput: nativeResult.Value,
            postProcessedHeightmap: postProcessed.ProcessedHeightmap,  // Post-processed raw [0.1-20]
            thresholds: thresholds,                                    // Quantile-based thresholds
            minElevation: minElevation,                                // Actual ocean floor (for meters mapping)
            maxElevation: maxElevation,                                // Actual peak (for meters mapping)
            oceanMask: postProcessed.OceanMask,                        // Flood-filled ocean
            seaDepth: postProcessed.SeaDepth,                          // Depth map
            temperatureMap: null,    // VS_025
            precipitationMap: null   // Stage 3
        );

        _logger.LogInformation(
            "Pipeline complete: {Width}x{Height} world with Stage 1 (original + post-processed + thresholds)",
            result.Width, result.Height);

        return Result.Success(result);
    }

    /// <summary>
    /// Calculates quantile-based elevation thresholds from heightmap distribution.
    /// Matches WorldEngine's adaptive approach - thresholds adjust to each world's terrain.
    /// </summary>
    /// <param name="heightmap">Post-processed heightmap (raw [0.1-20])</param>
    /// <param name="oceanMask">Ocean mask for separating land/ocean statistics</param>
    /// <returns>Elevation thresholds (SeaLevel, HillLevel, MountainLevel, PeakLevel)</returns>
    private static ElevationThresholds CalculateElevationThresholds(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Collect land elevations for quantile calculation
        var landElevations = new List<float>();
        var allElevations = new List<float>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float elevation = heightmap[y, x];
                allElevations.Add(elevation);

                if (!oceanMask[y, x])  // Land only
                {
                    landElevations.Add(elevation);
                }
            }
        }

        // Sort for quantile calculation
        landElevations.Sort();
        allElevations.Sort();

        // Calculate thresholds
        float seaLevel = GetPercentile(allElevations, 0.50f);       // 50th percentile overall (median)
        float hillLevel = GetPercentile(landElevations, 0.70f);     // 70th percentile of land
        float mountainLevel = GetPercentile(landElevations, 0.85f); // 85th percentile of land
        float peakLevel = GetPercentile(landElevations, 0.95f);     // 95th percentile of land

        return new ElevationThresholds(seaLevel, hillLevel, mountainLevel, peakLevel);
    }

    /// <summary>
    /// Gets the value at a given percentile from a sorted list.
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
    /// Used for realistic meters mapping in UI (prevents 50km ocean depths bug).
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
