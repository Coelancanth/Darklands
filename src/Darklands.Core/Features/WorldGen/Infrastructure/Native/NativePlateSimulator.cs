using System;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
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

        return EnsureLibraryLoaded()
            .Bind(() => RunNativeSimulation(parameters))
            .Bind(rawHeightmap => PostProcessElevation(rawHeightmap, parameters))
            .Bind(elevationData => CalculateClimate(elevationData, parameters))
            .Map(climateData => ClassifyBiomes(climateData));
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
            _logger.LogDebug("Creating native simulation handle");

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

            if (handle == IntPtr.Zero)
                return Result.Failure<float[,]>("ERROR_WORLDGEN_SIMULATION_FAILED");

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
            _logger.LogError(ex, "Native simulation failed");
            return Result.Failure<float[,]>("ERROR_WORLDGEN_NATIVE_EXCEPTION");
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
    // PHASE 2.2: Elevation Post-Processing (STUB - to be implemented)
    // ═══════════════════════════════════════════════════════════════════════

    private Result<ElevationData> PostProcessElevation(float[,] rawHeightmap, PlateSimulationParams p)
    {
        // TODO (Phase 2.2): Implement
        // - CenterLand() - rotate to center largest landmass
        // - AddNoise() - add Simplex noise for variation
        // - PlaceOceansAtBorders() - lower border elevation
        // - FillOcean() - flood fill to mark ocean cells

        _logger.LogDebug("Post-processing elevation (STUB)");

        // STUB: Just return raw heightmap with fake ocean mask
        var oceanMask = new bool[rawHeightmap.GetLength(0), rawHeightmap.GetLength(1)];
        for (int y = 0; y < oceanMask.GetLength(0); y++)
            for (int x = 0; x < oceanMask.GetLength(1); x++)
                oceanMask[y, x] = rawHeightmap[y, x] < p.SeaLevel;

        return Result.Success(new ElevationData(rawHeightmap, oceanMask));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.3: Climate Calculation (STUB - to be implemented)
    // ═══════════════════════════════════════════════════════════════════════

    private Result<ClimateData> CalculateClimate(ElevationData elevation, PlateSimulationParams p)
    {
        // TODO (Phase 2.3): Implement
        // - CalculatePrecipitation() - latitude-based
        // - CalculateTemperature() - latitude + elevation cooling

        _logger.LogDebug("Calculating climate (STUB)");

        // STUB: Return zero maps
        var size = elevation.Heightmap.GetLength(0);
        var precipitation = new float[size, size];
        var temperature = new float[size, size];

        return Result.Success(new ClimateData(
            elevation.Heightmap,
            elevation.OceanMask,
            precipitation,
            temperature));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.4: Biome Classification (STUB - to be implemented)
    // ═══════════════════════════════════════════════════════════════════════

    private PlateSimulationResult ClassifyBiomes(ClimateData climate)
    {
        // TODO (Phase 2.4): Implement
        // - Holdridge life zones model
        // - Combine elevation, precipitation, temperature

        _logger.LogDebug("Classifying biomes (STUB)");

        // STUB: Simple elevation-based biomes
        var size = climate.Heightmap.GetLength(0);
        var biomes = new BiomeType[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (climate.OceanMask[y, x])
                    biomes[y, x] = BiomeType.Ocean;
                else if (climate.Heightmap[y, x] > 0.8f)
                    biomes[y, x] = BiomeType.Ice; // High mountains
                else if (climate.Heightmap[y, x] > 0.7f)
                    biomes[y, x] = BiomeType.Tundra;
                else
                    biomes[y, x] = BiomeType.Grassland;
            }
        }

        return new PlateSimulationResult(
            climate.Heightmap,
            climate.OceanMask,
            climate.PrecipitationMap,
            climate.TemperatureMap,
            biomes);
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
}
