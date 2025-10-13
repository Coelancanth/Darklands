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
        // STAGE 0.5: Gaussian Smoothing (tmp.md fix - removes native plate noise)
        // ═══════════════════════════════════════════════════════════════════════
        // Problem: Native C++ plate simulation outputs high-frequency noise (3.3% land sinks before pit-filling)
        // Solution: Low-pass filter via Gaussian blur (preserves mountains/valleys, removes micro-pits)
        // Target: Reduce PRE-filling sinks from 3.3% to <2.0% (healthy baseline for pit-filling)

        var smoothedHeightmap = (float[,])nativeResult.Value.Heightmap.Clone();
        ElevationPostProcessor.ApplyGaussianBlur(smoothedHeightmap, sigma: 1.5f);

        _logger.LogInformation(
            "Stage 0.5 complete: Gaussian smoothing applied (sigma=1.5, removes high-frequency noise from native plate simulation)");

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 1: Elevation Post-Processing (VS_024)
        // ═══════════════════════════════════════════════════════════════════════
        // WorldEngine algorithms: add_noise, fill_ocean, harmonize_ocean, sea_depth
        // Produces: PostProcessedHeightmap (raw [0-20]) + NormalizedHeightmap ([0,1]) + OceanMask + SeaDepth

        var postProcessed = ElevationPostProcessor.Process(
            originalHeightmap: smoothedHeightmap,  // Use smoothed instead of raw native output!
            seaLevel: parameters.SeaLevel,
            seed: parameters.Seed,
            addNoise: false);  // DIAGNOSTIC: Disabled to isolate Gaussian blur effect (re-enable after validation)

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
        // STAGE 2: Climate - Temperature (VS_025)
        // ═══════════════════════════════════════════════════════════════════════
        // WorldEngine algorithms: latitude+tilt, noise, distance-to-sun, mountain-cooling
        // Produces: 4 temperature maps (LatitudeOnly, WithNoise, WithDistance, Final) + per-world params

        var tempResult = Algorithms.TemperatureCalculator.Calculate(
            postProcessedHeightmap: postProcessed.ProcessedHeightmap,  // RAW [0.1-20] for mountain cooling
            mountainLevelThreshold: thresholds.MountainLevel,           // RAW threshold
            width: nativeResult.Value.Width,
            height: nativeResult.Value.Height,
            seed: parameters.Seed);

        _logger.LogInformation(
            "Stage 2 complete: Temperature calculated (axialTilt={AxialTilt:F3}, distanceToSun={DistanceToSun:F3})",
            tempResult.AxialTilt, tempResult.DistanceToSun);

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 3: Climate - Precipitation (VS_026)
        // ═══════════════════════════════════════════════════════════════════════
        // WorldEngine algorithm: noise + temperature gamma curve + renormalization
        // Produces: 3 precipitation maps (NoiseOnly, TemperatureShaped, Final) + quantile thresholds

        var precipResult = Algorithms.PrecipitationCalculator.Calculate(
            temperatureMap: tempResult.FinalMap,  // Stage 2 final temperature output
            width: nativeResult.Value.Width,
            height: nativeResult.Value.Height,
            seed: parameters.Seed);

        _logger.LogInformation(
            "Stage 3 complete: Precipitation calculated (thresholds: Low={Low:F3}, Med={Med:F3}, High={High:F3})",
            precipResult.Thresholds.LowThreshold, precipResult.Thresholds.MediumThreshold, precipResult.Thresholds.HighThreshold);

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 4: Rain Shadow Effect (VS_027)
        // ═══════════════════════════════════════════════════════════════════════
        // Orographic blocking: Mountains create leeward deserts based on latitude-dependent prevailing winds
        // Input: VS_026 final precipitation (base) + PostProcessedHeightmap + Thresholds
        // Output: WithRainShadowPrecipitationMap (leeward = dry, windward = unchanged for now)

        var rainShadowResult = Algorithms.RainShadowCalculator.Calculate(
            basePrecipitation: precipResult.FinalMap,
            elevation: postProcessed.ProcessedHeightmap,
            seaLevel: thresholds.SeaLevel,
            maxElevation: maxElevation,
            width: nativeResult.Value.Width,
            height: nativeResult.Value.Height);

        _logger.LogInformation(
            "Stage 4 complete: Rain shadow effect applied (latitude-based prevailing winds)");

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 5: Coastal Moisture Enhancement (VS_028)
        // ═══════════════════════════════════════════════════════════════════════
        // Distance-to-ocean BFS + exponential decay + elevation resistance
        // Produces: PrecipitationFinal (THE final precipitation map for erosion/rivers)

        var coastalMoistureResult = Algorithms.CoastalMoistureCalculator.Calculate(
            rainShadowPrecipitation: rainShadowResult.WithRainShadowMap,
            oceanMask: postProcessed.OceanMask!,
            heightmap: postProcessed.ProcessedHeightmap,
            width: nativeResult.Value.Width,
            height: nativeResult.Value.Height);

        _logger.LogInformation(
            "Stage 5 complete: Coastal moisture enhancement applied (maritime vs continental climates)");

        // ═══════════════════════════════════════════════════════════════════════
        // STAGE 6: Phase 1 Erosion - Flow Directions & Accumulation (VS_029)
        // ═══════════════════════════════════════════════════════════════════════
        // Pit filling + D-8 flow directions + topological sort + flow accumulation + river sources
        // Produces: Phase1ErosionData (foundation for river tracing visualization)

        // STEP 6a: Detect local minima BEFORE pit-filling (diagnostic baseline)
        var preFillingMinima = Algorithms.LocalMinimaDetector.Detect(
            postProcessed.ProcessedHeightmap,
            postProcessed.OceanMask!);

        _logger.LogInformation(
            "Pre-filling sinks detected: {Count} local minima ({Percentage:F1}% of land cells) - baseline for pit-filling validation",
            preFillingMinima.Count,
            CalculateLandPercentage(preFillingMinima.Count, postProcessed.ProcessedHeightmap, postProcessed.OceanMask!));

        // STEP 6b: Run Phase 1 erosion pipeline (includes pit-filling)
        var phase1Erosion = HydraulicErosionProcessor.ProcessPhase1(
            heightmap: postProcessed.ProcessedHeightmap,
            oceanMask: postProcessed.OceanMask!,
            precipitation: coastalMoistureResult.FinalMap,  // VS_028 final precipitation
            thresholds: thresholds,
            logger: _logger);

        // STEP 6c: Calculate pit-filling effectiveness (diagnostic comparison)
        var postFillingMinima = phase1Erosion.Lakes.Count;  // Lakes = preserved sinks after filling
        var sinkReductionPercent = preFillingMinima.Count > 0
            ? ((preFillingMinima.Count - postFillingMinima) / (float)preFillingMinima.Count) * 100f
            : 0f;

        _logger.LogInformation(
            "Stage 6 complete: Phase 1 erosion (pit-filling: {PreCount} -> {PostCount} sinks, {Reduction:F1}% reduction | rivers: {RiverCount} sources detected)",
            preFillingMinima.Count,
            postFillingMinima,
            sinkReductionPercent,
            phase1Erosion.RiverSources.Count);

        // ═══════════════════════════════════════════════════════════════════════
        // ASSEMBLE RESULT (VS_024: Dual-heightmap + thresholds architecture)
        // ═══════════════════════════════════════════════════════════════════════

        var result = new WorldGenerationResult(
            heightmap: nativeResult.Value.Heightmap,                       // ORIGINAL raw [0.1-20] (SACRED!)
            platesMap: nativeResult.Value.PlatesMap,
            rawNativeOutput: nativeResult.Value,
            postProcessedHeightmap: postProcessed.ProcessedHeightmap,      // Post-processed raw [0.1-20]
            thresholds: thresholds,                                        // Quantile-based thresholds
            minElevation: minElevation,                                    // Actual ocean floor (for meters mapping)
            maxElevation: maxElevation,                                    // Actual peak (for meters mapping)
            oceanMask: postProcessed.OceanMask,                            // Flood-filled ocean
            seaDepth: postProcessed.SeaDepth,                              // Depth map
            temperatureLatitudeOnly: tempResult.LatitudeOnlyMap,           // VS_025 Stage 1 (debug)
            temperatureWithNoise: tempResult.WithNoiseMap,                 // VS_025 Stage 2 (debug)
            temperatureWithDistance: tempResult.WithDistanceMap,           // VS_025 Stage 3 (debug)
            temperatureFinal: tempResult.FinalMap,                         // VS_025 Stage 4 (production)
            axialTilt: tempResult.AxialTilt,                               // Per-world param
            distanceToSun: tempResult.DistanceToSun,                       // Per-world param
            baseNoisePrecipitationMap: precipResult.NoiseOnlyMap,          // VS_026 Stage 1 (debug)
            temperatureShapedPrecipitationMap: precipResult.TemperatureShapedMap, // VS_026 Stage 2 (debug)
            finalPrecipitationMap: precipResult.FinalMap,                  // VS_026 Stage 3 (base precip)
            precipitationThresholds: precipResult.Thresholds,              // Quantile-based classification
            withRainShadowPrecipitationMap: rainShadowResult.WithRainShadowMap, // VS_027 Stage 4 (+ rain shadow)
            precipitationFinal: coastalMoistureResult.FinalMap,            // VS_028 Stage 5 (FINAL - for erosion!)
            precipitationMap: null,                                        // Deprecated (use finalPrecipitationMap)
            phase1Erosion: phase1Erosion,                                  // VS_029 Stage 6 (D-8 flow visualization)
            preFillingLocalMinima: preFillingMinima                        // VS_029 diagnostic (pit-filling baseline)
        );

        _logger.LogInformation(
            "Pipeline complete: {Width}x{Height} world with Stage 1 (elevation) + Stage 2 (temperature) + Stage 3 (precipitation) + Stage 4 (rain shadow) + Stage 5 (coastal moisture) + Stage 6 (erosion phase 1)",
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

    /// <summary>
    /// Calculates the percentage of land cells that a given count represents.
    /// Used for pit-filling diagnostics (VS_029).
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
