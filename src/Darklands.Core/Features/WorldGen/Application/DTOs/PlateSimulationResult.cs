using Darklands.Core.Features.WorldGen.Domain;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Immutable result of world generation containing all terrain/climate data.
/// All arrays are indexed [y, x] (row-major).
/// </summary>
public record PlateSimulationResult
{
    /// <summary>
    /// Heightmap with elevation values (0.0 = lowest, 1.0 = highest).
    /// Values below SeaLevel are underwater.
    /// </summary>
    public float[,] Heightmap { get; init; }

    /// <summary>
    /// Ocean mask (true = water, false = land).
    /// Derived from heightmap and flood fill from borders.
    /// </summary>
    public bool[,] OceanMask { get; init; }

    /// <summary>
    /// Precipitation map (0.0 = arid, 1.0 = very wet).
    /// Based on latitude and rain shadow effects.
    /// </summary>
    public float[,] PrecipitationMap { get; init; }

    /// <summary>
    /// Temperature map (0.0 = coldest, 1.0 = hottest).
    /// Based on latitude and elevation (elevation cooling).
    /// </summary>
    public float[,] TemperatureMap { get; init; }

    /// <summary>
    /// Classified biomes based on Holdridge life zones model.
    /// Combines elevation, precipitation, and temperature.
    /// </summary>
    public BiomeType[,] BiomeMap { get; init; }

    /// <summary>
    /// Map width (all arrays have same dimensions)
    /// </summary>
    public int Width => Heightmap.GetLength(1);

    /// <summary>
    /// Map height (all arrays have same dimensions)
    /// </summary>
    public int Height => Heightmap.GetLength(0);

    public PlateSimulationResult(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitationMap,
        float[,] temperatureMap,
        BiomeType[,] biomeMap)
    {
        Heightmap = heightmap;
        OceanMask = oceanMask;
        PrecipitationMap = precipitationMap;
        TemperatureMap = temperatureMap;
        BiomeMap = biomeMap;
    }
}
