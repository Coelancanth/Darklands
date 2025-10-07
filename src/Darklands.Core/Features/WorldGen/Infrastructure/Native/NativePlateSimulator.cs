using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native;

/// <summary>
/// Implementation of IPlateSimulator using native plate-tectonics library.
/// LAYER 2 (Wrapper): Manages native lifecycle, marshaling, and error handling.
/// See ADR-007: Native Library Integration Architecture
/// </summary>
public class NativePlateSimulator : IPlateSimulator
{
    private readonly ILogger<NativePlateSimulator> _logger;
    private readonly string _projectPath;

    public NativePlateSimulator(ILogger<NativePlateSimulator> logger, string projectPath)
    {
        _logger = logger;
        _projectPath = projectPath;
    }

    public Result<PlateSimulationResult> Generate(PlateSimulationParams parameters)
    {
        _logger.LogInformation(
            "Generating world (seed: {Seed}, size: {Size}x{Size}, plates: {Plates})",
            parameters.Seed, parameters.WorldSize, parameters.WorldSize, parameters.PlateCount);

        _logger.LogDebug("Step 1: Validating library exists at path: {ProjectPath}", _projectPath);

        return EnsureLibraryLoaded()
            .Tap(() => _logger.LogDebug("Step 1 complete: Library validation succeeded"))
            .TapError(error => _logger.LogError("Step 1 FAILED: Library validation error: {Error}", error))
            .Bind(() =>
            {
                _logger.LogDebug("Step 2: Running native simulation");
                return RunNativeSimulation(parameters);
            })
            .Tap(_ => _logger.LogDebug("Step 2 complete: Native simulation succeeded"))
            .TapError(error => _logger.LogError("Step 2 FAILED: Native simulation error: {Error}", error))
            .Bind(nativeOut =>
            {
                _logger.LogDebug("Step 3: Post-processing elevation");
                return PostProcessElevation(nativeOut.Heightmap, parameters)
                    .Map(elev => (elev, nativeOut));
            })
            .Tap(_ => _logger.LogDebug("Step 3 complete: Elevation post-processing succeeded"))
            .TapError(error => _logger.LogError("Step 3 FAILED: Elevation post-processing error: {Error}", error))
            .Bind(tuple1 =>
            {
                _logger.LogDebug("Step 4: Calculating climate");
                return CalculateClimate(tuple1.elev, parameters)
                    .Map(climate => (climate, tuple1.elev, tuple1.nativeOut)); // Keep elevation stage
            })
            .Tap(_ => _logger.LogDebug("Step 4 complete: Climate calculation succeeded"))
            .TapError(error => _logger.LogError("Step 4 FAILED: Climate calculation error: {Error}", error))
            .Bind(tuple2 =>
            {
                _logger.LogDebug("Step 5: Simulating hydraulic erosion (rivers, lakes, valleys)");
                return SimulateErosion(tuple2.climate, parameters)
                    .Map(erosion => (erosion, tuple2.climate, tuple2.elev, tuple2.nativeOut)); // Keep all stages
            })
            .Tap(_ => _logger.LogDebug("Step 5 complete: Hydraulic erosion succeeded"))
            .TapError(error => _logger.LogError("Step 5 FAILED: Hydraulic erosion error: {Error}", error))
            .Bind(tuple3 =>
            {
                _logger.LogDebug("Step 6: Simulating hydrology (watermap, irrigation, humidity)");
                return SimulateHydrology(tuple3.erosion, parameters)
                    .Map(hydrology => (hydrology, tuple3.erosion, tuple3.climate, tuple3.elev, tuple3.nativeOut)); // Keep all stages
            })
            .Tap(_ => _logger.LogDebug("Step 6 complete: Hydrology simulation succeeded"))
            .TapError(error => _logger.LogError("Step 6 FAILED: Hydrology simulation error: {Error}", error))
            .Map(tuple4 =>
            {
                _logger.LogDebug("Step 7: Classifying biomes");

                var result = ClassifyBiomes(
                    tuple4.hydrology,
                    tuple4.nativeOut.Heightmap,
                    tuple4.nativeOut.Plates,
                    // Pass pipeline stages for debugging
                    elevationStage: tuple4.elev,
                    climateStage: tuple4.climate,
                    erosionStage: tuple4.erosion,
                    hydrologyStage: tuple4.hydrology);

                _logger.LogDebug("Step 7 complete: Biome classification succeeded");
                return result;
            });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.1: Native Simulation & Marshaling
    // ═══════════════════════════════════════════════════════════════════════

    private Result EnsureLibraryLoaded() =>
        NativeLibraryLoader.ValidateLibraryExists(_projectPath);

    private Result<NativeOut> RunNativeSimulation(PlateSimulationParams p)
    {
        try
        {
            _logger.LogDebug("Creating native simulation handle with params: seed={Seed}, size={Size}, plates={Plates}",
                p.Seed, p.WorldSize, p.PlateCount);

            var handle = PlateTectonicsNative.Create(
                seed: p.Seed,
                width: (uint)p.WorldSize,
                height: (uint)p.WorldSize,
                seaLevel: p.SeaLevel,
                erosionPeriod: (uint)p.ErosionPeriod,
                foldingRatio: p.FoldingRatio,
                aggrOverlapAbs: (uint)p.AggrOverlapAbs,
                aggrOverlapRel: p.AggrOverlapRel,
                cycleCount: (uint)p.CycleCount,
                numPlates: (uint)p.PlateCount);

            _logger.LogDebug("Native Create() returned handle: {Handle}", handle);

            if (handle == IntPtr.Zero)
            {
                _logger.LogError("Native Create() returned NULL handle");
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_SIMULATION_FAILED");
            }

            using var safeHandle = new PlateSimulationHandle(handle);

            // Step until simulation completes
            int stepCount = 0;
            const int maxSteps = 10000;

            // Optional frame capture controls via environment variables
            bool captureFrames = string.Equals(Environment.GetEnvironmentVariable("PLATEC_CAPTURE_FRAMES"), "1", StringComparison.OrdinalIgnoreCase);
            int captureEvery = int.TryParse(Environment.GetEnvironmentVariable("PLATEC_CAPTURE_EVERY"), out var ce) && ce > 0 ? ce : 1;
            string captureDir = Environment.GetEnvironmentVariable("PLATEC_CAPTURE_DIR") ?? System.IO.Path.Combine("logs", "platec_frames", $"seed_{p.Seed}_size_{p.WorldSize}");

            while (PlateTectonicsNative.IsFinished(handle) == 0 && stepCount < maxSteps)
            {
                PlateTectonicsNative.Step(handle);
                stepCount++;

                if (captureFrames && (stepCount % captureEvery == 0))
                {
                    try
                    {
                        var framePtr = PlateTectonicsNative.GetHeightmap(handle);
                        if (framePtr != IntPtr.Zero)
                        {
                            System.IO.Directory.CreateDirectory(captureDir);
                            var framePath = System.IO.Path.Combine(captureDir, $"frame_{stepCount:0000}.pgm");
                            WriteHeightmapAsPGM(framePtr, p.WorldSize, p.WorldSize, framePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to capture frame at step {Step}", stepCount);
                    }
                }
            }

            if (stepCount >= maxSteps)
            {
                _logger.LogWarning("Simulation reached max steps ({MaxSteps})", maxSteps);
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_SIMULATION_TIMEOUT");
            }

            _logger.LogInformation("Simulation completed in {Steps} steps", stepCount);

            // Marshal heightmap to C# 2D array
            var heightmapPtr = PlateTectonicsNative.GetHeightmap(handle);
            if (heightmapPtr == IntPtr.Zero)
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_MARSHALING_FAILED");

            var heightmap = Marshal2DArray(heightmapPtr, p.WorldSize, p.WorldSize);

            // Plates map
            var platesPtr = PlateTectonicsNative.GetPlatesMap(handle);
            var plates = Marshal2DArrayUInt(platesPtr, p.WorldSize, p.WorldSize);

            return Result.Success(new NativeOut(heightmap, plates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Native simulation exception: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            _logger.LogError("Exception stack trace: {StackTrace}", ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerType} - {InnerMessage}",
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }

            return Result.Failure<NativeOut>($"ERROR_WORLDGEN_NATIVE_EXCEPTION: {ex.Message}");
        }
    }

    /// <summary>
    /// Marshals native 1D float array to C# 2D array using Span for performance.
    /// Pattern from ADR-007 v1.2.
    /// </summary>
    private static unsafe float[,] Marshal2DArray(IntPtr ptr, int width, int height)
    {
        var result = new float[height, width];
        var span = new Span<float>(ptr.ToPointer(), width * height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = span[y * width + x];

        return result;
    }

    private static unsafe uint[,] Marshal2DArrayUInt(IntPtr ptr, int width, int height)
    {
        var result = new uint[height, width];
        var span = new Span<uint>(ptr.ToPointer(), width * height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = span[y * width + x];

        return result;
    }

    /// <summary>
    /// Writes a native heightmap (float*) to an 8-bit binary PGM image (P5).
    /// </summary>
    private static unsafe void WriteHeightmapAsPGM(IntPtr floatPtr, int width, int height, string filePath)
    {
        var span = new Span<float>(floatPtr.ToPointer(), width * height);
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < span.Length; i++) { var v = span[i]; if (v < min) min = v; if (v > max) max = v; }
        float delta = Math.Max(1e-6f, max - min);

        using var fs = System.IO.File.Create(filePath);
        var header = System.Text.Encoding.ASCII.GetBytes($"P5\n{width} {height}\n255\n");
        fs.Write(header, 0, header.Length);

        // Stream rows to avoid large allocations
        var row = new byte[width];
        for (int y = 0; y < height; y++)
        {
            int o = y * width;
            for (int x = 0; x < width; x++)
            {
                float v = span[o + x];
                int b = (int)Math.Clamp(((v - min) / delta) * 255f, 0f, 255f);
                row[x] = (byte)b;
            }
            fs.Write(row, 0, row.Length);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.2: Elevation Post-Processing
    // ═══════════════════════════════════════════════════════════════════════

    private Result<ElevationData> PostProcessElevation(float[,] rawHeightmap, PlateSimulationParams p)
    {
        _logger.LogDebug("Post-processing elevation: borders, noise, ocean flood fill");

        try
        {
            // Make a copy to avoid modifying raw data
            var heightmap = (float[,])rawHeightmap.Clone();

            // 1. Lower elevation at map borders for realistic coastlines
            ElevationPostProcessor.PlaceOceansAtBorders(heightmap, borderReduction: 0.8f);

            // 2. Add Perlin noise for terrain variation (reduced amplitude to avoid top-heavy land)
            ElevationPostProcessor.AddNoise(heightmap, p.Seed, scale: 0.05f, amplitude: 0.06f);

            // 3. Flood fill from borders to mark ocean cells
            var oceanMask = ElevationPostProcessor.FillOcean(heightmap, p.SeaLevel);

            // 3b. Harmonize ocean floor elevations (WorldEngine parity)
            ElevationPostProcessor.HarmonizeOcean(heightmap, oceanMask, p.SeaLevel);

            // Diagnostics: ocean vs land counts and elevation ranges
            int oceanCount = 0, landCount = 0;
            float minLand = float.PositiveInfinity, maxLand = float.NegativeInfinity;
            float minSea = float.PositiveInfinity, maxSea = float.NegativeInfinity;
            int h = heightmap.GetLength(0), w = heightmap.GetLength(1);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float e = heightmap[y, x];
                    if (oceanMask[y, x])
                    {
                        oceanCount++; if (e < minSea) minSea = e; if (e > maxSea) maxSea = e;
                    }
                    else
                    {
                        landCount++; if (e < minLand) minLand = e; if (e > maxLand) maxLand = e;
                    }
                }
            }
            _logger.LogInformation("PostProcess: seaLevel={SeaLevel:F3} ocean={Ocean} land={Land} sea[min={MinS:F3},max={MaxS:F3}] land[min={MinL:F3},max={MaxL:F3}]",
                p.SeaLevel, oceanCount, landCount, minSea, maxSea, minLand, maxLand);

            // 4. Normalize land elevation distribution (correct skew to high mountains):
            //    First gamma compress, then rank-based remap for stronger effect.
            ElevationPostProcessor.NormalizeLandDistribution(heightmap, oceanMask, gamma: 1.8f);
            ElevationPostProcessor.RemapLandByRank(heightmap, oceanMask, alpha: 1.6f);

            _logger.LogInformation("Elevation post-processing complete");

            return Result.Success(new ElevationData(heightmap, oceanMask));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elevation post-processing failed");
            return Result.Failure<ElevationData>("ERROR_WORLDGEN_POSTPROCESSING_FAILED");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.3: Climate Calculation
    // ═══════════════════════════════════════════════════════════════════════

    private Result<ClimateData> CalculateClimate(ElevationData elevation, PlateSimulationParams p)
    {
        _logger.LogDebug("Calculating climate: precipitation, temperature");

        try
        {
            // Enable algorithm tracing (logger will be used for sample cells + summary stats)
            ClimateCalculator.SetLogger(_logger);

            // Calculate temperature (latitude + elevation cooling + noise variation)
            var temperature = ClimateCalculator.CalculateTemperature(
                elevation.Heightmap,
                elevation.OceanMask,
                p.Seed);

            // Calculate precipitation AFTER temperature to apply gamma curve
            var precipitation = ClimateCalculator.CalculatePrecipitation(
                elevation.Heightmap,
                elevation.OceanMask,
                temperature,
                p.Seed);

            _logger.LogInformation("Climate calculation complete");

            return Result.Success(new ClimateData(
                elevation.Heightmap,
                elevation.OceanMask,
                precipitation,
                temperature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Climate calculation failed");
            return Result.Failure<ClimateData>("ERROR_WORLDGEN_CLIMATE_FAILED");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.4: Hydraulic Erosion (Rivers & Lakes)
    // ═══════════════════════════════════════════════════════════════════════

    private Result<ErosionData> SimulateErosion(ClimateData climate, PlateSimulationParams p)
    {
        _logger.LogDebug("Simulating hydraulic erosion: rivers, lakes, valley carving");

        try
        {
            // Execute erosion simulation (modifies heightmap in-place, creates rivers/lakes)
            var (erodedHeightmap, rivers, lakes) = HydraulicErosionProcessor.Execute(
                climate.Heightmap,
                climate.OceanMask,
                climate.PrecipitationMap,
                seaLevel: p.SeaLevel);

            _logger.LogInformation("Hydraulic erosion complete: {RiverCount} rivers, {LakeCount} lakes",
                rivers.Count, lakes.Count);

            // Log river statistics
            int riversReachedOcean = rivers.Count(r => r.ReachedOcean);
            int riversFormedLakes = rivers.Count - riversReachedOcean;
            _logger.LogDebug("Rivers: {Ocean} reached ocean, {Lakes} formed lakes",
                riversReachedOcean, riversFormedLakes);

            return Result.Success(new ErosionData(
                erodedHeightmap,
                climate.OceanMask,
                climate.PrecipitationMap,
                climate.TemperatureMap,
                rivers,
                lakes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hydraulic erosion failed");
            return Result.Failure<ErosionData>("ERROR_WORLDGEN_EROSION_FAILED");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.5: Hydrology Simulation (Watermap, Irrigation, Humidity)
    // ═══════════════════════════════════════════════════════════════════════

    private Result<HydrologyData> SimulateHydrology(ErosionData erosion, PlateSimulationParams p)
    {
        _logger.LogDebug("Simulating hydrology: watermap, irrigation, humidity");

        try
        {
            // Step 1: Watermap simulation (20k droplet model)
            var (watermap, watermapThresholds) = WatermapCalculator.Execute(
                erosion.Heightmap,
                erosion.OceanMask,
                erosion.PrecipitationMap,
                seed: p.Seed,
                dropletCount: 20000);

            _logger.LogDebug("Watermap simulation complete: creek={CreekThreshold:F4}, river={RiverThreshold:F4}, main_river={MainRiverThreshold:F4}",
                watermapThresholds.Creek, watermapThresholds.River, watermapThresholds.MainRiver);

            // Step 2: Irrigation simulation (moisture spreading from ocean)
            var irrigation = IrrigationCalculator.Execute(watermap, erosion.OceanMask);

            _logger.LogDebug("Irrigation simulation complete");

            // Step 3: Humidity simulation (precip + irrigation weighted average)
            var (humidity, quantiles) = HumidityCalculator.Execute(
                erosion.PrecipitationMap,
                irrigation,
                erosion.OceanMask);

            _logger.LogInformation("Hydrology simulation complete: watermap, irrigation, humidity");

            return Result.Success(new HydrologyData(
                erosion.Heightmap,
                erosion.OceanMask,
                erosion.PrecipitationMap,
                erosion.TemperatureMap,
                humidity,
                quantiles,
                watermap,
                irrigation,
                erosion.Rivers,
                erosion.Lakes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hydrology simulation failed");
            return Result.Failure<HydrologyData>("ERROR_WORLDGEN_HYDROLOGY_FAILED");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.6: Biome Classification
    // ═══════════════════════════════════════════════════════════════════════

    private PlateSimulationResult ClassifyBiomes(
        HydrologyData hydrology,
        float[,] rawHeightmap,
        uint[,] platesMap,
        // Pipeline stage data for visual debugging
        ElevationData elevationStage,
        ClimateData climateStage,
        ErosionData erosionStage,
        HydrologyData hydrologyStage)
    {
        _logger.LogDebug("Classifying biomes using Holdridge life zones model");

        // Classify biomes based on temperature, HUMIDITY (not precipitation!), and elevation
        // NOTE: Uses eroded heightmap (valleys carved around rivers)
        // NOTE: Uses humidity (precip + irrigation) instead of raw precipitation
        var biomes = BiomeClassifier.Classify(
            hydrology.Heightmap,
            hydrology.OceanMask,
            hydrology.HumidityMap,       // ✅ CORRECT: Uses humidity (precip + irrigation)
            hydrology.Quantiles,          // ✅ NEW: Humidity quantiles for moisture classification
            hydrology.TemperatureMap,
            seaLevel: NativeSeaLevelFallback(hydrology));

        _logger.LogInformation("Biome classification complete (using humidity-based moisture)");

        // Diagnostics: count water vs land cells
        int h = hydrology.Heightmap.GetLength(0);
        int w = hydrology.Heightmap.GetLength(1);
        int water = 0, land = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (biomes[y, x] == Domain.BiomeType.Ocean || biomes[y, x] == Domain.BiomeType.ShallowWater) water++; else land++;
        _logger.LogInformation("Biome counts: water={Water} land={Land}", water, land);

        // Build pipeline stage DTOs for visual debugging
        var stage1 = new Application.DTOs.Stage1_RawHeightmap(rawHeightmap, platesMap);
        var stage2 = new Application.DTOs.Stage2_ProcessedElevation(elevationStage.Heightmap, elevationStage.OceanMask);
        var stage3 = new Application.DTOs.Stage3_Temperature(climateStage.TemperatureMap);
        var stage4 = new Application.DTOs.Stage4_Precipitation(climateStage.PrecipitationMap);
        var stage5 = new Application.DTOs.Stage5_Erosion(erosionStage.Heightmap, erosionStage.Rivers, erosionStage.Lakes);
        var stage6 = new Application.DTOs.Stage6_Watermap(hydrologyStage.WatermapData, new WatermapThresholds(0, 0, 0)); // TODO: Store thresholds
        var stage7 = new Application.DTOs.Stage7_Irrigation(hydrologyStage.IrrigationMap);
        var stage8 = new Application.DTOs.Stage8_Humidity(hydrologyStage.HumidityMap, hydrologyStage.Quantiles);
        var stage9 = new Application.DTOs.Stage9_Biomes(biomes);

        return new PlateSimulationResult(
            hydrology.Heightmap,
            hydrology.OceanMask,
            hydrology.PrecipitationMap,
            hydrology.TemperatureMap,
            biomes,
            hydrology.Rivers,
            hydrology.Lakes,
            hydrology.HumidityMap,
            hydrology.WatermapData,
            hydrology.IrrigationMap,
            rawHeightmap,
            platesMap,
            // Pipeline stages for visual debugging
            stage1: stage1,
            stage2: stage2,
            stage3: stage3,
            stage4: stage4,
            stage5: stage5,
            stage6: stage6,
            stage7: stage7,
            stage8: stage8,
            stage9: stage9);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Records for Internal Pipeline
    // ═══════════════════════════════════════════════════════════════════════

    private record NativeOut(float[,] Heightmap, uint[,] Plates);

    private record ElevationData(float[,] Heightmap, bool[,] OceanMask);

    private record ClimateData(
        float[,] Heightmap,
        bool[,] OceanMask,
        float[,] PrecipitationMap,
        float[,] TemperatureMap);

    private record ErosionData(
        float[,] Heightmap,           // Eroded heightmap (valleys carved)
        bool[,] OceanMask,
        float[,] PrecipitationMap,
        float[,] TemperatureMap,
        List<River> Rivers,
        List<(int x, int y)> Lakes);

    private record HydrologyData(
        float[,] Heightmap,
        bool[,] OceanMask,
        float[,] PrecipitationMap,
        float[,] TemperatureMap,
        float[,] HumidityMap,          // Precip + irrigation (1:3 weight)
        HumidityQuantiles Quantiles,   // Quantile thresholds for moisture classification
        float[,] WatermapData,         // Flow accumulation from droplet simulation
        float[,] IrrigationMap,        // Moisture spreading from ocean
        List<River> Rivers,
        List<(int x, int y)> Lakes);

    private static float NativeSeaLevelFallback(HydrologyData hydrology)
    {
        // We do not carry parameters here; derive approximate sea level by land/ocean split if needed.
        // If more precise control is required, thread sea level through the pipeline.
        // For now, estimate sea level percentile from ocean coverage by sampling height at ocean cells' 80th percentile.
        int h = hydrology.Heightmap.GetLength(0);
        int w = hydrology.Heightmap.GetLength(1);
        var oceanHeights = new List<float>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (hydrology.OceanMask[y, x]) oceanHeights.Add(hydrology.Heightmap[y, x]);
        if (oceanHeights.Count == 0)
            return 0.65f;
        oceanHeights.Sort();
        int idx = Math.Clamp((int)(oceanHeights.Count * 0.8f), 0, oceanHeights.Count - 1);
        return oceanHeights[idx];
    }
}
