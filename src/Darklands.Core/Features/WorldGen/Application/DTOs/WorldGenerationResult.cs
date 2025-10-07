namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Complete world generation output including native simulation and post-processing results.
/// This is the pipeline's output type (vs PlateSimulationResult which is native-only).
/// </summary>
/// <remarks>
/// Design: Optional fields allow incremental implementation of VS_022 phases.
/// - Phase 0 (current): Only heightmap + plates (pass-through from native)
/// - Phase 1: Add OceanMask (normalized elevation + sea level detection)
/// - Phase 2: Add TemperatureMap (latitude + elevation cooling)
/// - Phase 3: Add PrecipitationMap (with rain shadow)
/// - Phase 4+: Erosion, hydrology, biomes
/// </remarks>
public record WorldGenerationResult
{
    /// <summary>
    /// Heightmap from simulation.
    /// Currently: Raw from native (unnormalized, typically 0-20 range).
    /// Phase 1: Will be normalized to [0, 1] range.
    /// </summary>
    public float[,] Heightmap { get; init; }

    /// <summary>
    /// Plate ownership map (plate ID per cell).
    /// </summary>
    public uint[,] PlatesMap { get; init; }

    /// <summary>
    /// Ocean mask (true = water, false = land).
    /// Available after Phase 1 implementation.
    /// </summary>
    public bool[,]? OceanMask { get; init; }

    /// <summary>
    /// Temperature map in Celsius.
    /// Available after Phase 2 implementation.
    /// </summary>
    public float[,]? TemperatureMap { get; init; }

    /// <summary>
    /// Precipitation map in mm/year.
    /// Available after Phase 3 implementation.
    /// </summary>
    public float[,]? PrecipitationMap { get; init; }

    /// <summary>
    /// Raw native output preserved for debugging and visualization.
    /// Always available regardless of pipeline phases.
    /// </summary>
    public PlateSimulationResult RawNativeOutput { get; init; }

    /// <summary>
    /// Map width (all arrays have same dimensions).
    /// </summary>
    public int Width => Heightmap.GetLength(1);

    /// <summary>
    /// Map height (all arrays have same dimensions).
    /// </summary>
    public int Height => Heightmap.GetLength(0);

    public WorldGenerationResult(
        float[,] heightmap,
        uint[,] platesMap,
        PlateSimulationResult rawNativeOutput,
        bool[,]? oceanMask = null,
        float[,]? temperatureMap = null,
        float[,]? precipitationMap = null)
    {
        Heightmap = heightmap;
        PlatesMap = platesMap;
        RawNativeOutput = rawNativeOutput;
        OceanMask = oceanMask;
        TemperatureMap = temperatureMap;
        PrecipitationMap = precipitationMap;
    }
}
