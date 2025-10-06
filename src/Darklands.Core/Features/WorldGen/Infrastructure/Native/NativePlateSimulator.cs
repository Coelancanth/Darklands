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
            .Bind(rawHeightmap =>
            {
                _logger.LogDebug("Step 3: Post-processing elevation");
                return PostProcessElevation(rawHeightmap, parameters);
            })
            .Tap(_ => _logger.LogDebug("Step 3 complete: Elevation post-processing succeeded"))
            .TapError(error => _logger.LogError("Step 3 FAILED: Elevation post-processing error: {Error}", error))
            .Bind(elevationData =>
            {
                _logger.LogDebug("Step 4: Calculating climate");
                return CalculateClimate(elevationData, parameters);
            })
            .Tap(_ => _logger.LogDebug("Step 4 complete: Climate calculation succeeded"))
            .TapError(error => _logger.LogError("Step 4 FAILED: Climate calculation error: {Error}", error))
            .Bind(climateData =>
            {
                _logger.LogDebug("Step 5: Simulating hydraulic erosion (rivers, lakes, valleys)");
                return SimulateErosion(climateData, parameters);
            })
            .Map(erosionData =>
            {
                _logger.LogDebug("Step 6: Classifying biomes");
                var result = ClassifyBiomes(erosionData);
                _logger.LogDebug("Step 6 complete: Biome classification succeeded");
                return result;
            });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.1: Native Simulation & Marshaling
    // ═══════════════════════════════════════════════════════════════════════

    private Result EnsureLibraryLoaded() =>
        NativeLibraryLoader.ValidateLibraryExists(_projectPath);

    private Result<float[,]> RunNativeSimulation(PlateSimulationParams p)
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
                return Result.Failure<float[,]>("ERROR_WORLDGEN_SIMULATION_FAILED");
            }

            using var safeHandle = new PlateSimulationHandle(handle);

            // Step until simulation completes
            int stepCount = 0;
            const int maxSteps = 10000;

            while (PlateTectonicsNative.IsFinished(handle) == 0 && stepCount < maxSteps)
            {
                PlateTectonicsNative.Step(handle);
                stepCount++;
            }

            if (stepCount >= maxSteps)
            {
                _logger.LogWarning("Simulation reached max steps ({MaxSteps})", maxSteps);
                return Result.Failure<float[,]>("ERROR_WORLDGEN_SIMULATION_TIMEOUT");
            }

            _logger.LogInformation("Simulation completed in {Steps} steps", stepCount);

            // Marshal heightmap to C# 2D array
            var heightmapPtr = PlateTectonicsNative.GetHeightmap(handle);
            if (heightmapPtr == IntPtr.Zero)
                return Result.Failure<float[,]>("ERROR_WORLDGEN_MARSHALING_FAILED");

            var heightmap = Marshal2DArray(heightmapPtr, p.WorldSize, p.WorldSize);

            return Result.Success(heightmap);
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

            return Result.Failure<float[,]>($"ERROR_WORLDGEN_NATIVE_EXCEPTION: {ex.Message}");
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

            // 2. Add Perlin noise for terrain variation
            ElevationPostProcessor.AddNoise(heightmap, p.Seed, scale: 0.05f, amplitude: 0.1f);

            // 3. Flood fill from borders to mark ocean cells
            var oceanMask = ElevationPostProcessor.FillOcean(heightmap, p.SeaLevel);

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

            // Calculate precipitation (noise + orographic lift + rain shadow)
            var precipitation = ClimateCalculator.CalculatePrecipitation(
                elevation.Heightmap,
                elevation.OceanMask,
                p.Seed);

            // Calculate temperature (latitude + elevation cooling + noise variation)
            var temperature = ClimateCalculator.CalculateTemperature(
                elevation.Heightmap,
                elevation.OceanMask,
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
    // PHASE 2.5: Biome Classification
    // ═══════════════════════════════════════════════════════════════════════

    private PlateSimulationResult ClassifyBiomes(ErosionData erosion)
    {
        _logger.LogDebug("Classifying biomes using Holdridge life zones model");

        // Classify biomes based on temperature, precipitation, and elevation
        // NOTE: Uses eroded heightmap (valleys carved around rivers)
        var biomes = BiomeClassifier.Classify(
            erosion.Heightmap,
            erosion.OceanMask,
            erosion.PrecipitationMap,
            erosion.TemperatureMap,
            seaLevel: 0.65f); // Use default sea level from params

        _logger.LogInformation("Biome classification complete");

        return new PlateSimulationResult(
            erosion.Heightmap,
            erosion.OceanMask,
            erosion.PrecipitationMap,
            erosion.TemperatureMap,
            biomes,
            erosion.Rivers,
            erosion.Lakes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Records for Internal Pipeline
    // ═══════════════════════════════════════════════════════════════════════

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
}
